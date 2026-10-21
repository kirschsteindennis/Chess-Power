using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Moves;

[TestFixture]
public class NormalMoveTests
{
    [Test]
    public void Execute_MovesThePieceAndMarksItAsMoved()
    {
        // Arrange
        Board board = Board.Initial();
        NormalMove move = new(Sq.At("g1"), Sq.At("f3"));

        // Act
        move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("g1")], Is.Null);
            Assert.That(board[Sq.At("f3")].Type, Is.EqualTo(PieceType.Knight));
            Assert.That(board[Sq.At("f3")].HasMoved, Is.True);
        }
    }

    [Test]
    public void Execute_ACaptureReplacesTheOtherPiece()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Rook, "a1")
            .Black(PieceType.Knight, "a5")
            .Build();
        NormalMove move = new(Sq.At("a1"), Sq.At("a5"));

        // Act
        move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("a5")].Color, Is.EqualTo(Player.White));
            Assert.That(board.CountPieces().TotalCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void Execute_ACapture_ResetsTheFiftyMoveCounter()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Rook, "a1")
            .Black(PieceType.Knight, "a5")
            .Build();
        NormalMove move = new(Sq.At("a1"), Sq.At("a5"));

        // Act
        bool resetsCounter = move.Execute(board);

        // Assert
        Assert.That(resetsCounter, Is.True);
    }

    [Test]
    public void Execute_APawnMove_ResetsTheFiftyMoveCounter()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "e4").Build();
        NormalMove move = new(Sq.At("e4"), Sq.At("e5"));

        // Act
        bool resetsCounter = move.Execute(board);

        // Assert
        Assert.That(resetsCounter, Is.True);
    }

    [Test]
    public void Execute_AQuietPieceMove_DoesNotResetTheFiftyMoveCounter()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Knight, "g1").Build();
        NormalMove move = new(Sq.At("g1"), Sq.At("f3"));

        // Act
        bool resetsCounter = move.Execute(board);

        // Assert
        Assert.That(resetsCounter, Is.False);
    }

    [Test]
    public void IsLegal_AMoveThatLeavesTheOwnKingInCheck_IsFalse()
    {
        // Arrange: the white rook on e2 is pinned against the king by the black rook
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Rook, "e2")
            .Black(PieceType.Rook, "e8")
            .Black(PieceType.King, "a8")
            .Build();
        NormalMove move = new(Sq.At("e2"), Sq.At("a2"));

        // Act
        bool legal = move.IsLegal(board);

        // Assert
        Assert.That(legal, Is.False);
    }

    [Test]
    public void IsLegal_AMoveAlongThePin_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Rook, "e2")
            .Black(PieceType.Rook, "e8")
            .Black(PieceType.King, "a8")
            .Build();
        NormalMove move = new(Sq.At("e2"), Sq.At("e5"));

        // Act
        bool legal = move.IsLegal(board);

        // Assert
        Assert.That(legal, Is.True);
    }

    [Test]
    public void IsLegal_DoesNotChangeTheRealBoard()
    {
        // Arrange
        Board board = Board.Initial();
        NormalMove move = new(Sq.At("g1"), Sq.At("f3"));

        // Act
        move.IsLegal(board);

        // Assert
        Assert.That(board[Sq.At("g1")], Is.Not.Null);
    }
}