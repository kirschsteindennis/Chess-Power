using ChessLogic.Enums;
using ChessLogic.Model;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class CountingTests
{
    [Test]
    public void NewCounting_IsEmpty()
    {
        // Arrange
        Counting counting = new();

        // Act
        int total = counting.TotalCount;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(total, Is.EqualTo(0));
            Assert.That(counting.White(PieceType.Queen), Is.EqualTo(0));
            Assert.That(counting.Black(PieceType.Pawn), Is.EqualTo(0));
        }
    }

    [Test]
    public void Increment_CountsPerColorAndType()
    {
        // Arrange
        Counting counting = new();

        // Act
        counting.Increment(Player.White, PieceType.Pawn);
        counting.Increment(Player.White, PieceType.Pawn);
        counting.Increment(Player.Black, PieceType.Queen);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counting.White(PieceType.Pawn), Is.EqualTo(2));
            Assert.That(counting.Black(PieceType.Queen), Is.EqualTo(1));
            Assert.That(counting.White(PieceType.Queen), Is.EqualTo(0));
            Assert.That(counting.TotalCount, Is.EqualTo(3));
        }
    }
}
