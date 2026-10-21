using Bunit;
using ChessLogic.Enums;
using ChessLogic.Model.Game;
using ChessUI.Shared.Components;

namespace ChessUI.Shared.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class GameOverDialogTests : BunitContext
{
    [TearDown]
    public void TearDown() => Dispose();

    [TestCase(Player.White, EndReason.Checkmate, Player.Black, "WHITE WINS!", "CHECKMATE - BLACK CAN'T MOVE")]
    [TestCase(Player.Black, EndReason.Checkmate, Player.White, "BLACK WINS!", "CHECKMATE - WHITE CAN'T MOVE")]
    [TestCase(Player.None, EndReason.Stalemate, Player.Black, "IT's A DRAW!", "STALEMATE - BLACK CAN'T MOVE")]
    [TestCase(Player.None, EndReason.FiftyMoveRule, Player.White, "IT's A DRAW!", "FIFTY-MOVE RULE")]
    [TestCase(Player.None, EndReason.InsufficientMaterial, Player.White, "IT's A DRAW!", "INSUFFICIENT MATERIAL")]
    [TestCase(Player.None, EndReason.ThreefoldRepetition, Player.Black, "IT's A DRAW!", "THREEFOLD REPETITION")]
    [TestCase(Player.White, EndReason.Resignation, Player.White, "WHITE WINS!", "BLACK RESIGNED")]
    [TestCase(Player.Black, EndReason.Resignation, Player.Black, "BLACK WINS!", "WHITE RESIGNED")]
    public void Render_ShowsWinnerAndReason(
        Player winner, EndReason reason, Player currentPlayer, string expectedTitle, string expectedReason)
    {
        // Arrange: the values come from the TestCase attributes
        Result result = new(winner, reason);

        // Act
        var cut = Render<GameOverDialog>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.CurrentPlayer, currentPlayer));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".menu-title").TextContent, Is.EqualTo(expectedTitle));
            Assert.That(cut.Find(".menu-reason").TextContent, Is.EqualTo(expectedReason));
        }
    }

    [Test]
    public void Render_ShowsTheActionsInsideTheDialog()
    {
        // Arrange
        Result result = Result.Win(Player.White);

        // Act
        var cut = Render<GameOverDialog>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.CurrentPlayer, Player.Black)
            .Add(p => p.Actions, "<button class=\"again\">Again</button>"));

        // Assert
        Assert.That(cut.Find(".overlay .menu-row .again").TextContent, Is.EqualTo("Again"));
    }
}
