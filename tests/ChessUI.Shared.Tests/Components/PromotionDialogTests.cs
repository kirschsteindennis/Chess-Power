using Bunit;
using ChessLogic.Enums;
using ChessUI.Shared.Components;
using ChessUI.Shared.Rendering;

namespace ChessUI.Shared.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class PromotionDialogTests : BunitContext
{
    [TearDown]
    public void TearDown() => Dispose();

    [Test]
    public void Render_ShowsTheTitleAndFourPieces()
    {
        // Arrange
        // Act
        var cut = Render<PromotionDialog>(parameters => parameters
            .Add(p => p.Color, Player.White));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".menu-title").TextContent, Is.EqualTo("SELECT A PIECE"));
            Assert.That(cut.FindAll("button.promotion-piece"), Has.Count.EqualTo(4));
        }
    }

    [TestCase(0, PieceType.Queen)]
    [TestCase(1, PieceType.Rook)]
    [TestCase(2, PieceType.Bishop)]
    [TestCase(3, PieceType.Knight)]
    public void Click_ReportsThePieceThatWasChosen(int buttonIndex, PieceType expected)
    {
        // Arrange
        PieceType? chosen = null;
        var cut = Render<PromotionDialog>(parameters => parameters
            .Add(p => p.Color, Player.White)
            .Add(p => p.PieceSelected, (PieceType type) => chosen = type));

        // Act
        cut.FindAll("button.promotion-piece")[buttonIndex].Click();

        // Assert
        Assert.That(chosen, Is.EqualTo(expected));
    }

    [Test]
    public void Render_ShowsTheDrawingOfEachPiece()
    {
        // Arrange
        PieceType[] order = [PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight];

        // Act
        var cut = Render<PromotionDialog>(parameters => parameters
            .Add(p => p.Color, Player.White));

        // Assert
        var buttons = cut.FindAll("button.promotion-piece");
        for (int i = 0; i < order.Length; i++)
        {
            Assert.That(PieceDrawing.PathDataOf(buttons[i]),
                Is.EqualTo(PieceDrawing.PathDataOf(PieceSvg.Get(Player.White, order[i]))),
                $"button {i} should show a {order[i]}");
        }
    }

    [TestCase(Player.White, "#000000", "#FFFFFF")]
    [TestCase(Player.Black, "#FFFFFF", "#000000")]
    public void Render_ColorsThePiecesForTheGivenPlayer(Player player, string rim, string main)
    {
        // Arrange: the values come from the TestCase attributes

        // Act
        var cut = Render<PromotionDialog>(parameters => parameters
            .Add(p => p.Color, player));

        // Assert: every drawing has an outline path (rim) and a filling path (main color)
        Assert.That(PieceDrawing.FillsOf(cut.FindAll("button.promotion-piece")[0]), Is.EqualTo(new[] { rim, main }));
    }
}
