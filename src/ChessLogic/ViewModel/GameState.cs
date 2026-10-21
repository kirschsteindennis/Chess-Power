using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.Pieces;

namespace ChessLogic.ViewModel;

public class GameState
{
    public Board Board { get; }
    public Player CurrentPlayer { get; private set; }
    public Result Result { get; private set; } = null;

    private int noCaptureOrPawnMoves = 0;
    private string stateString;
    private readonly Dictionary<string, int> stateHistory = [];

    /// <summary>
    /// Constructor for the class
    /// </summary>
    /// <param name="player">The Player of the game</param>
    /// <param name="board">The Board</param>
    /// <param name="halfMoveClock">Moves since the last capture or pawn move (for loading positions and tests)</param>
    public GameState(Player player, Board board, int halfMoveClock = 0)
    {
        CurrentPlayer = player;
        Board = board;
        noCaptureOrPawnMoves = halfMoveClock;

        stateString = new StateString(CurrentPlayer, board).ToString();
        stateHistory[stateString] = 1;
    }
    
    public IEnumerable<Move> LegalMovesForPiece(Position pos)
    {
        if (Board.IsEmpty(pos) || Board[pos].Color != CurrentPlayer)
            return [];



        Piece piece = Board[pos];
        IEnumerable<Move> moveCandidates = piece.GetMoves(pos, Board);
        return moveCandidates.Where(move => move.IsLegal(Board));
    }

    public void MakeMove(Move move)
    {
        Board.SetPawnSkipPosition(CurrentPlayer, null);

        if (move.Execute(Board))
        {
            noCaptureOrPawnMoves = 0;
            stateHistory.Clear();
        }
        else
            noCaptureOrPawnMoves++;

        CurrentPlayer = CurrentPlayer.Opponent();
        UpdateStateString();
        CheckForGameOver();
    }

    public IEnumerable<Move> AllLegalMovesFor(Player player)
    {
        IEnumerable<Move> moveCandidates = Board.PiecePositionsFor(player).SelectMany(pos =>
        {
            Piece piece = Board[pos];
            return piece.GetMoves(pos, Board);
        });

        return moveCandidates.Where(move => move.IsLegal(Board));
    }

    private void CheckForGameOver()
    {
        if (!AllLegalMovesFor(CurrentPlayer).Any())
            if (Board.IsInCheck(CurrentPlayer))
                Result = Result.Win(CurrentPlayer.Opponent());
            else
                Result = Result.Draw(EndReason.Stalemate);
        else if (Board.InsufficientMaterial())
            Result = Result.Draw(EndReason.InsufficientMaterial);
        else if (FiftyMoveRule())
            Result = Result.Draw(EndReason.FiftyMoveRule);
        else if (ThreefoldRepetition())
            Result = Result.Draw(EndReason.ThreefoldRepetition);
    }

    public bool IsGameOver()
    {
        return Result != null;
    }

    public void Resign(Player player)
    {
        if (IsGameOver())
            return;

        Result = Result.Resignation(player.Opponent());
    }

    private bool FiftyMoveRule()
    {
        int fullMoves = noCaptureOrPawnMoves / 2;
        return fullMoves == 50;
    }

    private void UpdateStateString()
    {
        stateString = new StateString(CurrentPlayer, Board).ToString();

        if (!stateHistory.ContainsKey(stateString))
            stateHistory[stateString] = 1;
        else
            stateHistory[stateString]++;
    }

    private bool ThreefoldRepetition()
    {
        return stateHistory[stateString] == 3;
    }
}
