using ChessLogic.Diagnostics;
using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.ViewModel;
using ChessUI.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace ChessUI.Wpf.Sessions;

public sealed class AiGameSession : IGameSession
{
    private readonly AIGameState game;
    private readonly AIDifficulty difficulty;
    private readonly ILogger<AiGameSession> logger;
    private bool aiIsThinking;

    public AiGameSession(AIDifficulty difficulty, ILogger<AiGameSession> logger = null)
    {
        this.difficulty = difficulty;
        this.logger = logger ?? NullLogger<AiGameSession>.Instance;
        game = new(Player.White, Board.Initial(), difficulty);

        this.logger.LogInformation("Game against the computer started ({Difficulty})", difficulty.ToString());
    }

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
        // The move of the human and the answer of the computer share one TraceId
        using Activity activity = ChessTelemetry.Source.StartActivity("Game.MakeMove");

        if (!IsLocalTurn || !IsAllowed(move))
        {
            logger.LogWarning("Move rejected: it is not allowed right now");
            return false;
        }

        game.MakeMove(move);
        logger.LogDebug("The human played {MoveType}", move.Type.ToString());

        if (IsGameOver)
        {
            LogGameOver();
            Changed?.Invoke();
            return true;
        }

        aiIsThinking = true;
        Changed?.Invoke();

        await Task.Delay(300);                      // short "thinking pause", so the ai move is visible

        Stopwatch watch = Stopwatch.StartNew();
        await Task.Run(() => game.MakeAIMove());    // Calculation in Background - ui stays steady
        watch.Stop();

        logger.LogDebug("The computer ({Difficulty}) answered after {ElapsedMs} ms",
            difficulty.ToString(), watch.ElapsedMilliseconds);

        aiIsThinking = false;

        if (IsGameOver)
            LogGameOver();

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

    private void LogGameOver()
    {
        string result = Result.Winner == Player.None ? "draw" : $"{Result.Winner} wins";
        logger.LogInformation("Game finished: {Result} ({Reason})", result, Result.Reason.ToString());
    }
}
