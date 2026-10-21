using ChessLogic.Services;
using Moq;

namespace ChessOnline.Tests;

[TestFixture]
public class MatchServiceTests
{
    private Mock<IRandomSource> random;
    private MatchService service;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        random = new Mock<IRandomSource>();
        random.Setup(r => r.Next(2)).Returns(0);

        service = new MatchService(random.Object);
    }

    #endregion

    #region Find Or Create

    [Test]
    public void FindOrCreate_InAnEmptyChat_CreatesAWaitingMatch()
    {
        // Arrange
        // Act
        ChessMatch match = service.FindOrCreate("chat1", "anna");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(match, Is.Not.Null);
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        }
    }

    [Test]
    public void FindOrCreate_TwoUsersInTheSameChat_GetTheSameRunningMatch()
    {
        // Arrange
        ChessMatch first = service.FindOrCreate("chat1", "anna");

        // Act
        ChessMatch second = service.FindOrCreate("chat1", "ben");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.Snapshot.Status, Is.EqualTo(MatchStatus.Running));
        }
    }

    [Test]
    public void FindOrCreate_UsesTheInjectedRandomSourceForTheColors()
    {
        // Arrange
        service.FindOrCreate("chat1", "anna");

        // Act
        ChessMatch match = service.FindOrCreate("chat1", "ben");

        // Assert
        random.Verify(r => r.Next(2), Times.Once);
        Assert.That(match.ColorOf("anna"), Is.EqualTo(ChessLogic.Enums.Player.White));
    }

    [Test]
    public void FindOrCreate_InDifferentChats_GetsDifferentMatches()
    {
        // Arrange
        ChessMatch first = service.FindOrCreate("chat1", "anna");

        // Act
        ChessMatch second = service.FindOrCreate("chat2", "ben");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        }
    }

    [Test]
    public void FindOrCreate_TheSameUserAgain_GetsTheSameMatch()
    {
        // Arrange
        ChessMatch first = service.FindOrCreate("chat1", "anna");

        // Act
        ChessMatch again = service.FindOrCreate("chat1", "anna");

        // Assert
        Assert.That(again, Is.SameAs(first));
    }

    [Test]
    public void FindOrCreate_AThirsUser_GetsNoMatch()
    {
        // Arrange
        service.FindOrCreate("chat1", "anna");
        service.FindOrCreate("chat1", "ben");

        // Act
        ChessMatch match = service.FindOrCreate("chat1", "carl");

        // Assert
        Assert.That(match, Is.Null);
    }

    [Test]
    public void FindOrCreate_AfterTheMatchIsFinished_CreatesANewWaitingGame()
    {
        // Arrange
        ChessMatch finished = service.FindOrCreate("chat1", "anna");
        service.FindOrCreate("chat1", "ben");
        finished.Resign("anna");

        // Act
        ChessMatch next = service.FindOrCreate("chat1", "anna");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(next, Is.Not.SameAs(finished));
            Assert.That(next.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        }
    }

    [Test]
    public void FindOrCreate_TheOtherPlayerJoinsTheRematch()
    {
        // Arrange
        ChessMatch finished = service.FindOrCreate("chat1", "anna");
        service.FindOrCreate("chat1", "ben");
        finished.Resign("anna");
        ChessMatch rematch = service.FindOrCreate("chat1", "anna");

        // Act
        ChessMatch joined = service.FindOrCreate("chat1", "ben");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(joined, Is.SameAs(rematch));
            Assert.That(rematch.Snapshot.Status, Is.EqualTo(MatchStatus.Running));
        }
    }

    #endregion

    #region Cancel Waiting

    [Test]
    public void CancelWaiting_ByTheCreator_RemovesTheMatch()
    {
        // Arrange
        ChessMatch waiting = service.FindOrCreate("chat1", "anna");

        // Act
        service.CancelWaiting("chat1", "anna");

        // Assert
        Assert.That(service.FindOrCreate("chat1", "ben"), Is.Not.SameAs(waiting));
    }

    [Test]
    public void CancelWaiting_ByAnotherUser_KeepsTheMatch()
    {
        // Arrange
        ChessMatch waiting = service.FindOrCreate("chat1", "anna");

        // Act
        service.CancelWaiting("chat1", "ben");

        // Assert
        Assert.That(service.FindOrCreate("chat1", "anna"), Is.SameAs(waiting));
    }

    [Test]
    public void CancelWaiting_WhenTheMatchAlreadyStarted_KeepsTheMatch()
    {
        // Arrange
        ChessMatch running = service.FindOrCreate("chat1", "anna");
        service.FindOrCreate("chat1", "ben");

        // Act
        service.CancelWaiting("chat1", "anna");

        // Assert
        Assert.That(service.FindOrCreate("chat1", "anna"), Is.SameAs(running));
    }

    [Test]
    public void CancelWaiting_InAChatWithoutAMatch_DoesNotThrow()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => service.CancelWaiting("unknown", "anna"));
    }

    #endregion
}
