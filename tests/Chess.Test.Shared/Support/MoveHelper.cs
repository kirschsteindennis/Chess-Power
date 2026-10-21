using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.ViewModel;

namespace Chess.Test.Shared.Support;

public static class MoveHelper
{
    /// <summary>The squares a piece could move to, ignoring check (pseudo-legal moves).</summary>
    public static List<string> Destinations(Board board, string from)
    {
        Position pos = Sq.At(from);
        return board[pos].GetMoves(pos, board).Select(m => Sq.Name(m.ToPos)).ToList();
    }

    /// <summary>All pseudo-legal moves of a piece.</summary>
    public static List<Move> MovesOf(Board board, string from)
    {
        Position pos = Sq.At(from);
        return board[pos].GetMoves(pos, board).ToList();
    }

    /// <summary>Plays the first legal move from one square to another, like two clicks on the board.</summary>
    public static void Play(GameState game, string from, string to)
    {
        Move move = game.LegalMovesForPiece(Sq.At(from)).FirstOrDefault(m => m.ToPos == Sq.At(to));

        Assert.That(move, Is.Not.Null, $"{from}-{to} should be a legal move");

        game.MakeMove(move);
    }

    /// <summary>Plays several moves written like "e2e4".</summary>
    public static void PlayAll(GameState game, params string[] moves)
    {
        foreach (string move in moves)
            Play(game, move[..2], move[2..4]);
    }
}
