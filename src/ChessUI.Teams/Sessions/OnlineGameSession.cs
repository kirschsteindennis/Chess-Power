using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessOnline;
using ChessUI.Shared.Services;

namespace ChessUI.Teams.Sessions;

/// <summary>
/// The view of ONE player on a shared match. Every player has their own session object
/// </summary>
public sealed class OnlineGameSession : IGameSession, IDisposable
{
    private readonly IChessMatch match;
    private readonly string userId;

    public OnlineGameSession(IChessMatch match, string userId)
    {
        this.match = match;
        this.userId = userId;
        match.Changed += OnMatchChanged;
    }

    public event Action Changed;

    /// <summary>False while we still wait for an opponent</summary>
    public bool HasStarted => match.Snapshot.Status != MatchStatus.Waiting;

    public Board Board => match.Snapshot.Board;
    public Player CurrentPlayer => match.Snapshot.CurrentPlayer;
    public Player LocalPlayer => match.ColorOf(userId);
    public Result Result => match.Snapshot.Result;
    public bool IsGameOver => match.Snapshot.Status == MatchStatus.Finished;
    public bool IsLocalTurn => HasStarted && !IsGameOver && CurrentPlayer == LocalPlayer;
    public bool CanResign => HasStarted && !IsGameOver;

    public IEnumerable<Move> LegalMovesFor(Position pos) => match.LegalMovesFor(userId, pos);

    public Task<bool> TryMakeMoveAsync(Move move) => Task.FromResult(match.TryMakeMove(userId, move));

    public bool Resign() => match.Resign(userId);

    private void OnMatchChanged() => Changed?.Invoke();

    public void Dispose() => match.Changed -= OnMatchChanged;
}
