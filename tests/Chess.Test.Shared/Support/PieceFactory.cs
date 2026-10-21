using ChessLogic.Enums;
using ChessLogic.Pieces;

namespace Chess.Test.Shared.Support;

public static class PieceFactory
{
    public static Piece Create(Player color, PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => new Pawn(color),
            PieceType.Knight => new Knight(color),
            PieceType.Bishop => new Bishop(color),
            PieceType.Rook => new Rook(color),
            PieceType.Queen => new Queen(color),
            PieceType.King => new King(color),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
