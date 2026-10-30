using ChessPower.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Reflection;

namespace ChessUI.Wpf.Logging;

public static class DesktopLogging
{
    /// <summary>%LocalAppData%\ChessPower\logs. The files stay on this computer, nothing is sent anywhere.</summary>
    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChessPower", "logs");

    public static Serilog.ILogger Create()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

#if DEBUG
        const LogEventLevel level = LogEventLevel.Debug;
#else
        const LogEventLevel level = LogEventLevel.Information;
#endif

        return new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            // One Id per program start: with it a bug report can be found in the file
            .Enrich.WithProperty("RunId", Guid.NewGuid().ToString("N")[..8])
            .EnrichWithChessDefaults("ChessPower.Wpf", version)
            .WriteTo.File(
                path: Path.Combine(LogDirectory, "chesspower-.log"),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{RunId}] [{TraceId}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 5_000_000,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }
}
