using ChessLogic.Enums;
using System.Runtime;

namespace ChessLogic.Clock;

/// <summary>
/// How much time each player has: a starting time and a bonus that is added after every move
/// (the "Fisher Increment" used in tournaments). "5+3" means 5 minutes plus 3 seconds per move
/// </summary>
public sealed class TimeControl
{
    public static readonly TimeSpan MaxInitial = TimeSpan.FromHours(3);
    public static readonly TimeSpan MaxIncrement = TimeSpan.FromMinutes(3);

    public TimeControl(TimeSpan initial, TimeSpan increment)
    {
        if (initial <= TimeSpan.Zero || initial > MaxInitial)
            throw new ArgumentOutOfRangeException(nameof(initial), "The starting time must be more than 0 and at most 3 hours.");

        if (increment < TimeSpan.Zero || increment > MaxIncrement)
            throw new ArgumentOutOfRangeException(nameof(increment), "The increment must be between 0 and 3 minutes.");

        Initial = initial;
        Increment = increment;
    }

    /// <summary>The ready-made time controls that the menu offers</summary>
    public static IReadOnlyList<TimeControl> Presets { get; } =
    [
        Minutes(1),
        Minutes(3, 2),
        Minutes(5),
        Minutes(10),
        Minutes(15, 10),
        Minutes(30),
    ];

    public TimeSpan Initial { get; }
    public TimeSpan Increment { get; }

    public static TimeControl Minutes(int minutes, int incrementSeconds = 0)
    {
        return new TimeControl(TimeSpan.FromMinutes(minutes), TimeSpan.FromSeconds(incrementSeconds));
    }

    /// <summary>"5+0" (minutes + seconds per move), or "0:30+0" for a starting time that is not whole minutes</summary>
    public string Name
    {
        get
        {
            int seconds = (int)Initial.TotalSeconds;
            string start = seconds % 60 == 0 ? $"{seconds / 60}" : $"{seconds / 60}:{seconds % 60:00}";
            return $"{start}+{(int)Increment.TotalSeconds}";
        }
    }

    /// <summary>The kind of game, estimated like chess servers do: starting time plus 40 moves of increment</summary>
    public TimeCategory Category
    {
        get
        {
            double estimated = (Initial + 40 * Increment).TotalSeconds;

            return estimated switch
            {
                < 180 => TimeCategory.Bullet,
                < 480 => TimeCategory.Blitz,
                < 1500 => TimeCategory.Rapid,
                _ => TimeCategory.Classical,
            };
        }
    }

    /// <summary>For example "Blitz 5+0"</summary>
    public string Label => $"{Category} {Name}";
}
