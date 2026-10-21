using AngleSharp.Dom;
using Bunit;
using Chess.Test.Shared.Support;
using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using ChessLogic.ViewModel;
using ChessUI.Shared.Rendering;
using ChessUI.Shared.Services;
using ChessUI.Shared.Components;
using Moq;

namespace ChessUI.Shared.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ChessBoardTests : BunitContext
{
    private GameState game;
    private Mock<IGameSession> session;
    private List<Move> playedMoves;

    [SetUp]
    public void SetUp()
    {
        UseGame(new GameState(Player.White, Board.Initial()));
    }

    [TearDown]
    public void TearDown() => Dispose();

    #region Helper Functions

    // The mock plays the role of the session: it hands out the board and the legal moves of a real game
    private void UseGame(GameState newGame, Player localPlayer = Player.White)
    {
        game = newGame;
        playedMoves = [];

        session = new Mock<IGameSession>();
        session.Setup(s => s.Board).Returns(game.Board);
        session.Setup(s => s.CurrentPlayer).Returns(game.CurrentPlayer);
        session.Setup(s => s.LocalPlayer).Returns(localPlayer);
        session.Setup(s => s.IsLocalTurn).Returns(true);
        session.Setup(s => s.IsGameOver).Returns(false);
        session.Setup(s => s.LegalMovesFor(It.IsAny<Position>()))
            .Returns((Position pos) => game.LegalMovesForPiece(pos));
        session.Setup(s => s.TryMakeMoveAsync(It.IsAny<Move>()))
            .Returns((Move move) =>
            {
                playedMoves.Add(move);
                return Task.FromResult(true);
            });
    }

    private IRenderedComponent<ChessBoard> RenderBoard()
    {
        return Render<ChessBoard>(parameters => parameters
            .Add(p => p.Session, session.Object));
    }

    // The squares are drawn row by row from the top. White sees rank 8 first, black sees rank 1 first.
    private static IElement SquareAt(IRenderedComponent<ChessBoard> cut, string square, bool blackPerspective = false)
    {
        int row = 8 - (square[1] - '0');
        int column = square[0] - 'a';
        int index = blackPerspective ? (7 - row) * 8 + (7 - column) : row * 8 + column;

        return cut.FindAll(".chess-board .square")[index];
    }

    private static IElement PieceOn(IRenderedComponent<ChessBoard> cut, string square, bool blackPerspective = false)
    {
        return SquareAt(cut, square, blackPerspective).QuerySelectorAll(".piece")[0];
    }

    private static string Status(IRenderedComponent<ChessBoard> cut) => cut.Find(".status").TextContent;

    private static Board BoardWithPromotionChance()
    {
        return new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "h5")
            .White(PieceType.Pawn, "a7")
            .Build();
    }

    #endregion

    #region Drawing the board

    [Test]
    public void Render_ShowsSixtyFourSquaresAndThirtyTwoPieces()
    {
        // Arrange
        // Act
        var cut = RenderBoard();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll(".chess-board .square"), Has.Count.EqualTo(64));
            Assert.That(cut.FindAll(".chess-board .piece"), Has.Count.EqualTo(32));
        }
    }

    [Test]
    public void Render_AlternatesLightAndDarkSquares()
    {
        // Arrange
        // Act
        var cut = RenderBoard();

        // Assert
        var squares = cut.FindAll(".chess-board .square");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(squares[0].ClassList.Contains("light"), Is.True);
            Assert.That(squares[1].ClassList.Contains("dark"), Is.True);
            Assert.That(cut.FindAll(".square.light"), Has.Count.EqualTo(32));
            Assert.That(cut.FindAll(".square.dark"), Has.Count.EqualTo(32));
        }
    }

    [TestCase("e1", Player.White, PieceType.King)]
    [TestCase("d1", Player.White, PieceType.Queen)]
    [TestCase("a1", Player.White, PieceType.Rook)]
    [TestCase("c1", Player.White, PieceType.Bishop)]
    [TestCase("b1", Player.White, PieceType.Knight)]
    [TestCase("h2", Player.White, PieceType.Pawn)]
    [TestCase("e8", Player.Black, PieceType.King)]
    [TestCase("g8", Player.Black, PieceType.Knight)]
    public void Render_DrawsEachPieceOnItsStartSquare(string square, Player color, PieceType type)
    {
        // Arrange: the values come from the TestCase attributes

        // Act
        var cut = RenderBoard();

        // Assert
        Assert.That(PieceDrawing.PathDataOf(PieceOn(cut, square)),
            Is.EqualTo(PieceDrawing.PathDataOf(PieceSvg.Get(color, type))));
    }

    [Test]
    public void Render_ColorsTheWhitePiecesWhiteAndTheBlackPiecesBlack()
    {
        // Arrange
        // Act
        var cut = RenderBoard();

        // Assert: rim first, then main color
        using (Assert.EnterMultipleScope())
        {
            Assert.That(PieceDrawing.FillsOf(PieceOn(cut, "e1")), Is.EqualTo(["#000000", "#FFFFFF"]));
            Assert.That(PieceDrawing.FillsOf(PieceOn(cut, "e8")), Is.EqualTo(["#FFFFFF", "#000000"]));
        }
    }

    [Test]
    public void Render_EmptySquaresShowNoPiece()
    {
        // Arrange
        // Act
        var cut = RenderBoard();

        // Assert
        Assert.That(SquareAt(cut, "e4").QuerySelectorAll(".piece"), Is.Empty);
    }

    [Test]
    public void Render_ForBlack_ShowsTheBoardTurnedAround()
    {
        // Arrange
        UseGame(new GameState(Player.White, Board.Initial()), localPlayer: Player.Black);

        // Act
        var cut = RenderBoard();

        // Assert: the first square is h1 (white rook), and both kings are where they belong
        var squares = cut.FindAll(".chess-board .square");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(PieceDrawing.PathDataOf(squares[0].QuerySelectorAll(".piece")[0]),
                Is.EqualTo(PieceDrawing.PathDataOf(PieceSvg.Get(Player.White, PieceType.Rook))));
            Assert.That(PieceDrawing.PathDataOf(PieceOn(cut, "e1", blackPerspective: true)),
                Is.EqualTo(PieceDrawing.PathDataOf(PieceSvg.Get(Player.White, PieceType.King))));
            Assert.That(PieceDrawing.PathDataOf(PieceOn(cut, "e8", blackPerspective: true)),
                Is.EqualTo(PieceDrawing.PathDataOf(PieceSvg.Get(Player.Black, PieceType.King))));
        }
    }

    [TestCase(Player.White, true, "You play White - your turn")]
    [TestCase(Player.White, false, "You play White - waiting for opponent")]
    [TestCase(Player.Black, true, "You play Black - your turn")]
    [TestCase(Player.Black, false, "You play Black - waiting for opponent")]
    public void Render_ShowsWhoPlaysWhichColorAndWhoseTurnItIs(Player local, bool isLocalTurn, string expected)
    {
        // Arrange
        UseGame(new GameState(Player.White, Board.Initial()), localPlayer: local);
        session.Setup(s => s.IsLocalTurn).Returns(isLocalTurn);

        // Act
        var cut = RenderBoard();

        // Assert
        Assert.That(Status(cut), Is.EqualTo(expected));
    }

    #endregion

    #region Selecting and moving

    [Test]
    public void Click_OnAPieceWithMoves_SelectsItAndShowsTheTargets()
    {
        // Arrange
        var cut = RenderBoard();

        // Act
        SquareAt(cut, "e2").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SquareAt(cut, "e2").ClassList.Contains("selected"), Is.True);
            Assert.That(cut.FindAll(".square.selected"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".hint"), Has.Count.EqualTo(2));
            Assert.That(SquareAt(cut, "e3").QuerySelectorAll(".hint"), Has.Count.EqualTo(1));
            Assert.That(SquareAt(cut, "e4").QuerySelectorAll(".hint"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Click_OnAPieceWithoutMoves_SelectsNothing()
    {
        // Arrange: the rook in the corner is blocked by its own pieces
        var cut = RenderBoard();

        // Act
        SquareAt(cut, "a1").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll(".square.selected"), Is.Empty);
            Assert.That(cut.FindAll(".hint"), Is.Empty);
        }
    }

    [Test]
    public void Click_OnAnEmptySquare_SelectsNothing()
    {
        // Arrange
        var cut = RenderBoard();

        // Act
        SquareAt(cut, "e4").Click();

        // Assert
        Assert.That(cut.FindAll(".square.selected"), Is.Empty);
    }

    [Test]
    public void Click_OnATarget_PlaysTheMoveAndClearsTheSelection()
    {
        // Arrange
        var cut = RenderBoard();
        SquareAt(cut, "e2").Click();

        // Act
        SquareAt(cut, "e4").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(playedMoves, Has.Count.EqualTo(1));
            Assert.That(playedMoves[0].FromPos, Is.EqualTo(new Position(6, 4)));
            Assert.That(playedMoves[0].ToPos, Is.EqualTo(new Position(4, 4)));
            Assert.That(cut.FindAll(".square.selected"), Is.Empty);
            Assert.That(cut.FindAll(".hint"), Is.Empty);
        }
    }

    [Test]
    public void Click_OnAnotherOwnPiece_MovesTheSelection()
    {
        // Arrange
        var cut = RenderBoard();
        SquareAt(cut, "e2").Click();

        // Act
        SquareAt(cut, "g1").Click();

        // Assert: the knight on g1 can go to f3 and h3
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SquareAt(cut, "g1").ClassList.Contains("selected"), Is.True);
            Assert.That(SquareAt(cut, "e2").ClassList.Contains("selected"), Is.False);
            Assert.That(cut.FindAll(".hint"), Has.Count.EqualTo(2));
            Assert.That(SquareAt(cut, "f3").QuerySelectorAll(".hint"), Has.Count.EqualTo(1));
            Assert.That(playedMoves, Is.Empty);
        }
    }

    [Test]
    public void Click_OnASquareThatIsNoTarget_ClearsTheSelection()
    {
        // Arrange
        var cut = RenderBoard();
        SquareAt(cut, "e2").Click();

        // Act
        SquareAt(cut, "e6").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll(".square.selected"), Is.Empty);
            Assert.That(cut.FindAll(".hint"), Is.Empty);
            Assert.That(playedMoves, Is.Empty);
        }
    }

    [Test]
    public void Click_WhenItIsNotTheLocalTurn_DoesNothing()
    {
        // Arrange
        session.Setup(s => s.IsLocalTurn).Returns(false);
        var cut = RenderBoard();

        // Act
        SquareAt(cut, "e2").Click();

        // Assert
        Assert.That(cut.FindAll(".square.selected"), Is.Empty);
        session.Verify(s => s.LegalMovesFor(It.IsAny<Position>()), Times.Never());
    }

    [Test]
    public void Click_WhenTheGameIsOver_DoesNothing()
    {
        // Arrange
        session.Setup(s => s.IsGameOver).Returns(true);
        session.Setup(s => s.Result).Returns(Result.Win(Player.White));
        var cut = RenderBoard();

        // Act
        SquareAt(cut, "e2").Click();

        // Assert
        Assert.That(cut.FindAll(".square.selected"), Is.Empty);
        session.Verify(s => s.LegalMovesFor(It.IsAny<Position>()), Times.Never());
    }

    #endregion

    #region Promotion

    [Test]
    public void Click_OnAPromotionSquare_AsksWhichPieceTheUserWantsBeforePlaying()
    {
        // Arrange
        UseGame(new GameState(Player.White, BoardWithPromotionChance()));
        var cut = RenderBoard();
        SquareAt(cut, "a7").Click();

        // Act
        SquareAt(cut, "a8").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("button.promotion-piece"), Has.Count.EqualTo(4));
            Assert.That(playedMoves, Is.Empty);
        }
    }

    [TestCase(0, PieceType.Queen)]
    [TestCase(1, PieceType.Rook)]
    [TestCase(2, PieceType.Bishop)]
    [TestCase(3, PieceType.Knight)]
    public void Click_OnAPieceInThePromotionDialog_PlaysThePromotionWithThatPiece(int buttonIndex, PieceType expected)
    {
        // Arrange
        UseGame(new GameState(Player.White, BoardWithPromotionChance()));
        var cut = RenderBoard();
        SquareAt(cut, "a7").Click();
        SquareAt(cut, "a8").Click();

        // Act
        cut.FindAll("button.promotion-piece")[buttonIndex].Click();

        // Assert: playing the move on a copy of the board shows which piece it creates
        Assert.That(playedMoves, Has.Count.EqualTo(1));
        Assert.That(playedMoves[0].Type, Is.EqualTo(MoveType.PawnPromotion));

        Board copy = game.Board.Copy();
        playedMoves[0].Execute(copy);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(copy[new Position(0, 0)].Type, Is.EqualTo(expected));
            Assert.That(cut.FindAll("button.promotion-piece"), Is.Empty);
            Assert.That(cut.FindAll(".square.selected"), Is.Empty);
        }
    }

    [Test]
    public void Click_OnTheBoardWhileThePromotionDialogIsOpen_IsIgnored()
    {
        // Arrange
        UseGame(new GameState(Player.White, BoardWithPromotionChance()));
        var cut = RenderBoard();
        SquareAt(cut, "a7").Click();
        SquareAt(cut, "a8").Click();

        // Act
        SquareAt(cut, "e1").Click();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("button.promotion-piece"), Has.Count.EqualTo(4));
            Assert.That(playedMoves, Is.Empty);
        }
        session.Verify(s => s.LegalMovesFor(new Position(7, 4)), Times.Never());
    }

    #endregion

    #region Reacting to the session

    [Test]
    public void SessionChanged_ClearsTheSelection()
    {
        // Arrange
        var cut = RenderBoard();
        SquareAt(cut, "e2").Click();

        // Act: the opponent moved, so the session reports a change
        session.Raise(s => s.Changed += null);

        // Assert
        cut.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll(".square.selected"), Is.Empty);
                Assert.That(cut.FindAll(".hint"), Is.Empty);
            }
        });
    }

    [Test]
    public void SessionChanged_DrawsTheNewPosition()
    {
        // Arrange
        var cut = RenderBoard();
        PlayOnTheGame("e2", "e4");

        // Act
        session.Raise(s => s.Changed += null);

        // Assert
        cut.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(SquareAt(cut, "e2").QuerySelectorAll(".piece"), Is.Empty);
                Assert.That(SquareAt(cut, "e4").QuerySelectorAll(".piece"), Has.Count.EqualTo(1));
            }
        });
    }

    // Plays a move directly on the game, like the opponent would do
    private void PlayOnTheGame(string from, string to)
    {
        Position fromPos = new(8 - (from[1] - '0'), from[0] - 'a');
        Position toPos = new(8 - (to[1] - '0'), to[0] - 'a');
        game.MakeMove(game.LegalMovesForPiece(fromPos).First(m => m.ToPos == toPos));
    }

    [Test]
    public void SessionChanged_WhenTheGameIsOver_ShowsTheGameOverDialog()
    {
        // Arrange
        var cut = RenderBoard();
        session.Setup(s => s.IsGameOver).Returns(true);
        session.Setup(s => s.Result).Returns(Result.Win(Player.White));
        session.Setup(s => s.CurrentPlayer).Returns(Player.Black);

        // Act
        session.Raise(s => s.Changed += null);

        // Assert
        cut.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.Find(".overlay .menu-title").TextContent, Is.EqualTo("WHITE WINS!"));
                Assert.That(cut.Find(".menu-reason").TextContent, Is.EqualTo("CHECKMATE - BLACK CAN'T MOVE"));
                Assert.That(Status(cut), Is.EqualTo("Game Over"));
            }
        });
    }

    #endregion

    #region Game Over

    [Test]
    public void Render_WhenTheGameIsOver_ShowsTheDialogWithTheActionsOfTheHost()
    {
        // Arrange
        session.Setup(s => s.IsGameOver).Returns(true);
        session.Setup(s => s.Result).Returns(Result.Draw(EndReason.Stalemate));
        session.Setup(s => s.CurrentPlayer).Returns(Player.Black);

        // Act
        var cut = Render<ChessBoard>(parameters => parameters
            .Add(p => p.Session, session.Object)
            .Add(p => p.GameOverActions, "<button class=\"again\">Again</button>"));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".overlay .menu-title").TextContent, Is.EqualTo("IT's A DRAW!"));
            Assert.That(cut.Find(".overlay .menu-row .again").TextContent, Is.EqualTo("Again"));
        }
    }

    [Test]
    public void Render_WhileTheGameRuns_ShowsNoDialog()
    {
        // Arrange
        // Act
        var cut = RenderBoard();

        // Assert
        Assert.That(cut.FindAll(".overlay"), Is.Empty);
    }

    #endregion

    #region Lifetime

    [Test]
    public async Task Dispose_StopsListeningToTheSession()
    {
        // Arrange
        RenderBoard();
        session.VerifyAdd(s => s.Changed += It.IsAny<Action>(), Times.Once());
        session.VerifyRemove(s => s.Changed -= It.IsAny<Action>(), Times.Never());

        // Act
        await DisposeComponentsAsync();

        // Assert
        session.VerifyRemove(s => s.Changed -= It.IsAny<Action>(), Times.Once());
    }

    #endregion
}