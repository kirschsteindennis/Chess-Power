using ChessLogic.Clock;
using ChessLogic.Enums;
using ChessLogic.Tests.Support;

namespace ChessLogic.Tests.Clock;

[TestFixture]
public class ChessClockTests
{
    private ManualTimeProvider time;
    private List<Player> flags;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        time = new();
        flags = [];
    }

    private ChessClock NewClock(int minutes = 5, int incrementSeconds = 0)
    {
        ChessClock clock = new(TimeControl.Minutes(minutes, incrementSeconds), time);
        clock.FlagFell += flags.Add;
        return clock;
    }

    private static TimeSpan Seconds(double seconds) => TimeSpan.FromSeconds(seconds);

    #endregion

    #region At the Start

    [Test]
    public void NewClock_GivesBothPlayersTheStartingTimeAndRunsNoClock()
    {
        // Arrange
        using ChessClock clock = NewClock(5);

        // Act
        TimeSpan white = clock.Remaining(Player.White);
        TimeSpan black = clock.Remaining(Player.Black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(white, Is.EqualTo(Seconds(300)));
            Assert.That(black, Is.EqualTo(Seconds(300)));
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(time.ActiveTimerCount, Is.EqualTo(0));
        }
    }

    [Test]
    public void Constructor_WithoutATimeControl_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ChessClock(null, time));
    }

    [Test]
    public void Start_RunsTheClockOfTheFirstPlayerOnly()
    {
        // Arrange
        using ChessClock clock = NewClock(5);

        // Act
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Running, Is.EqualTo(Player.White));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(290)));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(300)));
        }
    }

    [Test]
    public void Start_TheTimeBeforeTheStartDoesNotCount()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        time.Advance(Seconds(100));

        // Act
        clock.Start(Player.White);
        time.Advance(Seconds(5));

        // Assert
        Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(295)));
    }

    [Test]
    public void Start_ASecondTime_IsIgnored()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Act
        clock.Start(Player.Black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Running, Is.EqualTo(Player.White));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(290)));
        }
    }

    [Test]
    public void Start_WithNoPlayer_DoesNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5);

        // Act
        clock.Start(Player.None);

        // Assert
        Assert.That(clock.Running, Is.EqualTo(Player.None));
    }

    #endregion

    #region Pressing the Clock

    [Test]
    public void Press_StopsTheMoversClockAddsTheIncrementAndStartsTheOpponents()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 3);
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Act
        bool inTime = clock.Press(Player.White);
        time.Advance(Seconds(20));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inTime, Is.True);
            Assert.That(clock.Running, Is.EqualTo(Player.Black));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(293)));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(280)));
        }
    }

    [Test]
    public void Press_WithoutAnIncrement_AddsNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 0);
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Act
        clock.Press(Player.White);

        // Assert
        Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(290)));
    }

    [Test]
    public void Press_ByThePlayerWhoseClockDoesNotRun_ChangesNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 3);
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Act
        bool result = clock.Press(Player.Black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(clock.Running, Is.EqualTo(Player.White));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(300)));
        }
    }

    [Test]
    public void Press_BeforeTheStart_ChangesNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 3);

        // Act
        bool result = clock.Press(Player.White);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(300)));
        }
    }

    [Test]
    public void Press_ManyMoves_AddsUpTheTimeOfEachPlayerSeperately()
    {
        // Arrange: 5+2
        using ChessClock clock = NewClock(5, incrementSeconds: 2);
        clock.Start(Player.White);

        // Act
        time.Advance(Seconds(5));  clock.Press(Player.White);    // white: 300 - 5 + 2
        time.Advance(Seconds(8));  clock.Press(Player.Black);    // black: 300 - 8 + 2
        time.Advance(Seconds(1));  clock.Press(Player.White);    // white: 297 - 1 + 2
        time.Advance(Seconds(30)); clock.Press(Player.Black);    // black: 294 - 30 + 2

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(298)));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(266)));
            Assert.That(clock.Running, Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void Press_AfterTheTimeWasUpButBeforeTheTimerFired_ReturnsFalseAndAddsNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 3);
        clock.Start(Player.White);
        time.AdvanceWithoutFiring(Seconds(301));

        // Act
        bool inTime = clock.Press(Player.White);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inTime, Is.False);
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(TimeSpan.Zero));
            Assert.That(time.ActiveTimerCount, Is.Zero);
        }
    }

    #endregion

    #region The Time is Over

    [Test]
    public void FlagFell_IsRaisedWhenTheTimeIsOver()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);

        // Act
        time.Advance(Seconds(300));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(flags, Is.EqualTo([Player.White]));
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(TimeSpan.Zero));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(300)));
        }
    }

    [Test]
    public void FlagFell_IsNotRaisedBeforeTheTimeIsOver()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);

        // act
        time.Advance(Seconds(299.9));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(flags, Is.Empty);
            Assert.That(clock.Running, Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void FlagFell_AfterAMove_IsAboutTheOtherPlayer()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        time.Advance(Seconds(10));
        clock.Press(Player.White);

        // Act
        time.Advance(Seconds(300));

        // Assert
        Assert.That(flags, Is.EqualTo([Player.Black]));
    }

    #endregion
}
