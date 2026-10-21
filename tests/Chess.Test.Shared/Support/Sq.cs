using ChessLogic.Model;

namespace Chess.Test.Shared.Support;

/// <summary>
/// Converts chess notations to the coordinates the game uses.
/// Row 0 is rank 8 and column 0 is file a, so "e2" is row 6, column 4
/// </summary>
public static class Sq
{
    public static Position At(string square)
    {
        return new Position(8 - (square[1] - '0'), square[0] - 'a');
    }

    public static string Name(Position pos)
    {
        return $"{(char)('a' + pos.Column)}{8 - pos.Row}";
    }
}
