using ChessLogic.Enums;
using ChessLogic.Model;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Pieces;

[TestFixture]
public class SlidingPiecesTests
{
    [Test]
    public void Bishop_OnAnEmptyBoard_MovesAlongBothDiagonals()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Bishop, "d4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "d4");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(
        [
            "a1", "b2", "c3", "e5", "f6", "g7", "h8",
            "a7", "b6", "c5", "e3", "f2", "g1"
        ]));
    }

    [Test]
    public void Bishop_StopsInFrontOfAnOwnPiece()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Bishop, "a1")
            .White(PieceType.Pawn, "c3")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["b2"]));
    }

    [Test]
    public void Bishop_CapturesAnEnemyPieceAndStopsThere()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Bishop, "a1")
            .Black(PieceType.Pawn, "c3")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["b2", "c3"]));
    }

    [Test]
    public void Rook_OnAnEmptyBoard_Has14Moves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Rook, "d4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "d4");

        // Assert
        Assert.That(destinations, Has.Count.EqualTo(14));
    }

    [Test]
    public void Rook_MovesOnlyStraight()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Rook, "a1").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations.All(square => square[0] == 'a' || square[1] == '1'), Is.True);
    }

    [Test]
    public void Rook_InTheInitialPosition_HasNoMoves()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.Empty);
    }

    [Test]
    public void Queen_OnAnEmptyBoard_Has27Moves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Queen, "d4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "d4");

        // Assert
        Assert.That(destinations, Has.Count.EqualTo(27));
    }

    [Test]
    public void Queen_CannotJumpOverPieces()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Queen, "a1")
            .White(PieceType.Pawn, "a2")
            .White(PieceType.Pawn, "b1")
            .White(PieceType.Pawn, "b2")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.Empty);
    }
}
