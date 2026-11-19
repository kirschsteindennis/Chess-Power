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

    [Test]
    public void FlagFell_TheIncrementPostponesTheEnd()
    {
        // arrange: 5+10 white moves after 290 seconds, then has 20 seconds left
        using ChessClock clock = NewClock(5, incrementSeconds: 10);
        clock.Start(Player.White);
        time.Advance(Seconds(290));
        clock.Press(Player.White);
        time.Advance(Seconds(1));
        clock.Press(Player.Black);

        // Act: white's clock now has 20 seconds
        time.Advance(Seconds(19.9));
        bool beforeTheEnd = flags.Count == 0;
        time.Advance(Seconds(0.1));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(beforeTheEnd, Is.True);
            Assert.That(flags, Is.EqualTo([Player.White]));
        }
    }

    [Test]
    public void FlagFell_IsRaisedOnlyOnce()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);

        // Act
        time.Advance(Seconds(300));
        time.Advance(Seconds(1000));

        // Assert
        Assert.That(flags, Has.Count.EqualTo(1));
    }

    [Test]
    public void FlagFell_AMovePressedJustInTime_PreventsTheFlag()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        time.Advance(Seconds(299.9));

        // Act
        clock.Press(Player.White);
        time.Advance(Seconds(100));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(flags, Is.Empty);
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(200)));
        }
    }

    [Test]
    public void FlagFell_WhenTheTimerFiresTooEarly_WaitsForTheRestOfTheTime()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        time.Advance(Seconds(200));

        // Act
        time.FireTimersNow();
        bool noFlagYet = flags.Count == 0;
        time.Advance(Seconds(100));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(noFlagYet, Is.True);
            Assert.That(flags, Is.EqualTo([Player.White]));
        }
    }

    #endregion

    #region Stopping

    [Test]
    public void Stop_FreezesBothTimesAndRaisesNoFlag()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        time.Advance(Seconds(10));

        // Act
        clock.Stop();
        time.Advance(Seconds(1000));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(clock.Remaining(Player.White), Is.EqualTo(Seconds(290)));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(300)));
            Assert.That(flags, Is.Empty);
            Assert.That(time.ActiveTimerCount, Is.EqualTo(0));
        }
    }

    [Test]
    public void Stop_BeforeTheStartAndTwice_DoesNotHarm()
    {
        // Arrange
        using ChessClock clock = NewClock(5);

        // Act
        clock.Stop();
        clock.Start(Player.White);
        clock.Stop();
        clock.Stop();

        // Assert
        Assert.That(clock.Running, Is.EqualTo(Player.None));
    }

    [Test]
    public void Stop_AfterStop_DoesNotRestartTheClock()
    {
        // arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        clock.Stop();

        // Act
        clock.Start(Player.Black);
        time.Advance(Seconds(10));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(clock.Remaining(Player.Black), Is.EqualTo(Seconds(300)));
        }
    }

    [Test]
    public void Start_AfterAStopThatCameBeforeTheStart_AlsoDoesNothing()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Stop();

        // Act
        clock.Start(Player.White);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clock.Running, Is.EqualTo(Player.None));
            Assert.That(time.ActiveTimerCount, Is.Zero);
        }
    }

    #endregion

    #region Asking

    [Test]
    public void IsExpired_IsTrueOnlyForTheRunningPlayerWithoutTime()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);

        // Act & Assert
        Assert.That(clock.IsExpired(Player.White), Is.False);

        time.AdvanceWithoutFiring(Seconds(299));
        Assert.That(clock.IsExpired(Player.White), Is.False);

        time.AdvanceWithoutFiring(Seconds(1));
        Assert.That(clock.IsExpired(Player.White), Is.True);
        Assert.That(clock.IsExpired(Player.Black), Is.False);
    }

    [Test]
    public void Snapshot_DescribesTheStateAtThatMoment()
    {
        // Arrange
        using ChessClock clock = NewClock(5, incrementSeconds: 2);
        clock.Start(Player.White);
        time.Advance(Seconds(10));
        clock.Press(Player.White);
        time.Advance(Seconds(4));

        // Act
        ClockSnapshot snapshot = clock.Snapshot();

        // Assert: the snapshot is from the moment of the last press, the 4 seconds since then are not in it
        Assert.That(snapshot.White, Is.EqualTo(Seconds(292)));
        Assert.That(snapshot.Black, Is.EqualTo(Seconds(300)));
        Assert.That(snapshot.Running, Is.EqualTo(Player.Black));
        Assert.That(snapshot.Control, Is.EqualTo(TimeControl.Minutes(5, 2)));
        Assert.That(snapshot.RemainingOf(Player.Black, time.GetTimestamp()), Is.EqualTo(Seconds(296)));
    }

    [Test]
    public void Snapshot_IsNotChangedByLaterMoves()
    {
        // Arrange
        using ChessClock clock = NewClock(5);
        clock.Start(Player.White);
        ClockSnapshot before = clock.Snapshot();

        // Act
        time.Advance(Seconds(10));
        clock.Press(Player.White);

        // Assert
        Assert.That(before.Running, Is.EqualTo(Player.White));
        Assert.That(before.White, Is.EqualTo(Seconds(300)));
    }

    #endregion

    #region Disposing

    [Test]
    public void Dispose_StopsTheTimerSoThatNoFlagCanComeAnyMore()
    {
        // Arrange
        ChessClock clock = NewClock(5);
        clock.Start(Player.White);

        // Act
        clock.Dispose();
        int timersRightAfterwards = time.ActiveTimerCount;
        time.Advance(Seconds(1000));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(timersRightAfterwards, Is.EqualTo(0), "the timer must be released at once, not when it would have fired");
            Assert.That(flags, Is.Empty);
            Assert.That(time.ActiveTimerCount, Is.Zero);
        }
    }

    #endregion
}
