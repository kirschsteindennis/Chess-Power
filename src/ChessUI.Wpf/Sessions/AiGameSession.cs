using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.ViewModel;
using ChessUI.Shared.Services;

namespace ChessUI.Wpf.Sessions;

public sealed class AiGameSession(AIDifficulty difficulty) : IGameSession
{
    private readonly AIGameState game = new AIGameState(Player.White, Board.Initial(), difficulty);
    private bool aiIsThinking;

    public event Action Changed;

    public Board Board => game.Board;
    public Player CurrentPlayer => game.CurrentPlayer;
    public Player LocalPlayer => Player.White;
    public Result Result => game.Result;
    public bool IsGameOver => game.IsGameOver();
    public bool IsLocalTurn => !aiIsThinking && !IsGameOver && CurrentPlayer == LocalPlayer;

    public IEnumerable<Move> LegalMovesFor(Position pos)
    {
        return IsLocalTurn ? game.LegalMovesForPiece(pos).ToList() : [];
    }

    public async Task<bool> TryMakeMoveAsync(Move move)
    {
        if (!IsLocalTurn || !IsAllowed(move))
            return false;

        game.MakeMove(move);

        if (IsGameOver)
        {
            Changed?.Invoke();
            return true;
        }

        aiIsThinking = true;
        Changed?.Invoke();

        await Task.Delay(300);                      // short "thinking pause", so the ai move is visible
        await Task.Run(() => game.MakeAIMove());    // Calculation in Background - ui stays steady

        aiIsThinking = false;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Since we never trust the UI, the move needs to be really legal
    /// </summary>
    /// <param name="move">The move to check</param>
    /// <returns>True if the move is legal, false if not</returns>
    private bool IsAllowed(Move move)
    {
        return game.LegalMovesForPiece(move.FromPos)
            .Any(m => m.ToPos == move.ToPos && m.Type == move.Type);
    }
}
