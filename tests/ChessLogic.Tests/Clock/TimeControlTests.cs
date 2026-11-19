using ChessLogic.Clock;
using ChessLogic.Enums;

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

    [TestCase(300, 0, "5+0")]
    [TestCase(180, 2, "3+2")]
    [TestCase(900, 10, "15+10")]
    [TestCase(30, 0, "0:30+0")]
    [TestCase(90, 1, "1:30+1")]
    public void Name_ShowsMinutesPlusSecondsPerMove(int initialSeconds, int incrementSeconds, string expected)
    {
        // Arrange
        TimeControl control = new(TimeSpan.FromSeconds(initialSeconds), TimeSpan.FromSeconds(incrementSeconds));

        // Act
        string name = control.Name;

        // Assert
        Assert.That(name, Is.EqualTo(expected));
    }

    [TestCase(30, 0, TimeCategory.Bullet)]
    [TestCase(60, 0, TimeCategory.Bullet)]
    [TestCase(120, 0, TimeCategory.Bullet)]
    [TestCase(180, 2, TimeCategory.Blitz)]
    [TestCase(300, 0, TimeCategory.Blitz)]
    [TestCase(600, 0, TimeCategory.Rapid)]
    [TestCase(900, 10, TimeCategory.Rapid)]
    [TestCase(1800, 0, TimeCategory.Classical)]
    [TestCase(3600, 0, TimeCategory.Classical)]
    public void Category_FollowsTheEstimatedDurationOfAGame(int initialSeconds, int incrementSeconds, TimeCategory expected)
    {
        // Arrange
        TimeControl control = new(TimeSpan.FromSeconds(initialSeconds), TimeSpan.FromSeconds(incrementSeconds));

        // Act
        TimeCategory category = control.Category;

        // Assert
        Assert.That(category, Is.EqualTo(expected));
    }

    [Test]
    public void Label_PutsTheCategoryBeforeTheName()
    {
        // Arrange
        TimeControl control = TimeControl.Minutes(5);

        // Act
        string label = control.Label;

        // Assert
        Assert.That(label, Is.EqualTo("Blitz 5+0"));
    }

    [Test]
    public void Minutes_BuildsAStartingTimeInMinutesAndAnIncrementInSeconds()
    {
        // Arrange & Act
        TimeControl control = TimeControl.Minutes(15, 10);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.Initial, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(control.Increment, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }
    }

    [Test]
    public void Presets_AreTheSixOffersOfTheMenusInOrderOfLength()
    {
        // Arrange & Act
        IReadOnlyList<TimeControl> presets = TimeControl.Presets;

        // Assert
        Assert.That(string.Join(" ", presets.Select(p => p.Name)), Is.EqualTo("1+0 3+2 5+0 10+0 15+10 30+0"));
    }

    [Test]
    public void Equals_TimeControlsWithTheSameTimesAreEqual()
    {
        // Arrange
        TimeControl first = TimeControl.Minutes(5, 3);
        TimeControl second = TimeControl.Minutes(5, 3);
        TimeControl other = TimeControl.Minutes(5, 2);

        // Act & Assert
        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.EqualTo(other));
    }
}
