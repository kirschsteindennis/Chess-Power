using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace ChessPower.Logging.Tracing;

/// <summary>
/// Adds the TraceId and the SpanId of the current activity to every log entry.
/// All entries that belong to the same action (for example one chess move) share the same TraceId
/// </summary>
public sealed class TraceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Activity activity = Activity.Current;

        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString()));
    }
}
