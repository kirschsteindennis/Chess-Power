using ChessLogic.Enums;
using ChessLogic.Model;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Pieces;

[TestFixture]
public class KnightTests
{
    [Test]
    public void GetMoves_InTheCenter_HasEightMoves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Knight, "e4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e4");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["c3", "c5", "d2", "d6", "f2", "f6", "g3", "g5"]));
    }

    [Test]
    public void GetMoves_InACorner_HasTwoMoves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Knight, "a1").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["b3", "c2"]));
    }

    [Test]
    public void GetMoves_CannotLandOnOwnPieces()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Knight, "a1")
            .White(PieceType.Pawn, "b3")
            .White(PieceType.Pawn, "c2")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.Empty);
    }

    [Test]
    public void GetMoves_CanCaptureEnemyPieces()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Knight, "a1")
            .Black(PieceType.Pawn, "b3")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Does.Contain("b3"));
    }

    [Test]
    public void GetMoves_JumpsOverOtherPieces()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "b1");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["a3", "c3"]));
    }
}
