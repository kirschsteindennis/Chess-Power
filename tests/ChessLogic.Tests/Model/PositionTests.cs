using ChessLogic.Enums;
using ChessLogic.Model;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class PositionTests
{
    [Test]
    public void Constructor_SetsRowAndColumn()
    {
        // Arrange
        int row = 3;
        int column = 5;

        // Act
        Position pos = new(row, column);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(pos.Row, Is.EqualTo(3));
            Assert.That(pos.Column, Is.EqualTo(5));
        }
    }

    [TestCase(0, 0, Player.White)]
    [TestCase(0, 1, Player.Black)]
    [TestCase(4, 4, Player.White)]
    [TestCase(7, 0, Player.Black)]
    [TestCase(7, 7, Player.White)]
    public void SquareColor_DependsOnRowAndColumn(int row, int column, Player expected)
    {
        // Arrange
        Position pos = new(row, column);

        // Act
        Player color = pos.SquareColor();

        // Assert
        Assert.That(color, Is.EqualTo(expected));
    }

    [Test]
    public void Equals_SameCoordinates_AreEqual()
    {
        // Arrange
        Position first = new(2, 3);
        Position second = new(2, 3);

        // Act
        bool equalByOperator = first == second;
        bool equalByMethod = first.Equals(second);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(equalByOperator, Is.True);
            Assert.That(equalByMethod, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }

    [Test]
    public void Equals_DifferentCoordinates_AreNotEqual()
    {
        // Arrange
        Position first = new(2, 3);
        Position second = new(3, 2);

        // Act
        bool notEqualByOperator = first != second;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notEqualByOperator, Is.True);
            Assert.That(first.Equals(second), Is.False);
        }
    }

    [Test]
    public void Equals_WithNull_IsFalse()
    {
        // Arrange
        Position pos = new(1, 1);

        // Act
        bool equal = pos.Equals(null);

        // Assert
        Assert.That(equal, Is.False);
    }

    [Test]
    public void PlusDirection_MovesThePosition()
    {
        // Arrange
        Position pos = new(4, 4);

        // Act
        Position moved = pos + Direction.NorthEast;

        // Assert
        Assert.That(moved, Is.EqualTo(new Position(3, 5)));
    }

    [Test]
    public void PlusDirection_DoesNotChangeTheOriginalPosition()
    {
        // Arrange
        Position pos = new(4, 4);

        // Act
        Position unused = pos + Direction.South;

        // Assert
        Assert.That(pos, Is.EqualTo(new Position(4, 4)));
    }

    [Test]
    public void AsDictionaryKey_FindsAnEqualPosition()
    {
        // Arrange
        Dictionary<Position, string> dictionary = new() { { new Position(1, 1), "found" } };

        // Act
        string value = dictionary[new Position(1, 1)];

        // Assert
        Assert.That(value, Is.EqualTo("found"));
    }
}
