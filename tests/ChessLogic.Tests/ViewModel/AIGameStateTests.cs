using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Services;
using Chess.Test.Shared.Support;
using ChessLogic.ViewModel;
using Moq;

namespace ChessLogic.Tests.ViewModel;

[TestFixture]
public class AIGameStateTests
{
    private Mock<IRandomSource> random;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        random = new Mock<IRandomSource>();
    }

    private AIGameState CreateGame(AIDifficulty difficulty, Board board = null)
    {
        return new AIGameState(Player.Black, board ?? Board.Initial(), difficulty, random.Object);
    }

    #endregion

    [Test]
    public void MakeAIMove_Easy_PlaysTheMoveThatTheRandomSourcePicks()
    {
        // Arrange: black has 20 moves, the random source picks the last one (index 19)
        AIGameState game = CreateGame(AIDifficulty.Easy);
        Move expected = game.AllLegalMovesFor(Player.Black).ElementAt(19);
        random.Setup(r => r.Next(20)).Returns(19);

        // Act
        game.MakeAIMove();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Board[expected.FromPos], Is.Null);
            Assert.That(game.Board[expected.ToPos].Color, Is.EqualTo(Player.Black));
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White));
        }
        random.Verify(r => r.Next(20), Times.Once());
    }

    [Test]
    public void MakeAIMove_Medium_ChoosesAmongTheBetterHalfOfTheMoves()
    {
        // Arrange: pawn moves come first, and the better half of 20 moves is 10 moves
        AIGameState game = CreateGame(AIDifficulty.Medium);
        random.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        // Act
        game.MakeAIMove();

        // Assert
        int pawnsOnTheirStartRow = game.Board.PiecePositionsFor(Player.Black)
            .Count(pos => game.Board[pos].Type == PieceType.Pawn && pos.Row == 1);

        Assert.Multiple(() =>
        {
            Assert.That(pawnsOnTheirStartRow, Is.EqualTo(7));
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White));
        });
        random.Verify(r => r.Next(10), Times.Once());
    }

    [Test]
    public void MakeAIMove_Hard_CapturesAnUndefendedQueen()
    {
        // Arrange
        Board board = new BoardBuilder()
            .Black(PieceType.King, "g8")
            .Black(PieceType.Rook, "d8")
            .White(PieceType.King, "a1")
            .White(PieceType.Queen, "d4")
            .Build();
        AIGameState game = CreateGame(AIDifficulty.Hard, board);

        // Act
        game.MakeAIMove();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(game.Board[Sq.At("d4")].Type, Is.EqualTo(PieceType.Rook));
            Assert.That(game.Board[Sq.At("d4")].Color, Is.EqualTo(Player.Black));
        });
    }

    [Test]
    public void MakeAIMove_Hard_DoesNotNeedTheRandomSource()
    {
        // Arrange
        AIGameState game = CreateGame(AIDifficulty.Hard);

        // Act
        game.MakeAIMove();

        // Assert
        random.Verify(r => r.Next(It.IsAny<int>()), Times.Never());
    }

    [TestCase(AIDifficulty.Easy)]
    [TestCase(AIDifficulty.Medium)]
    [TestCase(AIDifficulty.Hard)]
    public void MakeAIMove_OnEveryDifficulty_PlaysALegalMoveAndPassesTheTurn(AIDifficulty difficulty)
    {
        // Arrange
        AIGameState game = CreateGame(difficulty);
        random.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        // Act
        game.MakeAIMove();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White));
            Assert.That(game.Board.CountPieces().TotalCount, Is.EqualTo(32));
            Assert.That(game.Board.PiecePositionsFor(Player.Black).Count(), Is.EqualTo(16));
        });
    }

    [Test]
    public void MakeAIMove_WhenTheComputerHasNoMoves_DoesNothing()
    {
        // Arrange: black is stalemated
        Board board = new BoardBuilder()
            .Black(PieceType.King, "a8")
            .White(PieceType.King, "c7")
            .White(PieceType.Queen, "b6")
            .Build();
        AIGameState game = CreateGame(AIDifficulty.Easy, board);

        // Act
        game.MakeAIMove();

        // Assert
        Assert.That(game.CurrentPlayer, Is.EqualTo(Player.Black));
        random.Verify(r => r.Next(It.IsAny<int>()), Times.Never());
    }

    [Test]
    public void MakeAIMove_WithoutARandomSource_UsesTheDefaultOne()
    {
        // Arrange
        AIGameState game = new(Player.Black, Board.Initial(), AIDifficulty.Easy);

        // Act
        game.MakeAIMove();

        // Assert
        Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White));
    }
}
