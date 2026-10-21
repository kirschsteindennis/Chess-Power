using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;

namespace ChessLogic.Pieces;

public class Rook : Piece
{
    public override PieceType Type => PieceType.Rook;
    public override Player Color { get; }

    private static readonly Direction[] dirs =
    [
        Direction.North,
        Direction.South,
        Direction.West,
        Direction.East,
    ];

    public Rook(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Rook copy = new(Color)
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
