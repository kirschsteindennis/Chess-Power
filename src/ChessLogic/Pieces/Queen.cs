using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;

namespace ChessLogic.Pieces;

public class Queen : Piece
{
    public override PieceType Type => PieceType.Queen;
    public override Player Color { get; }

    private static readonly Direction[] dirs =
    [
        Direction.North,
        Direction.South,
        Direction.West,
        Direction.East,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest
    ];

    public Queen(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Queen copy = new(Color)
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
