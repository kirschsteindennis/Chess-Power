using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Services;
using ChessLogic.ViewModel;

namespace ChessOnline;

/// <summary>
/// One match between two people. Every access to the GameState happens inside the lock,
/// because both players use the same match from different connections at the same time
/// </summary>
public sealed class ChessMatch(string creatorUserId, IRandomSource random) : IChessMatch
{
    private readonly object gate = new();
    private readonly IRandomSource random = random;
    private readonly string firstUserId = creatorUserId;
    private string secondUserId;
    private string whiteUserId;
    private string blackUserId;
    private GameState game;
    private volatile MatchSnapshot snapshot = new(null, Player.None, null, MatchStatus.Waiting);

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
        lock (gate)
        {
            if (!IsTurnOf(userId))
                return false;

            // The server decides: The move must be legal
            bool isLegal = game.LegalMovesForPiece(move.FromPos)
                .Any(m => m.ToPos == move.ToPos && m.Type == move.Type);

            if (!isLegal)
                return false;

            game.MakeMove(move);
            Publish();
        }

        Changed?.Invoke();
        return true;
    }

    public bool Resign(string userId)
    {
        lock (gate)
        {
            if (game == null || game.IsGameOver())
                return false;

            Player color = ColorOf(userId);

            if (color == Player.None)
                return false;

            game.Resign(color);
            Publish();
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
    }

    private bool IsTurnOf(string userId)
    {
        return game != null && !game.IsGameOver() && ColorOf(userId) == game.CurrentPlayer;
    }

    private void Publish()
    {
        MatchStatus status = game.IsGameOver() ? MatchStatus.Finished : MatchStatus.Running;
        snapshot = new MatchSnapshot(game.Board.Copy(), game.CurrentPlayer, game.Result, status);
    }
}
