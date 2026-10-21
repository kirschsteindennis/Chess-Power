using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Pieces;

namespace ChessLogic.Moves;

public class PawnPromotion(Position from, Position to, PieceType type) : Move
{
    public override MoveType Type => MoveType.PawnPromotion;
    public override Position FromPos { get; } = from;
    public override Position ToPos { get; } = to;

    private readonly PieceType newType = type;

    private Piece CreatePromotionPiece(Player color)
    {
        return newType switch
        {
            PieceType.Knight => new Knight(color),
            PieceType.Bishop => new Bishop(color),
            PieceType.Rook => new Rook(color),
            _ => new Queen(color)
        };
    }

    public override bool Execute(Board board)
    {
        Piece pawn = board[FromPos];
        board[FromPos] = null;

        Piece promotionPiece = CreatePromotionPiece(pawn.Color);
        promotionPiece.HasMoved = true;
        board[ToPos] = promotionPiece;

        return true;
    }
}
