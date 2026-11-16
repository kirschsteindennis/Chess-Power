using System.Diagnostics;

namespace ChessLogic.Diagnostics;

/// <summary>
/// The source of the "activities" of Chess Power. Everything that happens inside an activity
/// gets the same TraceId in the log, no matter which class writes the log entry.
/// </summary>
public static class ChessTelemetry
{
    public const string SourceName = "ChessPower";

    public static readonly ActivitySource Source = new(SourceName);
}
