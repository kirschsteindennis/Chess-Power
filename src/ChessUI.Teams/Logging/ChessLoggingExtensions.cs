using ChessLogic.Diagnostics;
using ChessLogic.Services;
using ChessPower.Logging;
using ChessPower.Logging.Privacy;
using ChessPower.Logging.Tracing;
using Serilog;
using Serilog.Formatting.Compact;
using System.Reflection;

namespace ChessUI.Teams.Logging;

public static class ChessLoggingExtensions
{
    /// <summary>
    /// A simple logger for the time before the real one is ready,
    /// so that an error during startup is not lost
    /// </summary>
    /// <returns>Returns a bootstrap logger</returns>
    public static Serilog.ILogger CreateBootstrapLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateBootstrapLogger();
    }

    public static WebApplicationBuilder AddChessLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(CreatePseudonymizer(builder.Configuration));

        // Without a listener there are no activities, and without activities no TraceId in the log.
        // It lives as long as the program, so it is never disposed.
        ChessActivityListener.Start(ChessTelemetry.SourceName);

        string version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        bool development = builder.Environment.IsDevelopment();

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)              // levels from appsettings.json
                .ReadFrom.Services(services)
                .EnrichWithChessDefaults("ChessPower.Teams", version);

            if (development)
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}");
            else
                // One JSON object per line: Azure and log tools can read it without guessing
                configuration.WriteTo.Console(new CompactJsonFormatter());
        });

        return builder;
    }

    private static HmacPseudonymizer CreatePseudonymizer(ConfigurationManager configuration)
    {
        string key = configuration["Privacy:PseudonymKey"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            Log.Warning("Privacy:PseudonymKey is missing or too short. The pseudonyms in the log change with every restart");
            return HmacPseudonymizer.CreateEphemeral();
        }

        return new HmacPseudonymizer(key);
    }
}
