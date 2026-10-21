using ChessLogic.Enums;
using ChessLogic.Model;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Moves;

[TestFixture]
public class CastleTests
{
    private static BoardBuilder WhiteKingWithRooks()
    {
        return new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Rook, "a1")
            .White(PieceType.Rook, "h1")
            .Black(PieceType.King, "e8");
    }

    [Test]
    public void Execute_KingSide_MovesKingAndRook()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        castle.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("g1")].Type, Is.EqualTo(PieceType.King));
            Assert.That(board[Sq.At("f1")].Type, Is.EqualTo(PieceType.Rook));
            Assert.That(board[Sq.At("e1")], Is.Null);
            Assert.That(board[Sq.At("h1")], Is.Null);
        }
    }

    [Test]
    public void Execute_QueenSide_MovesKingAndRook()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleQS, Sq.At("e1"));

        // Act
        castle.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("c1")].Type, Is.EqualTo(PieceType.King));
            Assert.That(board[Sq.At("d1")].Type, Is.EqualTo(PieceType.Rook));
            Assert.That(board[Sq.At("e1")], Is.Null);
            Assert.That(board[Sq.At("a1")], Is.Null);
        }
    }

    [Test]
    public void Execute_DoesNotResetTheFiftyMoveCounter()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        bool resetsCounter = castle.Execute(board);

        // Assert
        Assert.That(resetsCounter, Is.False);
    }

    [Test]
    public void Constructor_SetsTheKingsStartAndTargetSquare()
    {
        // Arrange
        Position kingPos = Sq.At("e8");

        // Act
        ChessLogic.Moves.Castle castle = new(MoveType.CastleQS, kingPos);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(castle.FromPos, Is.EqualTo(Sq.At("e8")));
            Assert.That(castle.ToPos, Is.EqualTo(Sq.At("c8")));
        }
    }

    [Test]
    public void IsLegal_WhenNothingIsAttacked_IsTrue()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        bool legal = castle.IsLegal(board);

        // Assert
        Assert.That(legal, Is.True);
    }

    [Test]
    public void IsLegal_WhenTheKingIsInCheck_IsFalse()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Black(PieceType.Rook, "e5").Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        bool legal = castle.IsLegal(board);

        // Assert
        Assert.That(legal, Is.False);
    }

    [Test]
    public void IsLegal_WhenTheKingPassesAnAttackedSquare_IsFalse()
    {
        // Arrange: the black rook attacks f1, which the king crosses
        Board board = WhiteKingWithRooks().Black(PieceType.Rook, "f8").Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        bool legal = castle.IsLegal(board);

        // Assert
        Assert.That(legal, Is.False);
    }

    [Test]
    public void IsLegal_WhenTheKingEndsOnAnAttackedSquare_IsFalse()
    {
        // Arrange: g1 is attacked
        Board board = WhiteKingWithRooks().Black(PieceType.Rook, "g8").Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        bool legal = castle.IsLegal(board);

        // Assert
        Assert.That(legal, Is.False);
    }

    [Test]
    public void IsLegal_QueenSide_WhenOnlyTheRooksSquareIsAttacked_IsTrue()
    {
        // Arrange: b1 is attacked, but the king never crosses it
        Board board = WhiteKingWithRooks().Black(PieceType.Rook, "b8").Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleQS, Sq.At("e1"));

        // Act
        bool legal = castle.IsLegal(board);

        // Assert
        Assert.That(legal, Is.True);
    }

    [Test]
    public void IsLegal_DoesNotChangeTheRealBoard()
    {
        // Arrange
        Board board = WhiteKingWithRooks().Build();
        ChessLogic.Moves.Castle castle = new(MoveType.CastleKS, Sq.At("e1"));

        // Act
        castle.IsLegal(board);

        // Assert
        Assert.That(board[Sq.At("e1")].Type, Is.EqualTo(PieceType.King));
    }
}