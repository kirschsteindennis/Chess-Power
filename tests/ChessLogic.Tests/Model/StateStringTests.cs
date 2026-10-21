using ChessLogic.Enums;
using ChessLogic.Model;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class StateStringTests
{
    [Test]
    public void ToString_InitialPosition_IsTheStartingFen()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        string state = new StateString(Player.White, board).ToString();

        // Assert
        Assert.That(state, Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -"));
    }

    [Test]
    public void ToString_WithBlackToMove_UsesB()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        string state = new StateString(Player.Black, board).ToString();

        // Assert
        Assert.That(state, Does.Contain(" b KQkq"));
    }

    [Test]
    public void ToString_WithoutCastlingRights_UsesADash()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .Build();

        // Act
        string state = new StateString(Player.White, board).ToString();

        // Assert
        Assert.That(state, Is.EqualTo("4k3/8/8/8/8/8/8/4K3 w - -"));
    }

    [Test]
    public void ToString_WithAnEnPassantChance_NamesTheSkippedSquare()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "e5")
            .Black(PieceType.Pawn, "d5")
            .Build();
        board.SetPawnSkipPosition(Player.Black, Sq.At("d6"));

        // Act
        string state = new StateString(Player.White, board).ToString();

        // Assert
        Assert.That(state, Is.EqualTo("4k3/8/8/3pP3/8/8/8/4K3 w - d6"));
    }

    [Test]
    public void ToString_SamePositionTwice_GivesTheSameString()
    {
        // Arrange
        Board first = Board.Initial();
        Board second = Board.Initial();

        // Act
        string firstState = new StateString(Player.White, first).ToString();
        string secondState = new StateString(Player.White, second).ToString();

        // Assert
        Assert.That(firstState, Is.EqualTo(secondState));
    }
}
