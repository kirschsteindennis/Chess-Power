using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Pieces;

[TestFixture]
public class PawnTests
{
    [Test]
    public void WhitePawn_OnItsStartRow_CanMoveOneOrTwoSquares()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "e2").Build();

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e2");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moves.Select(m => Sq.Name(m.ToPos)), Is.EquivalentTo(["e3", "e4"]));
            Assert.That(moves.Any(m => m.Type == MoveType.DoublePawn), Is.True);
        }
    }

    [Test]
    public void BlackPawn_MovesDownTheBoard()
    {
        // Arrange
        Board board = new BoardBuilder().Black(PieceType.Pawn, "e7").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e7");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e6", "e5"]));
    }

    [Test]
    public void Pawn_AwayFromItsStartRow_CanOnlyMoveOneSquare()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "e4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e4");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e5"]));
    }

    [Test]
    public void Pawn_BlockedDirectlyAhead_HasNoForwardMoves()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e2")
            .Black(PieceType.Pawn, "e3")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e2");

        // Assert
        Assert.That(destinations, Is.Empty);
    }

    [Test]
    public void Pawn_WithTheSecondSquareBlocked_CanOnlyMoveOne()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e2")
            .Black(PieceType.Knight, "e4")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e2");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e3"]));
    }

    [Test]
    public void Pawn_CapturesDiagonally_ButNotItsOwnPieces()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e2")
            .Black(PieceType.Knight, "d3")
            .White(PieceType.Knight, "f3")
            .Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e2");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e3", "e4", "d3"]));
    }

    [Test]
    public void Pawn_OnTheLastRow_OffersFourPromotions()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.Pawn, "a7").Build();

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "a7");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moves.Count, Is.EqualTo(4));
            Assert.That(moves.All(m => m.Type == MoveType.PawnPromotion), Is.True);
            Assert.That(moves.All(m => m.ToPos == Sq.At("a8")), Is.True);
        }
    }

    [Test]
    public void Pawn_PromotingByCapture_OffersFourPromotionsPerTarget()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "a7")
            .Black(PieceType.Rook, "b8")
            .Build();

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "a7");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moves.Count, Is.EqualTo(8));
            Assert.That(moves.Count(m => m.ToPos == Sq.At("b8")), Is.EqualTo(4));
        }
    }

    [Test]
    public void Pawn_OffersEnPassant_WhenTheSkippedSquareIsDiagonallyAhead()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e5")
            .Black(PieceType.Pawn, "d5")
            .Build();
        board.SetPawnSkipPosition(Player.Black, Sq.At("d6"));

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e5");

        // Assert
        Assert.That(moves.Any(m => m.Type == MoveType.EnPassant && m.ToPos == Sq.At("d6")), Is.True);
    }

    [Test]
    public void CanCaputreOpponentKing_WhenTheKingIsDiagonallyAhead_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e4")
            .Black(PieceType.King, "d5")
            .Build();

        // Act
        bool attacks = board[Sq.At("e4")].CanCaputreOpponentKing(Sq.At("e4"), board);

        // Assert
        Assert.That(attacks, Is.True);
    }

    [Test]
    public void CanCaputreOpponentKing_WhenTheKingIsStraightAhead_IsFalse()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.Pawn, "e4")
            .Black(PieceType.King, "e5")
            .Build();

        // Act
        bool attacks = board[Sq.At("e4")].CanCaputreOpponentKing(Sq.At("e4"), board);

        // Assert
        Assert.That(attacks, Is.False);
    }
}
