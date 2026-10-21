using Bunit;
using ChessUI.Shared.Components;

namespace ChessUI.Shared.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MenuButtonTests : BunitContext
{
    [TearDown]
    public void TearDown() => Dispose();

    [Test]
    public void Render_ShowsTheText()
    {
        // Arrange
        // Act
        var cut = Render<MenuButton>(parameters => parameters
            .Add(p => p.Text, "Find opponent"));

        // Assert
        Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find opponent"));
    }

    [Test]
    public void Click_InvokesTheCallbackOnce()
    {
        // Arrange
        int clicks = 0;
        var cut = Render<MenuButton>(parameters => parameters
            .Add(p => p.Text, "Go")
            .Add(p => p.OnClick, () => clicks++));

        // Act
        cut.Find("button").Click();

        // Assert
        Assert.That(clicks, Is.EqualTo(1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Render_TheDisabledParameterDecidesIfTheButtonIsDisabled(bool disabled)
    {
        // Arrange: the value comes from the TestCase attributes

        // Act
        var cut = Render<MenuButton>(parameters => parameters
            .Add(p => p.Text, "Go")
            .Add(p => p.Disabled, disabled));

        // Assert
        Assert.That(cut.Find("button").HasAttribute("disabled"), Is.EqualTo(disabled));
    }
}
