using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.ViewModel;
using ChessUI.Shared.Services;

namespace ChessUI.Teams.Sessions;

/// <summary>
/// Placeholder for a local two-player-one-device-session, so it can be testes in the Browser
/// Will be replaced at a later date with OnlineGameSession
/// </summary>
public sealed class HotseatGameSession : IGameSession
{
    private readonly GameState game = new(Player.White, Board.Initial());

    public event Action Changed;

    public Board Board => game.Board;
    public Player CurrentPlayer => game.CurrentPlayer;
    public Player LocalPlayer => Player.White;
    public Result Result => game.Result;
    public bool IsGameOver => game.IsGameOver();
    public bool IsLocalTurn => !IsGameOver;

    public IEnumerable<Move> LegalMovesFor(Position pos) => game.LegalMovesForPiece(pos).ToList();

    public Task<bool> TryMakeMoveAsync(Move move)
    {
        game.MakeMove(move);
        Changed?.Invoke();
        return Task.FromResult(true);
    }
}
