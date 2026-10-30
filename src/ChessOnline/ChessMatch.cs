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
public sealed class ChessMatch(string _creatorUserId, IRandomSource _random, ILogger _logger = null) : IChessMatch
{
    private readonly object gate = new();
    private readonly IRandomSource random = _random;
    private readonly ILogger logger = _logger ?? NullLogger.Instance;
    private readonly string firstUserId = _creatorUserId;
    private string secondUserId;
    private string whiteUserId;
    private string blackUserId;
    private GameState game;
    private volatile MatchSnapshot snapshot = new(null, Player.None, null, MatchStatus.Waiting);

    /// <summary>A random number, that says nothing about the players. It connects all log entries of this match</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];

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
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.MakeMove");
        activity?.SetTag("chess.match_id", Id);

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
            game.MakeMove(move);
            Publish();

            logger.LogDebug("Move played in match {MatchId} by {Player}: {MoveType}", Id, player.ToString(), move.Type.ToString());
            LogIfFinished();
        }

        Changed?.Invoke();
        return true;
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
        Publish();

        logger.LogInformation("Match {MatchId} started", Id);
    }

    private bool IsTurnOf(string userId)
    {
        return game != null && !game.IsGameOver() && ColorOf(userId) == game.CurrentPlayer;
    }

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
        MatchStatus status = game.IsGameOver() ? MatchStatus.Finished : MatchStatus.Running;
        snapshot = new MatchSnapshot(game.Board.Copy(), game.CurrentPlayer, game.Result, status);
    }

    private void LogIfFinished()
    {
        if (snapshot.Status != MatchStatus.Finished)
            return;

        string result = snapshot.Result.Winner == Player.None ? "draw" : $"{snapshot.Result.Winner} wins";
        logger.LogInformation("Match {MatchId} finished: {Result} ({Reason})", Id, result, snapshot.Result.Reason.ToString());
    }
}
