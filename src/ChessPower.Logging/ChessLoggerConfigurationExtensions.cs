using ChessPower.Logging.Privacy;
using ChessPower.Logging.Tracing;
using Serilog;

namespace ChessPower.Logging;

public static class ChessLoggerConfigurationExtensions
{
    /// <summary>
    /// The settings that every Chess Power program shares: context properties. TraceId and privacy.
    /// Deliberately NOT used: the machine name and the user name of the computer (personal data).
    /// </summary>
    /// <param name="configuration">The <see cref="LoggerConfiguration"/> from Serilog</param>
    /// <param name="application">The Name of the Application</param>
    /// <param name="version">The Version of the Application</param>
    /// <returns></returns>
    public static LoggerConfiguration EnrichWithChessDefaults(
        this LoggerConfiguration configuration, string application, string version)
    {
        return configuration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", application)
            .Enrich.WithProperty("AppVersion", version)
            .Enrich.With<TraceContextEnricher>()
            // Always last: it cleans what all the others has added
            .Enrich.With<PrivacyEnricher>();
    }
}
