using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.Sessions;

namespace ChessUI.Teams.Tests.Integration;

/// <summary>
/// Everything except the pages is real here: the match service, the matches, the sessions and
/// the random source. Two "players" use the same match, like two browsers do
/// </summary>
[TestFixture]
public class MatchSessionIntegrationTests
{
    private MatchService matches;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        matches = new MatchService(new SystemRandomSource());
    }

    // Both players ask for a match in the chat. Returns the sessions as (white, black)
    private (OnlineGameSession White, OnlineGameSession Black) StartAMatch(string chat = "tenant:chat-1")
    {
        OnlineGameSession anna = new(matches.FindOrCreate(chat, "anna"), "anna");
        OnlineGameSession ben = new(matches.FindOrCreate(chat, "ben"), "ben");

        return anna.LocalPlayer == ChessLogic.Enums.Player.White ? (anna, ben) : (ben, anna);
    }

    private static Position Square(string square) => new(8 - (square[1] - '0'), square[0] - 'a');

    private static async Task<bool> PlayAsync(OnlineGameSession session, string from, string to)
    {
        Move move = session.LegalMovesFor(Square(from)).FirstOrDefault(m => m.ToPos == Square(to));

        return move != null && await session.TryMakeMoveAsync(move);
    }

    private static async Task PlayFoolsMateAsync(OnlineGameSession white, OnlineGameSession black)
    {
        await PlayAsync(white, "f2", "f3");
        await PlayAsync(black, "e7", "e5");
        await PlayAsync(white, "g2", "g4");
        await PlayAsync(black, "d8", "h4");
    }

    #endregion

    #region Starting

    [Test]
    public void TwoPlayersInTheSameChat_AlwaysGetOppositeColors()
    {
        // Arrange
        List<(Player, Player)> colors = [];

        // Act
        for (int i = 0; i < 30; i++)
        {
            var (white, black) = StartAMatch($"tenant:chat-{i}");
            colors.Add((white.LocalPlayer, black.LocalPlayer));
        }

        // Assert
        Assert.That(colors.All(pair => pair == (Player.White, Player.Black)), Is.True);
    }

    [Test]
    public void TwoPlayersInTheSameChat_ShareOneRunningMatchAtTheInitialPosition()
    {
        // Arrange
        // Act
        var (white, black) = StartAMatch();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(white.HasStarted, Is.True);
            Assert.That(black.HasStarted, Is.True);
            Assert.That(white.Board.CountPieces().TotalCount, Is.EqualTo(32));
            Assert.That(white.IsLocalTurn, Is.True);
            Assert.That(black.IsLocalTurn, Is.False);
        }
    }

    [Test]
    public void TheFirstPlayerWaits_UntilTheSecondOneArrives()
    {
        // Arrange
        OnlineGameSession anna = new(matches.FindOrCreate("tenant:chat-1", "anna"), "anna");
        int changes = 0;
        anna.Changed += () => changes++;

        // Act
        matches.FindOrCreate("tenant:chat-1", "ben");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(anna.HasStarted, Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }
    }

    #endregion

    #region Playing

    [Test]
    public async Task AMoveOfOnePlayer_IsVisibleToTheOtherAndPassesTheTurn()
    {
        // Arrange
        var (white, black) = StartAMatch();

        // Act
        bool played = await PlayAsync(white, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.True);
            Assert.That(black.Board[Square("e4")].Type, Is.EqualTo(PieceType.Pawn));
            Assert.That(black.Board[Square("e2")], Is.Null);
            Assert.That(white.IsLocalTurn, Is.False);
            Assert.That(black.IsLocalTurn, Is.True);
            Assert.That(white.Board, Is.SameAs(black.Board));
        }
    }

    [Test]
    public async Task AMove_NotifiesBothPlayers()
    {
        // Arrange
        var (white, black) = StartAMatch();
        int whiteChanges = 0;
        int blackChanges = 0;
        white.Changed += () => whiteChanges++;
        black.Changed += () => blackChanges++;

        // Act
        await PlayAsync(white, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(whiteChanges, Is.EqualTo(1));
            Assert.That(blackChanges, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ThePlayerWhoIsNotOnTurn_CannotMove()
    {
        // Arrange
        var (white, black) = StartAMatch();

        // Act
        bool blackStarts = await PlayAsync(black, "e7", "e5");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(blackStarts, Is.False);
            Assert.That(white.IsLocalTurn, Is.True);
        }
    }

    [Test]
    public async Task APlayer_CannotMoveThePiecesOfTheOpponent()
    {
        // Arrange
        var (white, black) = StartAMatch();

        // Act: black tries to push a white pawn while it is white's turn
        bool played = await PlayAsync(black, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(played, Is.False);
            Assert.That(white.Board[Square("e2")], Is.Not.Null);
        }
    }

    [Test]
    public async Task AnIllegalMove_IsRejected()
    {
        // Arrange
        var (white, _) = StartAMatch();
        Move illegal = new NormalMove(Square("e2"), Square("e5"));

        // Act
        bool played = await white.TryMakeMoveAsync(illegal);

        // Assert
        Assert.That(played, Is.False);
    }

    #endregion

    #region The end

    [Test]
    public async Task ACheckmate_EndsTheGameForBothPlayersWithTheSameResult()
    {
        // Arrange
        var (white, black) = StartAMatch();

        // Act
        await PlayFoolsMateAsync(white, black);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(white.IsGameOver, Is.True);
            Assert.That(black.IsGameOver, Is.True);
            Assert.That(white.Result.Winner, Is.EqualTo(Player.Black));
            Assert.That(white.Result.Reason, Is.EqualTo(EndReason.Checkmate));
            Assert.That(black.Result, Is.SameAs(white.Result));
            Assert.That(white.CanResign, Is.False);
        }
    }

    [Test]
    public async Task AfterTheEnd_NobodyCanMoveAnyMore()
    {
        // Arrange
        var (white, black) = StartAMatch();
        await PlayFoolsMateAsync(white, black);

        // Act
        bool played = await PlayAsync(white, "a2", "a3");

        // Assert
        Assert.That(played, Is.False);
    }

    [Test]
    public void Resigning_EndsTheGameForBothPlayers()
    {
        // Arrange
        var (white, black) = StartAMatch();

        // Act
        bool resigned = black.Resign();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resigned, Is.True);
            Assert.That(white.IsGameOver, Is.True);
            Assert.That(black.IsGameOver, Is.True);
            Assert.That(white.Result.Winner, Is.EqualTo(Player.White));
            Assert.That(white.Result.Reason, Is.EqualTo(EndReason.Resignation));
        }
    }

    [Test]
    public async Task ARematch_StartsAFreshGameInTheSameChat()
    {
        // Arrange
        var (white, black) = StartAMatch();
        await PlayFoolsMateAsync(white, black);

        // Act: both players ask for a new match
        OnlineGameSession first = new(matches.FindOrCreate("tenant:chat-1", "anna"), "anna");
        bool waitedFirst = !first.HasStarted;
        OnlineGameSession second = new(matches.FindOrCreate("tenant:chat-1", "ben"), "ben");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(waitedFirst, Is.True);
            Assert.That(first.HasStarted, Is.True);
            Assert.That(first.IsGameOver, Is.False);
            Assert.That(first.Board.CountPieces().TotalCount, Is.EqualTo(32));
            Assert.That(second.Board, Is.SameAs(first.Board));
        }
    }

    #endregion

    #region Reaching a running match

    [Test]
    public async Task APlayerWhoReloadsThePage_RejoinsTheRunningMatchWithTheSameColor()
    {
        // Arrange
        OnlineGameSession anna = new(matches.FindOrCreate("tenant:chat-1", "anna"), "anna");
        OnlineGameSession ben = new(matches.FindOrCreate("tenant:chat-1", "ben"), "ben");
        OnlineGameSession white = anna.LocalPlayer == Player.White ? anna : ben;
        string whiteUser = white == anna ? "anna" : "ben";
        await PlayAsync(white, "e2", "e4");

        // Act: the white player opens the page again, which creates a new session
        OnlineGameSession reloaded = new(matches.FindOrCreate("tenant:chat-1", whiteUser), whiteUser);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reloaded.LocalPlayer, Is.EqualTo(Player.White));
            Assert.That(reloaded.Board[Square("e4")].Type, Is.EqualTo(PieceType.Pawn));
            Assert.That(reloaded.IsLocalTurn, Is.False);
        }
    }

    [Test]
    public void AThirdPersonInTheChat_GetsNoMatch()
    {
        // Arrange
        StartAMatch();

        // Act
        ChessMatch match = matches.FindOrCreate("tenant:chat-1", "carl");

        // Assert
        Assert.That(match, Is.Null);
    }

    [Test]
    public async Task DifferentChats_PlayIndependently()
    {
        // Arrange
        var (whiteA, _) = StartAMatch("tenant:chat-A");
        var (whiteB, _) = StartAMatch("tenant:chat-B");

        // Act
        await PlayAsync(whiteA, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(whiteA.Board[Square("e4")], Is.Not.Null);
            Assert.That(whiteB.Board[Square("e4")], Is.Null);
            Assert.That(whiteB.IsLocalTurn, Is.True);
        }
    }

    [Test]
    public void ADisposedSession_ReceivesNoMoreChanges()
    {
        // Arrange
        var (white, black) = StartAMatch();
        int changes = 0;
        black.Changed += () => changes++;
        black.Dispose();

        // Act
        white.Resign();

        // Assert
        Assert.That(changes, Is.EqualTo(0));
    }

    #endregion

    #region Under load

    [Test]
    public async Task ManyMatchesAtTheSameTime_AllFinishCorrectly()
    {
        // Arrange: a hundred chats play the fastest possible game at the same moment
        const int chats = 100;

        // Act
        OnlineGameSession[][] results = await Task.WhenAll(Enumerable.Range(0, chats).Select(i => Task.Run(async () =>
        {
            var (white, black) = StartAMatch($"tenant:chat-{i}");
            await PlayFoolsMateAsync(white, black);
            return new[] { white, black };
        })));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Length, Is.EqualTo(chats));
            Assert.That(results.All(pair => pair[0].IsGameOver && pair[0].Result.Winner == Player.Black), Is.True);
        }
    }

    #endregion
}
