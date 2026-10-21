using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;

namespace ChessOnline;

/// <summary>
/// What a player's session needs from a match. Exists so that session can be tested
/// </summary>
public interface IChessMatch
{
    /// <summary>Raised after every change: opponent joined, move made, game over</summary>
    event Action Changed;

    MatchSnapshot Snapshot { get; }

    Player ColorOf(string userId);

    IEnumerable<Move> LegalMovesFor(string userId, Position pos);

    bool TryMakeMove(string userId, Move move);

    bool Resign(string userId);
}
