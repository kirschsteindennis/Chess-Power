using ChessLogic.Clock;
using ChessLogic.Diagnostics;
using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Services;
using ChessLogic.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace ChessOnline;

/// <summary>
/// One match between two people. Every access to the GameState happens inside the lock,
/// because both players use the same match from different connections at the same time
/// </summary>
/// <param name="timeControl">How much time each player has, or null to play without a clock</param>
/// <param name="timeProvider">The source of the time. Only tests replace it.</param>
public sealed class ChessMatch(string _creatorUserId, IRandomSource _random, ILogger _logger = null,
    TimeControl timeControl = null, TimeProvider _timeProvider = null) : IChessMatch
{
    private readonly object gate = new();
    private readonly IRandomSource random = _random;
    private readonly ILogger logger = _logger ?? NullLogger.Instance;
    private readonly TimeProvider timeProvider = _timeProvider ?? TimeProvider.System;
    private readonly string firstUserId = _creatorUserId;
    private string secondUserId;
    private string whiteUserId;
    private string blackUserId;
    private GameState game;
    private ChessClock clock;
    private volatile MatchSnapshot snapshot = new(null, Player.None, null, MatchStatus.Waiting);

    /// <summary>A random number, that says nothing about the players. It connects all log entries of this match</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>The time control of this match, or null if it is played without a clock</summary>
    public TimeControl TimeControl { get; } = timeControl;

    /// <summary>Raised after every change: opponent joined, move made, game over</summary>
    public event Action Changed;

    public MatchSnapshot Snapshot => snapshot;

    public bool IsWaitingFor(string userId)
    {
        return snapshot.Status == MatchStatus.Waiting && userId == firstUserId;
    }

    public Player ColorOf(string userId)
    {
        lock (gate)
        {
            if (userId == whiteUserId) return Player.White;
            if (userId == blackUserId) return Player.Black;
            return Player.None;
        }
    }

    /// <returns>True if the user takes part in this match afterwards, false if it is already full</returns>
    public bool Join(string userId)
    {
        lock (gate)
        {
            if (userId == firstUserId || userId == secondUserId)
                return true;    // Page reload or reconnect

            if (secondUserId != null)
                return false;   // a third person

            secondUserId = userId;
            Start();
        }

        Changed?.Invoke();
        return true;
    }

    public IEnumerable<Move> LegalMovesFor(string userId, Position pos)
    {
        lock (gate)
        {
            if (!IsTurnOf(userId))
                return [];

            return game.LegalMovesForPiece(pos).ToList();
        }
    }

    public bool TryMakeMove(string userId, Move move)
    {
        // Everything that happens during this move (also the reaction of the other player) shares one TraceId
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.MakeMove");
        activity?.SetTag("chess.match_id", Id);

        bool accepted;

        lock (gate)
        {
            string rejection = WhyNotTurnOf(userId);

            // The server decides: The move must be legal
            if (rejection == null && !game.LegalMovesForPiece(move.FromPos)
                .Any(m => m.ToPos == move.ToPos && m.Type == move.Type))
                rejection = "illegal-move";

            if (rejection != null)
            {
                logger.LogWarning("Move rejected in match {MatchId}: {Reason}", Id, rejection);
                return false;
            }

            Player player = game.CurrentPlayer;

            if (clock != null && clock.IsExpired(player))
            {
                // The move came after the time was up, before the timer could report
                logger.LogWarning("Move rejected in match {MatchId}: {Reason}", Id, "time-expired");
                EndOnTime(player);
                accepted = false;
            }
            else
            {
                game.MakeMove(move);
                logger.LogDebug("Move played in match {MatchId} by {Player}: {MoveType}", Id, player.ToString(), move.Type.ToString());

                // The clock of the player who moved stops (and he gets the increment)
                // After the last move of the match there is nothing to start
                if (!game.IsGameOver() && clock != null && !clock.Press(player))
                    EndOnTime(player);      // the time ran out in the very moment of the move
                else
                {
                    Publish();
                    LogIfFinished();
                }

                accepted = true;
            }
        }

        Changed?.Invoke();
        return accepted;
    }

    public bool Resign(string userId)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.Resign");
        activity?.SetTag("chess.match_id", Id);

        lock (gate)
        {
            if (game == null || game.IsGameOver())
            {
                logger.LogDebug("Resignation ignored in match {MatchId}: The match is not running", Id);
                return false;
            }

            Player color = ColorOf(userId);

            if (color == Player.None)
            {
                logger.LogWarning("Resignation rejected in match {MatchId}: not a player of this match", Id);
                return false;
            }

            game.Resign(color);
            Publish();

            logger.LogInformation("{Player} resigned in match {MatchId}", color.ToString(), Id);
            LogIfFinished();
        }

        Changed?.Invoke();
        return true;
    }

    private void Start()
    {
        // 0 = the creator plays white, 1 = the creator plays black
        bool firstIsWhite = random.Next(2) == 0;

        whiteUserId = firstIsWhite ? firstUserId : secondUserId;
        blackUserId = firstIsWhite ? secondUserId : firstUserId;

        game = new GameState(Player.White, Board.Initial());

        if (TimeControl != null)
        {
            clock = new ChessClock(TimeControl, timeProvider);
            clock.FlagFell += OnFlagFell;
            clock.Start(Player.White);

            logger.LogInformation("Clock of match {MatchId} started: {TimeControl}", Id, TimeControl.Name);
        }

        Publish();

        logger.LogInformation("Match {MatchId} started", Id);
    }

    // Called by the timer of the clock, on another thread, when the time of a player is over
    private void OnFlagFell(Player flagged)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.Timeout");
        activity?.SetTag("chess.match_id", Id);

        lock (gate)
        {
            if (game == null || game.IsGameOver())
                return;

            EndOnTime(flagged);
        }

        Changed?.Invoke();
    }

    private void EndOnTime(Player flagged)
    {
        game.LoseOnTime(flagged);
        Publish();

        logger.LogInformation("{Player} ran out of time in match {MatchId}", flagged.ToString(), Id);
        LogIfFinished();
    }

    private bool IsTurnOf(string userId) => WhyNotTurnOf(userId) == null;

    /// <summary>null if the user may move, otherwise the reason (for the log)</summary>
    /// <param name="userId">The Id of the user</param>
    /// <returns>Returns a reason why it is not the turn of the player</returns>
    private string WhyNotTurnOf(string userId)
    {
        if (game == null) return "match-not-started";
        if (game.IsGameOver()) return "match-finished";

        Player color = ColorOf(userId);

        if (color == Player.None) return "not-a-player";
        if (color != game.CurrentPlayer) return "not-your-turn";

        return null;
    }

    private void Publish()
    {
        bool over = game.IsGameOver();

        // When the match is over the clock stops, and its last state goes into the snapshot
        if (over)
            clock?.Stop();

        MatchStatus status = over ? MatchStatus.Finished : MatchStatus.Running;
        snapshot = new MatchSnapshot(game.Board.Copy(), game.CurrentPlayer, game.Result, status, clock?.Snapshot());

        if (over)
            clock?.Dispose();
    }

    private void LogIfFinished()
    {
        if (snapshot.Status != MatchStatus.Finished)
            return;

        string result = snapshot.Result.Winner == Player.None ? "draw" : $"{snapshot.Result.Winner} wins";
        logger.LogInformation("Match {MatchId} finished: {Result} ({Reason})", Id, result, snapshot.Result.Reason.ToString());
    }
}
