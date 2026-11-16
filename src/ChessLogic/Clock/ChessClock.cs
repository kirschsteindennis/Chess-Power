using ChessLogic.Enums;
using System.Data;

namespace ChessLogic.Clock;

/// <summary>
/// The chess clock of one match. Only the clock of the player who has to move is running.
/// It reads the time from a TimeProvider, so tests can set the time themselves.
/// All methods are multi-thread safe
/// </summary>
public sealed class ChessClock : IDisposable
{
    private readonly object gate = new();
    private readonly TimeProvider time;
    private readonly ITimer timer;

    private TimeSpan white, black;
    private Player running = Player.None;
    private long startedAt;
    private bool stopped, disposed;

    public ChessClock(TimeControl control, TimeProvider timeProvider)
    {
        Control = control ?? throw new ArgumentNullException(nameof(control));
        time = timeProvider ?? TimeProvider.System;

        white = control.Initial;
        black = control.Initial;

        // The timer wakes the clock up when the running player's time is over
        timer = time.CreateTimer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public TimeControl Control { get; }

    /// <summary>
    /// Raised when the time of the player whose clock runs is over. The clock has stopped by then.
    /// Raised on another thread and never while a lock of the clock is held.
    /// </summary>
    public event Action<Player> FlagFell;

    public Player Running
    {
        get
        {
            lock (gate)
                return running;
        }
    }

    /// <summary>
    /// Starts the clock of the player who has to move first.
    /// Does nothing if the clock was started before, or if it was stopped
    /// </summary>
    /// <param name="first">The Player</param>
    public void Start(Player first)
    {
        lock (gate)
        {
            if (disposed || stopped || running != Player.None || first == Player.None)
                return;

            running = first;
            startedAt = time.GetTimestamp();
            Schedule();
        }
    }

    /// <summary>
    /// The player has moved: his time is stopped, the increment is added, and the clock of the opponent starts.
    /// </summary>
    /// <param name="mover">The Player that made the move</param>
    /// <returns>False if the time of the player was already over. Then the clock stops and nothing is added.</returns>
    public bool Press(Player mover)
    {
        lock (gate)
        {
            // Not the clock of this player (not started, stopped, or the other side runs): nothing to do
            if (disposed || running != mover)
                return true;

            TimeSpan left = RemainingNoLock(mover);

            if (left <= TimeSpan.Zero)
            {
                SetTime(mover, TimeSpan.Zero);
                running = Player.None;
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return false;
            }

            SetTime(mover, left + Control.Increment);
            running = mover.Opponent();
            startedAt = time.GetTimestamp();
            Schedule();
            return true;
        }
    }

    /// <summary>Stops the clock for good, for example when the match is over.The times stay as they are.</summary>
    public void Stop()
    {
        lock (gate)
        {
            stopped = true;

            if (running != Player.None)
            {
                SetTime(running, RemainingNoLock(running));
                running = Player.None;
            }

            if (!disposed)
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }
}
