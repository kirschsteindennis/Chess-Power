using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Pieces;
using ChessLogic.Services;

namespace ChessLogic.ViewModel;

public class AIGameState(Player humanPlayer, Board board, AIDifficulty difficulty, IRandomSource random = null) : GameState(humanPlayer, board)
{
    private readonly AIDifficulty difficulty = difficulty;
    private readonly IRandomSource random = random ?? new SystemRandomSource();

    public void MakeAIMove()
    {
        IEnumerable<Move> moves = AllLegalMovesFor(CurrentPlayer);

        if (!moves.Any()) return;

        Move move = difficulty switch
        {
            AIDifficulty.Easy => GetRandomMove(moves),
            AIDifficulty.Medium => GetIntermediateMove(moves),
            AIDifficulty.Hard => GetBestMove(moves),
            _ => GetRandomMove(moves)
        };

        MakeMove(move);
    }

    private Move GetRandomMove(IEnumerable<Move> moves)
    {
        return moves.ElementAt(random.Next(moves.Count()));
    }

    private Move GetIntermediateMove(IEnumerable<Move> moves)
    {
        var prioritizedMoves = moves.OrderByDescending(m =>
        {
            int score = 0;
            Piece piece = Board[m.FromPos];

            if (piece.Type == PieceType.Pawn)
                score += 1;

            return score;
        });

        return GetRandomMove(prioritizedMoves.Take(Math.Max(1, prioritizedMoves.Count() / 2)));
    }

    private Move GetBestMove(IEnumerable<Move> moves)
    {
        return moves.OrderByDescending(m =>
        {
            Board boardCopy = Board.Copy();
            m.Execute(boardCopy);
            return EvaluateBoard(boardCopy, CurrentPlayer);
        }).First();
    }

    private int EvaluateBoard(Board board, Player player)
    {
        int score = 0;

        foreach (Position pos in board.PiecePositionsFor(player))
            score += GetPieceValue(board[pos]);

        foreach (Position pos in board.PiecePositionsFor(player.Opponent()))
            score -= GetPieceValue(board[pos]);

        return score;
    }

    private int GetPieceValue(Piece piece)
    {
        return piece.Type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            PieceType.King => 100,
            _ => 0
        };
    }

    //private int EvaluateMove(Move move)
    //{
    //    int score = 0;

    //    if (move.IsCapture)
    //        score += (int)move.CapturedPiece.Type * 10;

    //    if (move.IsCheck(Board))
    //        score += 5;

    //    if (move is PawnPromotion)
    //        score += 9;

    //    Position kingPos = Board.FindKing(CurrentPlayer.Opponent()).Position;
    //    int distanceToKing = Math.Abs(move.ToPos.Row - kingPos.Row) +
    //        Math.Abs(move.ToPos.Column - kingPos.Column);
    //    score += 5 - distanceToKing;

    //    return score;
    //}
}
