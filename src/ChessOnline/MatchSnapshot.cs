using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;

namespace ChessOnline;

/// <summary>
/// Immutable copy of the game state. The UI only reads snapshots, never the live board
/// </summary>
public sealed record MatchSnapshot(Board Board, Player CurrentPlayer, Result Result, MatchStatus Status);
