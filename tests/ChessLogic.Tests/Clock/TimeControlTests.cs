using ChessLogic.Clock;

namespace ChessLogic.Tests.Clock;

[TestFixture]
public class TimeControlTests
{
    [TestCase(0, 0)]
    [TestCase(-60, 0)]
    [TestCase(10801, 0)]
    public void Constructor_WithAnInvalidStartingTime_Throws(int initialSeconds, int incrementSeconds)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeControl(TimeSpan.FromSeconds(initialSeconds), TimeSpan.FromSeconds(incrementSeconds)));
    }

    [TestCase(300, -1)]
    [TestCase(300, 181)]

    public void Constructor_WithAnInvalidIncrement_Throws(int initialSeconds, int incrementSeconds)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeControl(TimeSpan.FromSeconds(initialSeconds), TimeSpan.FromSeconds(incrementSeconds)));
    }

    [TestCase(1, 0)]
    [TestCase(180, 180)]
    [TestCase(10800, 0)]
    public void Constructor_AtTheLimits_IsAccepted(int initialSeconds, int incrementSeconds)
    {
        // Arrange & Act
        TimeControl control = new(TimeSpan.FromSeconds(initialSeconds), TimeSpan.FromSeconds(incrementSeconds));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.Initial, Is.EqualTo(TimeSpan.FromSeconds(initialSeconds)));
            Assert.That(control.Increment, Is.EqualTo(TimeSpan.FromSeconds(incrementSeconds)));
        }
    }
}
