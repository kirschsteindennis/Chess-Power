using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;

namespace ChessUI.Shared.Services;

/// <summary>
/// A running session from the view of the local user
/// The Board only knows this Interface, not how the moves are made
/// </summary>
public interface IGameSession
{
    /// <summary>The Game Board</summary>
    Board Board { get; }
    /// <summary>The Current Player whos turn it is</summary>
    Player CurrentPlayer { get; }

    /// <summary>Which color the local player uses. On Black the board will be turned</summary>
    Player LocalPlayer { get; }

    /// <summary>The Result of the game</summary>
    Result Result { get; }
    /// <summary>Bool to tell if it's game over</summary>
    bool IsGameOver { get; }

    /// <summary>False, if the enemy (AI or Human) is in turn</summary>
    bool IsLocalTurn { get; }

    /// <summary>Will be fired, when something changed (own turn, enemy turn, game over, etc)</summary>
    event Action Changed;

    /// <summary>Shows all legal moves for the current chosen position</summary>
    /// <param name="pos">The current chosen Position</param>
    /// <returns>Returns a list of legal moves for this Position</returns>
    IEnumerable<Move> LegalMovesFor(Position pos);
    /// <summary>The Move that was chosen</summary>
    /// <param name="move">The chosen Move</param>
    /// <returns>Returns true if the move was successful, false if not</returns>
    Task<bool> TryMakeMoveAsync(Move move);
}
