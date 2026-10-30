using ChessLogic.Diagnostics;
using System.Diagnostics;

namespace ChessPower.Logging.Tracing;

/// <summary>
/// An activity only exists if somebody listens to its source. This listener makes sure
/// that the activities of Chess Power are always created, so every log entry gets a TraceId.
/// (Later a tracing tool like OpenTelemetry can take over this job.)
/// </summary>
public static class ChessActivityListener
{
    /// <summary>The name of the source, see <see cref="ChessTelemetry"/></summary>
    public const string DefaultSourceName = "ChessPower";

    /// <summary>
    /// Creates a Disposable to listen
    /// </summary>
    /// <param name="sourceNames">A List of Sources to Listen to</param>
    /// <returns>A Disposable to start listening - Dispose it to stop listening</returns>
    public static IDisposable Start(params string[] sourceNames)
    {
        HashSet<string> names = [.. sourceNames.Length == 0 ? [DefaultSourceName] : sourceNames];

        ActivityListener listener = new()
        {
            ShouldListenTo = source => names.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
