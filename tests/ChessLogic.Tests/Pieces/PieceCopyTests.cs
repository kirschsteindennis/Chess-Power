using ChessLogic.Enums;
using ChessLogic.Pieces;
using Chess.Test.Shared.Support;

namespace ChessLogic.Tests.Pieces;

[TestFixture]
public class PieceCopyTests
{
    [TestCase(PieceType.Pawn)]
    [TestCase(PieceType.Knight)]
    [TestCase(PieceType.Bishop)]
    [TestCase(PieceType.Rook)]
    [TestCase(PieceType.Queen)]
    [TestCase(PieceType.King)]
    public void Copy_KeepsColorTypeAndHasMoved(PieceType type)
    {
        // Arrange
        Piece original = PieceFactory.Create(Player.Black, type);
        original.HasMoved = true;

        // Act
        Piece copy = original.Copy();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.Type, Is.EqualTo(type));
            Assert.That(copy.Color, Is.EqualTo(Player.Black));
            Assert.That(copy.HasMoved, Is.True);
        }
    }
}
