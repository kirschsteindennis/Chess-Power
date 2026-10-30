using Serilog.Core;
using Serilog.Events;

namespace ChessPower.Logging.Tests.Support;

/// <summary>Catches the log entries in memory, so that tests can look at them</summary>
internal sealed class CollectingSink : ILogEventSink
{
    private readonly List<LogEvent> events = [];

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (events)
                return events.ToList();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (events)
            events.Add(logEvent);
    }
}
