namespace ChessLogic.Tests.Support;

/// <summary>
/// A clock that only moves when the test says so. Timers fire when the timer is advanced past them
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object gate = new();
    private readonly List<ManualTimer> timers = [];
    private readonly DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private long ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (gate)
            return ticks;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (gate)
            return start + TimeSpan.FromTicks(ticks);
    }

    /// <summary>How many timers are alive (not disposed) and waiting to fire</summary>
    public int ActiveTimerCount
    {
        get
        {
            lock (gate)
                return timers.Count(t => t.Due.HasValue);
        }
    }

    /// <summary>How long until the next timer would fire, or null if no timer is waiting</summary>
    public TimeSpan? NextTimerIn
    {
        get
        {
            lock (gate)
            {
                long? next = timers.Where(t => t.Due.HasValue).Select(t => t.Due.Value).Cast<long?>().Min();
                return next.HasValue ? TimeSpan.FromTicks(next.Value - ticks) : null;
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        ManualTimer timer = new(this, callback, state);

        lock (gate)
            timers.Add(timer);

        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan delta)
    {
        long target;

        lock (gate)
            target = ticks + delta.Ticks;

        while (true)
        {
            ManualTimer next;

            lock (gate)
            {
                next = timers.Where(t => t.Due.HasValue && t.Due.Value <= target)
                             .OrderBy(t => t.Due.Value)
                             .FirstOrDefault();

                if (next == null)
                {
                    ticks = target;
                    return;
                }

                // Time moves to the moment of the timer, and a repeating timer is set up again
                ticks = Math.Max(ticks, next.Due.Value);
                next.Due = next.Period > TimeSpan.Zero ? next.Due.Value + next.Period.Ticks : null;
            }

            next.Callback(next.State);
        }
    }

    /// <summary>Moves the time WITHOUT firing the timers: a timer that is late, as it can be in reality</summary>
    public void AdvanceWithoutFiring(TimeSpan delta)
    {
        lock (gate)
            ticks += delta.Ticks;
    }

    /// <summary>Fires every timer now, whatever its time: a timer that fires too early</summary>
    public void FireTimersNow()
    {
        List<ManualTimer> due;

        lock (gate)
            due = timers.Where(t => t.Due.HasValue).ToList();

        foreach (ManualTimer timer in due)
            timer.Callback(timer.State);
    }

    private void Remove(ManualTimer timer)
    {
        lock (gate)
            timers.Remove(timer);
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object state) : ITimer
    {
        public TimerCallback Callback { get; } = callback;
        public object State { get; } = state;
        public long? Due { get; set; }
        public TimeSpan Period { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner.gate)
            {
                Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner.ticks + dueTime.Ticks;
                Period = period == Timeout.InfiniteTimeSpan ? TimeSpan.Zero : period;
            }

            return true;
        }

        public void Dispose()
        {
            lock (owner.gate)
                Due = null;

            owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
