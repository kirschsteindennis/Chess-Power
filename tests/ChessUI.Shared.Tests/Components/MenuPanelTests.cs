using Bunit;
using ChessUI.Shared.Components;

namespace ChessUI.Shared.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MenuPanelTests : BunitContext
{
    [TearDown]
    public void TearDown() => Dispose();

    [Test]
    public void REnder_WithATitle_ShowsItAsAHeading()
    {
        // Arrange
        // Act
        var cut = Render<MenuPanel>(parameters => parameters
            .Add(p => p.Title, "Chess Power"));

        // Assert
        Assert.That(cut.Find("h2.menu-title").TextContent, Is.EqualTo("Chess Power"));
    }

    [Test]
    public void Render_WithoutATitle_ShowsNoHeading()
    {
        // Arrange
        // Act
        var cut = Render<MenuPanel>();

        // Assert
        Assert.That(cut.FindAll("h2"), Is.Empty);
    }

    [Test]
    public void Render_PutsTheChildContentInsideTheCard()
    {
        // Arrange
        // Act: the child content is what the buttons of a menu are passed as
        var cut = Render<MenuPanel>(parameters => parameters
                .Add(p => p.Title, "Menu")
                .AddChildContent("<p class=\"inner\">hello</p>"));

        // Assert
        Assert.That(cut.Find(".menu-card .inner").TextContent, Is.EqualTo("hello"));
    }
}
