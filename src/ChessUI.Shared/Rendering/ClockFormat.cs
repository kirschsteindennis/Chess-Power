namespace ChessUI.Shared.Rendering;

/// <summary>How a chess clock shows the time that is left</summary>
public static class ClockFormat
{
    /// <summary>
    /// "5:00", "0:59", "1:05:00", and with tenth under 10 seconds: "0:09.4"
    /// The Time is rounded UP, so the display shows 0:00.0 exactly when the time is over
    /// </summary>
    /// <param name="remaining"></param>
    /// <returns></returns>
    public static string Format(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        long tenths = (long)Math.Ceiling(remaining.TotalMilliseconds / 100.0);

        if (tenths < 100)
            return $"0:{tenths / 10:00}.{tenths % 10}";

        long seconds = (long)Math.Ceiling(remaining.TotalSeconds);
        long hours = seconds / 3600;
        long minutes = seconds % 3600 / 60;
        long rest = seconds % 60;

        return hours > 0 ? $"{hours}:{minutes:00}:{rest:00}" : $"{minutes:00}:{rest:00}";
    }

    /// <summary>
    /// Time is getting short: 30 seconds, or a fifth of the starting time if that is less
    /// </summary>
    /// <param name="remaining">The remaining Time</param>
    /// <param name="initial">The initial Time</param>
    /// <returns>True if time is 30 seconds or a fifth of the starting time if that is less</returns>
    public static bool IsLow(TimeSpan remaining, TimeSpan initial)
    {
        return remaining <= TimeSpan.FromSeconds(Math.Min(30, initial.TotalSeconds * 0.2));
    }
}
