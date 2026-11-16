using ChessLogic.Enums;

namespace ChessLogic.Clock;

/// <summary>
/// The state of the chess clock at one moment. It is immutable, so a screen can read it at any time.
/// The time of the player whose clock is running keeps going down: ask <see cref="RemainingOf"/> with the current timestamp.
/// </summary>
/// <param name="Control">The <see cref="TimeControl"/> Instance</param>
/// <param name="White">White's time at <paramref name="Timestamp"/></param>
/// <param name="Black">Black's time at <paramref name="Timestamp"/></param>
/// <param name="Running">Whose clock is running, or None if the clock is stopped</param>
/// <param name="Timestamp">The moment of the snapshot, from <see cref="TimeProvider.GetTimestamp()"/></param>
/// <param name="TimestampFrequency">Ticks per Second of the timestamp</param>
public sealed record ClockSnapshot(
    TimeControl Control, TimeSpan White, TimeSpan Black, Player Running, long Timestamp, long TimestampFrequency)
{
    /// <summary>
    /// Shows the remaining Time of the player
    /// </summary>
    /// <param name="player">The player to check for</param>
    /// <param name="now">The current timestamp from the same TimeProvider</param>
    /// <returns>Returns the remaining time of the player</returns>
    public TimeSpan RemainingOf(Player player, long now)
    {
        TimeSpan atSnapshot = player == Player.White ? White : Black;

        if (player != Running)
            return atSnapshot;

        // A double: a long multiplication could overflow for long elapsed times
        TimeSpan elapsed = TimeSpan.FromSeconds((now - Timestamp) / (double)TimestampFrequency);
        TimeSpan remaining = atSnapshot - elapsed;

        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }
}
