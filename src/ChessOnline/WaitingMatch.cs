using ChessLogic.Clock;

namespace ChessOnline;

/// <summary>A match in a chat that waits for an opponent. Whoever joins plays with this time control</summary>
/// <param name="Control">The <see cref="TimeControl"/>, or null if the match is played without a clock</param>
public sealed record WaitingMatch(TimeControl Control);
