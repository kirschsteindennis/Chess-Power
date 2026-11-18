using ChessLogic.Clock;
using ChessLogic.Diagnostics;
using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.ViewModel;
using ChessUI.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.CodeDom;
using System.Diagnostics;

namespace ChessUI.Wpf.Sessions;

/// <summary>
/// A game of the human (white) against the computer (black), with an optional chess clock.
/// The clock of the computer runs while it "thinks", like the clock of a human.
/// </summary>
public sealed class AiGameSession : IGameSession, IDisposable
{
    // The short pause before the computer answers, so that the move can be seen
    private static readonly TimeSpan ThinkingPause = TimeSpan.FromMilliseconds(300);

    // The state of the game is used by the user interface, the computer's calculation and the timer of the clock
    private readonly object gate = new();
    private readonly AIGameState game;
    private readonly AIDifficulty difficulty;
    private readonly ILogger<AiGameSession> logger;
    private readonly TimeProvider time;
    private readonly ChessClock clock;
    private bool aiIsThinking;

    /// <summary>
    /// The Game Session with an AI
    /// </summary>
    /// <param name="difficulty">The <see cref="AIDifficulty"/> of the Opponent</param>
    /// <param name="logger">The Logger for the Session</param>
    /// <param name="timeControl">How much time each player has, or null to play without a clock</param>
    /// <param name="timeProvider">The source of the time. Only tests replace it.</param>
    public AiGameSession(AIDifficulty difficulty, ILogger<AiGameSession> logger = null,
        TimeControl timeControl = null, TimeProvider timeProvider = null)
    {
        this.difficulty = difficulty;
        this.logger = logger ?? NullLogger<AiGameSession>.Instance;
        time = timeProvider ?? TimeProvider.System;
        game = new AIGameState(Player.White, Board.Initial(), difficulty);

        if (timeControl != null)
        {
            clock = new ChessClock(timeControl, time);
            clock.FlagFell += OnFlagFell;
            clock.Start(Player.White);
        }

        this.logger.LogInformation("Game against the computer started ({Difficulty}, clock {TimeControl})",
            difficulty.ToString(), timeControl?.Name ?? "none");
    }

    public event Action Changed;

    public Board Board => game.Board;
    public Player CurrentPlayer => game.CurrentPlayer;
    public Player LocalPlayer => Player.White;
    public Result Result => game.Result;
    public bool IsGameOver => game.IsGameOver();
    public bool IsLocalTurn => !aiIsThinking && !IsGameOver && CurrentPlayer == LocalPlayer;

    public ClockSnapshot Clock => clock?.Snapshot();

    private static Player ComputerPlayer => Player.Black;

    public IEnumerable<Move> LegalMovesFor(Position pos)
    {
        return IsLocalTurn ? game.LegalMovesForPiece(pos).ToList() : [];
    }

    public async Task<bool> TryMakeMoveAsync(Move move)
    {
        // The move of the human and the answer of the computer share one TraceId
        using Activity activity = ChessTelemetry.Source.StartActivity("Game.MakeMove");

        bool accepted, computerMoves;

        lock (gate)
        {
            if (!IsLocalTurn || !IsAllowed(move))
            {
                logger.LogWarning("Move rejected: it is not allowed right now");
                return false;
            }

            if (clock != null && clock.IsExpired(LocalPlayer))
            {
                // The move came after the time was up, before the timer could report it
                logger.LogWarning("Move rejected: the time is up");
                EndOnTime(LocalPlayer);
                accepted = false;
                computerMoves = false;
            }
            else
            {
                game.MakeMove(move);
                logger.LogDebug("The human played {MoveType}", move.Type.ToString());
                accepted = true;

                if (IsGameOver)
                {
                    LogGameOver();
                    StopClock();
                    computerMoves = false;
                }
                else if (clock != null && !clock.Press(LocalPlayer))
                {
                    EndOnTime(LocalPlayer);       // the time ran out in the very moment of the move
                    computerMoves = false;
                }
                else
                {
                    aiIsThinking = true;
                    computerMoves = true;
                }
            }
        }

        Changed?.Invoke();

        if (!computerMoves)
            return accepted;

        await Task.Delay(ThinkingPause, time);
        await Task.Run(PlayTheComputersMove);   // the calculation runs in the background, the screen stays smooth

        Changed?.Invoke();
        return true;
    }

    public void Dispose()
    {
        clock?.Dispose();
    }

    private void PlayTheComputersMove()
    {
        lock (gate)
        {
            try
            {
                // The time of the computer can have run out while it was "thinking"
                if (IsGameOver)
                    return;

                Stopwatch watch = Stopwatch.StartNew();
                game.MakeAIMove();
                watch.Stop();

                logger.LogDebug("The computer ({Difficulty}) answered after {ElapsedMs} ms",
                    difficulty.ToString(), watch.ElapsedMilliseconds);

                if (IsGameOver)
                {
                    LogGameOver();
                    StopClock();
                }
                else if (clock != null && !clock.Press(ComputerPlayer))
                    EndOnTime(ComputerPlayer);       // the time ran out in the very moment of the move
            }
            finally
            {
                aiIsThinking = false;
            }
        }
    }

    // Called by the timer of the clock, on another thread, when the time of a player is over
    private void OnFlagFell(Player flagged)
    {
        lock (gate)
        {
            if (IsGameOver)
                return;

            EndOnTime(flagged);
        }

        Changed?.Invoke();
    }

    private void EndOnTime(Player flagged)
    {
        logger.LogInformation("{Player} ran out of time", flagged.ToString());
        game.LoseOnTime(flagged);
        LogGameOver();
        StopClock();
    }

    private void StopClock()
    {
        clock?.Stop();
        clock?.Dispose();
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
