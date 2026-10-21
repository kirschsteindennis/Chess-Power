using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Services;
using Moq;

namespace ChessOnline.Tests;

[TestFixture]
public class ChessMatchTests
{
    private const string Creator = "Anna";
    private const string Guest = "Ben";

    private Mock<IRandomSource> random;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        random = new Mock<IRandomSource>();
    }

    // 0 = the creator plays white, 1 = the creator plays black
    private ChessMatch StartedMatch(int randomValue = 0)
    {
        random.Setup(r => r.Next(2)).Returns(randomValue);

        ChessMatch match = new(Creator, random.Object);
        match.Join(Guest);
        return match;
    }

    // Plays one move like the UI does: ask for the legal moves, then pick the matching one
    private static bool Play(ChessMatch match, string user, string from, string to)
    {
        Move move = match.LegalMovesFor(user, Square(from))
            .FirstOrDefault(m => m.ToPos == Square(to));

        return move != null && match.TryMakeMove(user, move);
    }

    private static Position Square(string square) => new(8 - (square[1] - '0'), square[0] - 'a');

    #endregion

    #region Waiting

    [Test]
    public void NewMatch_IsWaitingForAnOpponent()
    {
        // Arrange
        ChessMatch match = new(Creator, random.Object);

        // Act
        MatchSnapshot snapshot = match.Snapshot;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
            Assert.That(match.ColorOf(Creator), Is.EqualTo(Player.None));
        }
    }

    [Test]
    public void IsWaitingFor_IsTrueOnlyForTheCreatorWhileWaiting()
    {
        // Arrange
        ChessMatch match = new(Creator, random.Object);

        // Act
        bool forCreator = match.IsWaitingFor(Creator);
        bool forGuest = match.IsWaitingFor(Guest);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forCreator, Is.True);
            Assert.That(forGuest, Is.False);
        }
    }

    [Test]
    public void Join_ByTheCreator_DoesNotStartTheMatch()
    {
        // Arrange
        ChessMatch match = new(Creator, random.Object);

        // Act
        bool joined = match.Join(Creator);

        // Assert
        Assert.That(joined, Is.True);
        Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        random.Verify(r => r.Next(It.IsAny<int>()), Times.Never());
    }

    #endregion

    #region Starting and Colors

    [Test]
    public void Join_ByAGuest_StartsTheMatchWithWhiteToMove()
    {
        // Arrange
        random.Setup(r => r.Next(2)).Returns(0);
        ChessMatch match = new(Creator, random.Object);

        // Act
        bool joined = match.Join(Guest);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(joined, Is.True);
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Running));
            Assert.That(match.Snapshot.CurrentPlayer, Is.EqualTo(Player.White));
            Assert.That(match.Snapshot.Board.CountPieces().TotalCount, Is.EqualTo(32));
        }
    }

    [Test]
    public void Join_WhenTheRandomSourceReturnsZero_TheCreatorPlaysWhite()
    {
        // Arrange
        random.Setup(r => r.Next(2)).Returns(0);
        ChessMatch match = new(Creator, random.Object);

        // Act
        match.Join(Guest);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(match.ColorOf(Creator), Is.EqualTo(Player.White));
            Assert.That(match.ColorOf(Guest), Is.EqualTo(Player.Black));
        }
    }

    [Test]
    public void Join_WhenTheRandomSourceReturnsOne_TheCreatorPlaysBlack()
    {
        // Arrange
        random.Setup(r => r.Next(2)).Returns(1);
        ChessMatch match = new(Creator, random.Object);

        // Act
        match.Join(Guest);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(match.ColorOf(Creator), Is.EqualTo(Player.Black));
            Assert.That(match.ColorOf(Guest), Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void Join_DrawsTheColorsExactlyOnce()
    {
        // Arrange
        ChessMatch match = StartedMatch();

        // Act: reloads of both players must not shuffle the colors again
        match.Join(Creator);
        match.Join(Guest);

        // Assert
        random.Verify(r => r.Next(2), Times.Once());
        Assert.That(match.ColorOf(Creator), Is.EqualTo(Player.White));
    }

    [Test]
    public void Join_ByAThirdPerson_IsRejected()
    {
        // Arrange
        ChessMatch match = StartedMatch();

        // Act
        bool joined = match.Join("Carl");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(joined, Is.False);
            Assert.That(match.ColorOf("Carl"), Is.EqualTo(Player.None));
        }
    }

    [Test]
    public void Join_RaisesChangedWhenTheMatchStarts()
    {
        // Arrange
        random.Setup(r => r.Next(2)).Returns(0);
        ChessMatch match = new(Creator, random.Object);
        int changes = 0;
        match.Changed += () => changes++;

        // Act
        match.Join(Guest);

        // Assert
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void Join_ByAnExistingPlayer_DoesNotRaiseChanged()
    {
        // Arrange
        ChessMatch match = StartedMatch();
        int changes = 0;
        match.Changed += () => changes++;

        // Act
        match.Join(Guest);

        // Assert
        Assert.That(changes, Is.EqualTo(0));
    }

    #endregion

    #region Moves

    [Test]
    public void TryMakeMove_ByThePlayerWhoseTurnItIs_Succeeds()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);

        // Act
        bool played = Play(match, Creator, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.True);
            Assert.That(match.Snapshot.CurrentPlayer, Is.EqualTo(Player.Black));
            Assert.That(match.Snapshot.Board[Square("e4")].Type, Is.EqualTo(PieceType.Pawn));
        }
    }

    [Test]
    public void TryMakeMove_ByThePlayerWhoIsNotOnTurn_IsRejected()
    {
        // Arrange: the guest plays black, but white starts
        ChessMatch match = StartedMatch(0);
        Move move = new NormalMove(Square("e7"), Square("e5"));

        // Act
        bool played = match.TryMakeMove(Guest, move);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.False);
            Assert.That(match.Snapshot.Board[Square("e7")], Is.Not.Null);
            Assert.That(match.Snapshot.CurrentPlayer, Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void TryMakeMove_ThePieceOfTheOpponent_IsRejected()
    {
        // Arrange: it is white's turn and the guest (black) tries to move a white pawn
        ChessMatch match = StartedMatch(0);
        Move whitePawnMove = new DoublePawn(Square("e2"), Square("e4"));

        // Act
        bool played = match.TryMakeMove(Guest, whitePawnMove);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.False);
            Assert.That(match.Snapshot.Board[Square("e2")], Is.Not.Null);
            Assert.That(match.Snapshot.CurrentPlayer, Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void TryMakeMove_ByAStranger_IsRejected()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        Move move = new NormalMove(Square("e2"), Square("e3"));

        // Act
        bool played = match.TryMakeMove("Carl", move);

        // Assert
        Assert.That(played, Is.False);
    }

    [Test]
    public void TryMakeMove_AnIllegalMove_IsRejected()
    {
        // Arrange: a pawn cannot jump three squares
        ChessMatch match = StartedMatch(0);
        Move illegal = new NormalMove(Square("e2"), Square("e5"));

        // Act
        bool played = match.TryMakeMove(Creator, illegal);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.False);
            Assert.That(match.Snapshot.CurrentPlayer, Is.EqualTo(Player.White));
        }
    }

    [Test]
    public void TryMakeMove_BeforeTheMatchStarted_IsRejected()
    {
        // Arrange
        ChessMatch match = new(Creator, random.Object);
        Move move = new NormalMove(Square("e2"), Square("e4"));

        // Act
        bool played = match.TryMakeMove(Creator, move);

        // Assert
        Assert.That(played, Is.False);
    }

    [Test]
    public void TryMakeMove_RaisesChangedOnceForALegalMove()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        int changes = 0;
        match.Changed += () => changes++;

        // Act
        Play(match, Creator, "e2", "e4");

        // Assert
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void TryMakeMove_DoesNotRaiseChangedForARejectedMove()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        int changes = 0;
        match.Changed += () => changes++;

        // Act
        match.TryMakeMove(Guest, new NormalMove(Square("e7"), Square("e5")));

        // Assert
        Assert.That(changes, Is.EqualTo(0));
    }

    [Test]
    public void TryMakeMove_SameMoveFromManyThreads_SucceedsExactlyOnce()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        Move move = new DoublePawn(Square("e2"), Square("e4"));
        int successes = 0;

        // Act
        Parallel.For(0, 50, _ =>
        {
            if (match.TryMakeMove(Creator, move))
                Interlocked.Increment(ref successes);
        });

        // Assert
        Assert.That(successes, Is.EqualTo(1));
    }

    [Test]
    public void LegalMovesFor_ThePlayerOnTurn_ReturnsTheMovesOfThePiece()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);

        // Act
        List<Move> moves = match.LegalMovesFor(Creator, Square("e2")).ToList();

        // Assert
        Assert.That(moves.Count, Is.EqualTo(2));
    }

    [Test]
    public void LegalMovesFor_ThePlayerWhoIsNotOnTurn_ReturnsNothing()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);

        // Act
        IEnumerable<Move> moves = match.LegalMovesFor(Guest, Square("e7"));

        // Assert
        Assert.That(moves, Is.Empty);
    }

    [Test]
    public void LegalMovesFor_ThePieceOfTheOpponent_ReturnsNothing()
    {
        // Arrange: it is white's turn, but the guest (black) asks about a white pawn
        ChessMatch match = StartedMatch(0);

        // Act
        IEnumerable<Move> moves = match.LegalMovesFor(Guest, Square("e2"));

        // Assert
        Assert.That(moves, Is.Empty);
    }

    #endregion

    #region Snapshots

    [Test]
    public void Snapshot_AnOldSnapshotIsNotChangedByLaterMoves()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        MatchSnapshot before = match.Snapshot;

        // Act
        Play(match, Creator, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(before.Board[Square("e2")], Is.Not.Null);
            Assert.That(match.Snapshot.Board[Square("e2")], Is.Null);
            Assert.That(match.Snapshot, Is.Not.SameAs(before));
        }
    }

    #endregion

    #region Game end

    [Test]
    public void TryMakeMove_FoolsMate_FinishesTheMatch()
    {
        // Arrange: the creator is white, the guest black
        ChessMatch match = StartedMatch(0);

        // Act
        Play(match, Creator, "f2", "f3");
        Play(match, Guest, "e7", "e5");
        Play(match, Creator, "g2", "g4");
        Play(match, Guest, "d8", "h4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Finished));
            Assert.That(match.Snapshot.Result.Winner, Is.EqualTo(Player.Black));
            Assert.That(match.Snapshot.Result.Reason, Is.EqualTo(EndReason.Checkmate));
        }
    }

    [Test]
    public void TryMakeMove_AfterTheMatchIsFinished_IsRejected()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        match.Resign(Guest);

        // Act
        bool played = Play(match, Creator, "e2", "e4");

        // Assert
        Assert.That(played, Is.False);
    }

    #endregion

    #region Resign

    [Test]
    public void Resign_EndsTheMatchAndTheOpponentWins()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);

        // Act: it is white's turn, but the black player may resign at any time
        bool resigned = match.Resign(Guest);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resigned, Is.True);
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Finished));
            Assert.That(match.Snapshot.Result.Winner, Is.EqualTo(Player.White));
            Assert.That(match.Snapshot.Result.Reason, Is.EqualTo(EndReason.Resignation));
        }
    }

    [Test]
    public void Resign_RaisesChanged()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        int changes = 0;
        match.Changed += () => changes++;

        // Act
        match.Resign(Creator);

        // Assert
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void Resign_BeforeTheMatchStarted_IsRejected()
    {
        // Arrange
        ChessMatch match = new(Creator, random.Object);

        // Act
        bool resigned = match.Resign(Creator);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resigned, Is.False);
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        }
    }

    [Test]
    public void Resign_ByAStranger_IsRejected()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);

        // Act
        bool resigned = match.Resign("Carl");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resigned, Is.False);
            Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Running));
        }
    }

    [Test]
    public void Resign_AfterTheMatchIsFinished_IsRejected()
    {
        // Arrange
        ChessMatch match = StartedMatch(0);
        match.Resign(Guest);

        // Act
        bool resignedAgain = match.Resign(Creator);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resignedAgain, Is.False);
            Assert.That(match.Snapshot.Result.Winner, Is.EqualTo(Player.White));
        }
    }

    #endregion
}