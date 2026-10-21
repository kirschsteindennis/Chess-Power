using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessOnline;
using ChessUI.Teams.Sessions;
using Moq;

namespace ChessUI.Teams.Tests.Sessions;

[TestFixture]
public class OnlineGameSessionTests
{
    private const string User = "anna";

    private Mock<IChessMatch> match;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        // By default: a running match, white to move, and our user plays white
        match = new Mock<IChessMatch>();
        match.Setup(m => m.Snapshot).Returns(SnapshotOf(MatchStatus.Running, Player.White));
        match.Setup(m => m.ColorOf(User)).Returns(Player.White);
    }

    private OnlineGameSession CreateSession() => new(match.Object, User);

    // A waiting match has no board yet, exactly like the real one
    private static MatchSnapshot SnapshotOf(MatchStatus status, Player currentPlayer, Result result = null)
    {
        Board board = status == MatchStatus.Waiting ? null : Board.Initial();
        return new MatchSnapshot(board, currentPlayer, result, status);
    }

    #endregion

    #region Events

    [Test]
    public void Changed_IsRaisedWhenTheMatchChanges()
    {
        // Arrange
        OnlineGameSession session = CreateSession();
        int changes = 0;
        session.Changed += () => changes++;

        // Act
        match.Raise(m => m.Changed += null);

        // Assert
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_StopsForwardingChangesOfTheMatch()
    {
        // Arrange
        OnlineGameSession session = CreateSession();
        int changes = 0;
        session.Changed += () => changes++;

        // Act
        session.Dispose();
        match.Raise(m => m.Changed += null);

        // Assert
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void Changed_WithoutSubscribers_DoesNotThrow()
    {
        // Arrange: nobody listens to the session, but the match raises the event anyway
        CreateSession();

        // Act
        // Assert
        Assert.DoesNotThrow(() => match.Raise(m => m.Changed += null));
    }

    #endregion

    #region Reading the match

    [TestCase(MatchStatus.Waiting, false)]
    [TestCase(MatchStatus.Running, true)]
    [TestCase(MatchStatus.Finished, true)]
    public void HasStarted_DependsOnTheMatchStatus(MatchStatus status, bool expected)
    {
        // Arrange
        match.Setup(m => m.Snapshot).Returns(SnapshotOf(status, Player.White));
        OnlineGameSession session = CreateSession();

        // Act
        bool started = session.HasStarted;

        // Assert
        Assert.That(started, Is.EqualTo(expected));
    }

    [Test]
    public void BoardPlayerAndResult_ComeFromTheSnapshot()
    {
        // Arrange
        Board board = Board.Initial();
        Result result = Result.Win(Player.Black);
        match.Setup(m => m.Snapshot).Returns(new MatchSnapshot(board, Player.Black, result, MatchStatus.Finished));
        OnlineGameSession session = CreateSession();

        // Act
        Board sessionBoard = session.Board;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sessionBoard, Is.SameAs(board));
            Assert.That(session.CurrentPlayer, Is.EqualTo(Player.Black));
            Assert.That(session.Result, Is.SameAs(result));
            Assert.That(session.IsGameOver, Is.True);
        }
    }

    [Test]
    public void IsGameOver_WhileTheMatchRuns_IsFalse()
    {
        // Arrange
        OnlineGameSession session = CreateSession();

        // Act
        bool gameOver = session.IsGameOver;

        // Assert
        Assert.That(gameOver, Is.False);
    }

    [Test]
    public void LocalPlayer_IsTheColorThatTheMatchGaveToThisUser()
    {
        // Arrange
        match.Setup(m => m.ColorOf(User)).Returns(Player.Black);
        OnlineGameSession session = CreateSession();

        // Act
        Player local = session.LocalPlayer;

        // Assert
        Assert.That(local, Is.EqualTo(Player.Black));
        match.Verify(m => m.ColorOf(User), Times.Once());
    }

    [TestCase(MatchStatus.Running, Player.White, Player.White, true)]
    [TestCase(MatchStatus.Running, Player.Black, Player.White, false)]
    [TestCase(MatchStatus.Running, Player.Black, Player.Black, true)]
    [TestCase(MatchStatus.Finished, Player.White, Player.White, false)]
    [TestCase(MatchStatus.Waiting, Player.None, Player.None, false)]
    public void IsLocalTurn_NeedsARunningMatchAndTheUsersTurn(
        MatchStatus status, Player current, Player local, bool expected)
    {
        // Arrange
        match.Setup(m => m.Snapshot).Returns(SnapshotOf(status, current));
        match.Setup(m => m.ColorOf(User)).Returns(local);
        OnlineGameSession session = CreateSession();

        // Act
        bool isLocalTurn = session.IsLocalTurn;

        // Assert
        Assert.That(isLocalTurn, Is.EqualTo(expected));
    }

    #endregion

    #region Playing

    [Test]
    public void LegalMovesFor_AsksTheMatchOnBehalfOfThisUser()
    {
        // Arrange
        Position pos = new(6, 4);
        List<Move> expected = [new NormalMove(pos, new Position(5, 4))];
        match.Setup(m => m.LegalMovesFor(User, pos)).Returns(expected);
        OnlineGameSession session = CreateSession();

        // Act
        List<Move> moves = session.LegalMovesFor(pos).ToList();

        // Assert
        Assert.That(moves, Is.EqualTo(expected));
        match.Verify(m => m.LegalMovesFor(User, pos), Times.Once());
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task TryMakeMoveAsync_ReturnsWhatTheMatchDecides(bool matchAccepts)
    {
        // Arrange
        Move move = new NormalMove(new Position(6, 4), new Position(5, 4));
        match.Setup(m => m.TryMakeMove(User, move)).Returns(matchAccepts);
        OnlineGameSession session = CreateSession();

        // Act
        bool accepted = await session.TryMakeMoveAsync(move);

        // Assert
        Assert.That(accepted, Is.EqualTo(matchAccepts));
        match.Verify(m => m.TryMakeMove(User, move), Times.Once());
    }

    [Test]
    public async Task TryMakeMoveAsync_NeverPassesAnotherUsersIdToTheMatch()
    {
        // Arrange
        Move move = new NormalMove(new Position(6, 4), new Position(5, 4));
        OnlineGameSession session = CreateSession();

        // Act
        await session.TryMakeMoveAsync(move);

        // Assert: exactly one call happened, and it carried our own user id
        match.Verify(m => m.TryMakeMove(It.IsAny<string>(), move), Times.Once());
        match.Verify(m => m.TryMakeMove(User, move), Times.Once());
    }

    #endregion

    #region Resigning

    [TestCase(MatchStatus.Waiting, false)]
    [TestCase(MatchStatus.Running, true)]
    [TestCase(MatchStatus.Finished, false)]
    public void CanResign_OnlyWhileTheMatchRuns(MatchStatus status, bool expected)
    {
        // Arrange
        match.Setup(m => m.Snapshot).Returns(SnapshotOf(status, Player.White));
        OnlineGameSession session = CreateSession();

        // Act
        bool canResign = session.CanResign;

        // Assert
        Assert.That(canResign, Is.EqualTo(expected));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Resign_ReturnsWhatTheMatchDecides(bool matchAccepts)
    {
        // Arrange
        match.Setup(m => m.Resign(User)).Returns(matchAccepts);
        OnlineGameSession session = CreateSession();

        // Act
        bool resigned = session.Resign();

        // Assert
        Assert.That(resigned, Is.EqualTo(matchAccepts));
        match.Verify(m => m.Resign(User), Times.Once());
    }

    #endregion
}