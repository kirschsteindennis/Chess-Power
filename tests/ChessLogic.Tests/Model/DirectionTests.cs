using ChessLogic.Model;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class DirectionTests
{
    [Test]
    public void CompassDirections_HaveTheExpectedDeltas()
    {
        // Arrange: row 0 is the top of the board, so north means a smaller row

        // Act
        (int Row, int Column)[] deltas =
        [
            (Direction.North.RowDelta, Direction.North.ColumnDelta),
            (Direction.South.RowDelta, Direction.South.ColumnDelta),
            (Direction.East.RowDelta, Direction.East.ColumnDelta),
            (Direction.West.RowDelta, Direction.West.ColumnDelta),
            (Direction.NorthEast.RowDelta, Direction.NorthEast.ColumnDelta),
            (Direction.NorthWest.RowDelta, Direction.NorthWest.ColumnDelta),
            (Direction.SouthEast.RowDelta, Direction.SouthEast.ColumnDelta),
            (Direction.SouthWest.RowDelta, Direction.SouthWest.ColumnDelta),
        ];

        // Assert
        Assert.That(deltas, Is.EqualTo(
        [
            (-1, 0), (1, 0), (0, 1), (0, -1), (-1, 1), (-1, -1), (1, 1), (1, -1)
        ]));
    }

    [Test]
    public void Plus_AddsTheDeltas()
    {
        // Arrange
        Direction first = new(1, 2);
        Direction second = new(3, -5);

        // Act
        Direction sum = first + second;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sum.RowDelta, Is.EqualTo(4));
            Assert.That(sum.ColumnDelta, Is.EqualTo(-3));
        }
    }

    [Test]
    public void Multiply_ScalesBothDeltas()
    {
        // Arrange
        Direction direction = new(-1, 1);

        // Act
        Direction scaled = 2 * direction;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(scaled.RowDelta, Is.EqualTo(-2));
            Assert.That(scaled.ColumnDelta, Is.EqualTo(2));
        }
    }
}
