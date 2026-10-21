using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;

namespace ChessLogic.Pieces;

public class Bishop : Piece
{
    public override PieceType Type => PieceType.Bishop;
    public override Player Color { get; }

    private static readonly Direction[] dirs =
    [
        Direction.NorthWest,
        Direction.NorthEast,
        Direction.SouthWest,
        Direction.SouthEast
    ];

    public Bishop(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Bishop copy = new(Color)
        {
            HasMoved = HasMoved
        };
        return copy;
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        return MovePositionsInDir(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
