using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Pieces;

namespace Chess.Test.Shared.Support;

/// <summary>
/// Builds small test positions, e.g.
/// new BoardBuilder().White(PieceType.King, "e1").Black(PieceType.King, "e8").Build();
/// </summary>
public sealed class BoardBuilder
{
    private readonly Board board = new();

    public BoardBuilder White(PieceType type, string square) => With(Player.White, type, square);

    public BoardBuilder Black(PieceType type, string square) => With(Player.Black, type, square);

    public BoardBuilder With(Player color, PieceType type, string square)
    {
        Position pos = Sq.At(square);
        Piece piece = PieceFactory.Create(color, type);

        // A pawn away from its start row has moved already, otherwise it could jump two squares
        if (type == PieceType.Pawn)
            piece.HasMoved = pos.Row != (color == Player.White ? 6 : 1);

        board[pos] = piece;
        return this;
    }

    public Board Build() => board;
}
