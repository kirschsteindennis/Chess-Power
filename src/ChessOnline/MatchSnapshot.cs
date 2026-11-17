using ChessLogic.Clock;
using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;

namespace ChessOnline;

/// <summary>
/// Immutable copy of the game state. The UI only reads snapshots, never the live board
/// </summary>
/// <param name="Board">The Board that is played</param>
/// <param name="CurrentPlayer">The <see cref="Player"/> that has the current turn</param>
/// <param name="Result">The current Result</param>
/// <param name="Status">The current <see cref="MatchStatus"/></param>
/// <param name="Clock">The state of the chess clock, or null if the match is played without a clock</param>
public sealed record MatchSnapshot(Board Board, Player CurrentPlayer, Result Result, MatchStatus Status, ClockSnapshot Clock = null);
