using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Moves;

[TestFixture]
public class SpecialPawnMovesTests
{
    #region Double Pawn

    [Test]
    public void DoublePawn_Execute_MovesThePawnAndRemembersTheSkippedSquare()
    {
        // Arrange
        Board board = Board.Initial();
        DoublePawn move = new(Sq.At("e2"), Sq.At("e4"));

        // Act
        bool resetsCounter = move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("e4")].Type, Is.EqualTo(PieceType.Pawn));
            Assert.That(board[Sq.At("e2")], Is.Null);
            Assert.That(board.GetPawnSkipPosition(Player.White), Is.EqualTo(Sq.At("e3")));
            Assert.That(resetsCounter, Is.True);
        }
    }

    [Test]
    public void DoublePawn_Execute_RemembersTheSquareForTheMovingColorOnly()
    {
        // Arrange
        Board board = Board.Initial();
        DoublePawn move = new(Sq.At("d7"), Sq.At("d5"));

        // Act
        move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board.GetPawnSkipPosition(Player.Black), Is.EqualTo(Sq.At("d6")));
            Assert.That(board.GetPawnSkipPosition(Player.White), Is.Null);
        }
    }

    #endregion

    #region En Passant

    [Test]
    public void EnPassant_Execute_RemovesTheCapturedPawn()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e5")
            .Black(PieceType.Pawn, "d5")
            .Build();
        EnPassant move = new(Sq.At("e5"), Sq.At("d6"));

        // Act
        move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("d6")].Color, Is.EqualTo(Player.White));
            Assert.That(board[Sq.At("e5")], Is.Null);
            Assert.That(board[Sq.At("d5")], Is.Null);
        }
    }

    #endregion

    #region Pawn Promotion
    
    [TestCase(PieceType.Queen)]
    [TestCase(PieceType.Rook)]
    [TestCase(PieceType.Bishop)]
    [TestCase(PieceType.Knight)]
    public void PawnPromotion_Execute_CreatesTheChosenPiece(PieceType chosen)
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "a7").Build();
        PawnPromotion move = new(Sq.At("a7"), Sq.At("a8"), chosen);

        // Act
        move.Execute(board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(board[Sq.At("a7")], Is.Null);
            Assert.That(board[Sq.At("a8")].Type, Is.EqualTo(chosen));
            Assert.That(board[Sq.At("a8")].Color, Is.EqualTo(Player.White));
            Assert.That(board[Sq.At("a8")].HasMoved, Is.True);
        }
    }

    [Test]
    public void PawnPromotion_Execute_AnUnsupportedTypeBecomesAQueen()
    {
        // Arrange
        Board board = new BoardBuilder().Black(PieceType.Pawn, "h2").Build();
        PawnPromotion move = new(Sq.At("h2"), Sq.At("h1"), PieceType.King);

        // Act
        move.Execute(board);

        // Assert
        Assert.That(board[Sq.At("h1")].Type, Is.EqualTo(PieceType.Queen));
    }

    [Test]
    public void PawnPromotion_Execute_ResetsTheFiftyMoveCounter()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "a7").Build();
        PawnPromotion move = new(Sq.At("a7"), Sq.At("a8"), PieceType.Queen);

        // Act
        bool resetsCounter = move.Execute(board);

        // Assert
        Assert.That(resetsCounter, Is.True);
    }

    #endregion
}