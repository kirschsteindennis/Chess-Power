using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Pieces;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class BoardTests
{
    #region Initial Position

    [Test]
    public void Initial_Has32Pieces()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        int total = board.CountPieces().TotalCount;

        // Assert
        Assert.That(total, Is.EqualTo(32));
    }

    [TestCase("a1", PieceType.Rook, Player.White)]
    [TestCase("b1", PieceType.Knight, Player.White)]
    [TestCase("c1", PieceType.Bishop, Player.White)]
    [TestCase("d1", PieceType.Queen, Player.White)]
    [TestCase("e1", PieceType.King, Player.White)]
    [TestCase("e2", PieceType.Pawn, Player.White)]
    [TestCase("a8", PieceType.Rook, Player.Black)]
    [TestCase("d8", PieceType.Queen, Player.Black)]
    [TestCase("e8", PieceType.King, Player.Black)]
    [TestCase("h7", PieceType.Pawn, Player.Black)]
    public void Initial_PutsThePiecesOnTheirStartSquares(string square, PieceType type, Player color)
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        Piece piece = board[Sq.At(square)];

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(piece, Is.Not.Null);
            Assert.That(piece.Type, Is.EqualTo(type));
            Assert.That(piece.Color, Is.EqualTo(color));
        }
    }

    [Test]
    public void Initial_LeavesTheMiddleOfTheBoardEmpty()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        int piecesInTheMiddle = board.PiecePositions().Count(pos => pos.Row is >= 2 and <= 5);

        // Assert
        Assert.That(piecesInTheMiddle, Is.EqualTo(0));
    }

    [Test]
    public void PiecePositionsFor_ReturnsOnlyThePiecesOfThatColor()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        List<Position> white = board.PiecePositionsFor(Player.White).ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(white.Count, Is.EqualTo(16));
            Assert.That(white.All(pos => board[pos].Color == Player.White), Is.True);
        }
    }

    [Test]
    public void CountPieces_CountsByColorAndType()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        Counting counting = board.CountPieces();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counting.White(PieceType.Pawn), Is.EqualTo(8));
            Assert.That(counting.Black(PieceType.Queen), Is.EqualTo(1));
            Assert.That(counting.Black(PieceType.Knight), Is.EqualTo(2));
        }
    }

    #endregion

    #region Squares

    [TestCase(0, 0, true)]
    [TestCase(7, 7, true)]
    [TestCase(-1, 0, false)]
    [TestCase(0, -1, false)]
    [TestCase(8, 3, false)]
    [TestCase(3, 8, false)]
    public void IsInside_ChecksTheBoardBorders(int row, int column, bool expected)
    {
        // Arrange
        Position pos = new(row, column);

        // Act
        bool inside = Board.IsInside(pos);

        // Assert
        Assert.That(inside, Is.EqualTo(expected));
    }

    [Test]
    public void IsEmpty_DistinguishesOccupiedAndFreeSquares()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        bool e2Empty = board.IsEmpty(Sq.At("e2"));
        bool e4Empty = board.IsEmpty(Sq.At("e4"));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(e2Empty, Is.False);
            Assert.That(e4Empty, Is.True);
        }
    }

    #endregion

    #region Check

    [Test]
    public void IsInCheck_InitialPosition_IsFalseForBoth()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        bool white = board.IsInCheck(Player.White);
        bool black = board.IsInCheck(Player.Black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(white, Is.False);
            Assert.That(black, Is.False);
        }
    }

    [Test]
    public void IsInCheck_RookOnTheOpenFile_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "e8")
            .White(PieceType.Rook, "e1")
            .White(PieceType.King, "a1")
            .Build();

        // Act
        bool blackInCheck = board.IsInCheck(Player.Black);
        bool whiteInCheck = board.IsInCheck(Player.White);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(blackInCheck, Is.True);
            Assert.That(whiteInCheck, Is.False);
        }
    }

    [Test]
    public void IsInCheck_WhenAPieceBlocksTheLine_IsFalse()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "e8")
            .Black(PieceType.Pawn, "e7")
            .White(PieceType.Rook, "e1")
            .White(PieceType.King, "a1")
            .Build();

        // Act
        bool inCheck = board.IsInCheck(Player.Black);

        // Assert
        Assert.That(inCheck, Is.False);
    }

    [Test]
    public void IsInCheck_ByAKnight_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "e8")
            .White(PieceType.Knight, "f6")
            .White(PieceType.King, "a1")
            .Build();

        // Act
        bool inCheck = board.IsInCheck(Player.Black);

        // Assert
        Assert.That(inCheck, Is.True);
    }

    [Test]
    public void IsInCheck_ByAPawnFromTheDiagonal_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "d7")
            .White(PieceType.King, "a1")
            .Build();

        // Act
        bool inCheck = board.IsInCheck(Player.Black);

        // Assert
        Assert.That(inCheck, Is.True);
    }

    [Test]
    public void IsInCheck_ByAPawnStraightAhead_IsFalse()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "e7")
            .White(PieceType.King, "a1")
            .Build();

        // Act
        bool inCheck = board.IsInCheck(Player.Black);

        // Assert
        Assert.That(inCheck, Is.False);
    }

    #endregion

    #region Copy

    [Test]
    public void Copy_CreatesAnIndependentBoard()
    {
        // Arrange
        Board original = Board.Initial();

        // Act
        Board copy = original.Copy();
        copy[Sq.At("e2")] = null;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(original[Sq.At("e2")], Is.Not.Null);
            Assert.That(copy.CountPieces().TotalCount, Is.EqualTo(31));
        }
    }

    [Test]
    public void Copy_CreatesNewPieceInstances()
    {
        // Arrange
        Board original = Board.Initial();

        // Act
        Board copy = original.Copy();

        // Assert
        Assert.That(copy[Sq.At("e1")], Is.Not.SameAs(original[Sq.At("e1")]));
    }

    [Test]
    public void Copy_KeepsTheHasMovedFlag()
    {
        // Arrange
        Board original = Board.Initial();
        original[Sq.At("e1")].HasMoved = true;

        // Act
        Board copy = original.Copy();

        // Assert
        Assert.That(copy[Sq.At("e1")].HasMoved, Is.True);
    }

    #endregion

    #region Insufficient material

    [Test]
    public void InsufficientMaterial_InitialPosition_IsFalse()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.False);
    }

    [Test]
    public void InsufficientMaterial_KingAgainstKing_IsTrue()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .Build();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.True);
    }

    [TestCase(PieceType.Bishop)]
    [TestCase(PieceType.Knight)]
    public void InsufficientMaterial_KingAndMinorPieceAgainstKing_IsTrue(PieceType minorPiece)
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(minorPiece, "c3")
            .Black(PieceType.King, "e8")
            .Build();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.True);
    }

    [Test]
    public void InsufficientMaterial_BishopsOnTheSameSquareColor_IsTrue()
    {
        // Arrange: c1 and f8 are both dark squares
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Bishop, "c1")
            .Black(PieceType.King, "e8")
            .Black(PieceType.Bishop, "f8")
            .Build();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.True);
    }

    [Test]
    public void InsufficientMaterial_BishopsOnDifferentSquareColors_IsFalse()
    {
        // Arrange: c1 is a dark square, c8 is a light square
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Bishop, "c1")
            .Black(PieceType.King, "e7")
            .Black(PieceType.Bishop, "c8")
            .Build();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.False);
    }

    [TestCase(PieceType.Rook)]
    [TestCase(PieceType.Queen)]
    [TestCase(PieceType.Pawn)]
    public void InsufficientMaterial_WithAMajorPieceOrPawn_IsFalse(PieceType piece)
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(piece, "a3")
            .Black(PieceType.King, "e8")
            .Build();

        // Act
        bool insufficient = board.InsufficientMaterial();

        // Assert
        Assert.That(insufficient, Is.False);
    }

    #endregion

    #region Castling rights

    [TestCase(Player.White)]
    [TestCase(Player.Black)]
    public void CastleRights_InitialPosition_AreAvailableOnBothSides(Player player)
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        bool kingSide = board.CastleRightsKS(player);
        bool queenSide = board.CastleRightsQS(player);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(kingSide, Is.True);
            Assert.That(queenSide, Is.True);
        }
    }

    [Test]
    public void CastleRights_AfterTheKingMoved_AreLostForThatColorOnly()
    {
        // Arrange
        Board board = Board.Initial();
        board[Sq.At("e1")].HasMoved = true;

        // Act
        bool whiteKingSide = board.CastleRightsKS(Player.White);
        bool whiteQueenSide = board.CastleRightsQS(Player.White);
        bool blackKingSide = board.CastleRightsKS(Player.Black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(whiteKingSide, Is.False);
            Assert.That(whiteQueenSide, Is.False);
            Assert.That(blackKingSide, Is.True);
        }
    }

    [Test]
    public void CastleRights_AfterARookMoved_AreLostOnThatSideOnly()
    {
        // Arrange
        Board board = Board.Initial();
        board[Sq.At("h1")].HasMoved = true;

        // Act
        bool kingSide = board.CastleRightsKS(Player.White);
        bool queenSide = board.CastleRightsQS(Player.White);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(kingSide, Is.False);
            Assert.That(queenSide, Is.True);
        }
    }

    [Test]
    public void CastleRights_WhenTheRookWasCaptured_AreLost()
    {
        // Arrange
        Board board = Board.Initial();
        board[Sq.At("a8")] = null;

        // Act
        bool queenSide = board.CastleRightsQS(Player.Black);

        // Assert
        Assert.That(queenSide, Is.False);
    }

    [Test]
    public void CastleRights_ForNoPlayer_AreFalse()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        bool kingSide = board.CastleRightsKS(Player.None);

        // Assert
        Assert.That(kingSide, Is.False);
    }

    #endregion

    #region En Passant

    [Test]
    public void CanCaptureEnPassant_WithAPawnNextToTheSkippedSquare_IsTrue()
    {
        // Arrange: black just played d7-d5, so d6 was skipped
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "e5")
            .Black(PieceType.Pawn, "d5")
            .Build();
        board.SetPawnSkipPosition(Player.Black, Sq.At("d6"));

        // Act
        bool canCapture = board.CanCaptureEnPassant(Player.White);

        // Assert
        Assert.That(canCapture, Is.True);
    }

    [Test]
    public void CanCaptureEnPassant_WithoutASkippedSquare_IsFalse()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "e5")
            .Black(PieceType.Pawn, "d5")
            .Build();

        // Act
        bool canCapture = board.CanCaptureEnPassant(Player.White);

        // Assert
        Assert.That(canCapture, Is.False);
    }

    [Test]
    public void CanCaptureEnPassant_WhenNoPawnIsNextToTheSkippedSquare_IsFalse()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "a5")
            .Black(PieceType.Pawn, "d5")
            .Build();
        board.SetPawnSkipPosition(Player.Black, Sq.At("d6"));

        // Act
        bool canCapture = board.CanCaptureEnPassant(Player.White);

        // Assert
        Assert.That(canCapture, Is.False);
    }

    #endregion
}
