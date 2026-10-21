using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Pieces;

[TestFixture]
public class KingTests
{
    private static Board KingWithBothRooks()
    {
        return new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Rook, "a1")
            .White(PieceType.Rook, "h1")
            .Build();
    }

    [Test]
    public void GetMoves_InTheCenter_HasEightMoves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.King, "e4").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "e4");

        // Assert
        Assert.That(destinations, Has.Count.EqualTo(8));
    }

    [Test]
    public void GetMoves_InACorner_HasThreeMoves()
    {
        // Arrange
        Board board = new BoardBuilder().White(PieceType.King, "a1").Build();

        // Act
        List<string> destinations = MoveHelper.Destinations(board, "a1");

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["a2", "b1", "b2"]));
    }

    [Test]
    public void GetMoves_WithBothRooksUnmoved_OffersBothCastlingMoves()
    {
        // Arrange
        Board board = KingWithBothRooks();

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e1");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(moves.Any(m => m.Type == MoveType.CastleKS), Is.True);
            Assert.That(moves.Any(m => m.Type == MoveType.CastleQS), Is.True);
            Assert.That(moves.Count, Is.EqualTo(7));
        });
    }

    [TestCase("f1")]
    [TestCase("g1")]
    public void GetMoves_CastlingKingSide_IsNotOffered_WhenAPieceIsInTheWay(string blockingSquare)
    {
        // Arrange
        Board board = KingWithBothRooks();
        board[Sq.At(blockingSquare)] = PieceFactory.Create(Player.White, PieceType.Knight);

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e1");

        // Assert
        Assert.That(moves.Any(m => m.Type == MoveType.CastleKS), Is.False);
    }

    [TestCase("b1")]
    [TestCase("c1")]
    [TestCase("d1")]
    public void GetMoves_CastlingQueenSide_IsNotOffered_WhenAPieceIsInTheWay(string blockingSquare)
    {
        // Arrange: all three squares between king and rook must be free
        Board board = KingWithBothRooks();
        board[Sq.At(blockingSquare)] = PieceFactory.Create(Player.White, PieceType.Knight);

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e1");

        // Assert
        Assert.That(moves.Any(m => m.Type == MoveType.CastleQS), Is.False);
    }

    [Test]
    public void GetMoves_NoCastling_WhenTheKingHasMoved()
    {
        // Arrange
        Board board = KingWithBothRooks();
        board[Sq.At("e1")].HasMoved = true;

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e1");

        // Assert
        Assert.That(moves.Any(m => m.Type is MoveType.CastleKS or MoveType.CastleQS), Is.False);
    }

    [Test]
    public void GetMoves_NoCastlingWithARookThatHasMoved()
    {
        // Arrange
        Board board = KingWithBothRooks();
        board[Sq.At("h1")].HasMoved = true;

        // Act
        List<Move> moves = MoveHelper.MovesOf(board, "e1");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moves.Any(m => m.Type == MoveType.CastleKS), Is.False);
            Assert.That(moves.Any(m => m.Type == MoveType.CastleQS), Is.True);
        }
    }

    [Test]
    public void CanCaputreOpponentKing_WhenTheKingsAreNeighbours_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e4")
            .Black(PieceType.King, "e5")
            .Build();

        // Act
        bool attacks = board[Sq.At("e4")].CanCaputreOpponentKing(Sq.At("e4"), board);

        // Assert
        Assert.That(attacks, Is.True);
    }
}
