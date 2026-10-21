using Bunit;
using ChessUI.Teams.Components.Pages;

namespace ChessUI.Teams.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class TeamsConfigTests : BunitContext
{
    [TearDown]
    public void TearDown() => Dispose();

    [Test]
    public void Render_ExplainsWhatTheSaveButtonOfTeamsDoes()
    {
        // Arrange
        JSInterop.SetupVoid("chessTeams.registerConfig");

        // Act
        var cut = Render<TeamsConfig>();

        // Assert
        Assert.That(cut.Find(".menu-reason").TextContent, Is.EqualTo("Press \"Save\" to add Chess Power to this chat."));
    }

    [Test]
    public void Render_RegistersTheTabConfigurationWithTeamsExactlyOnce()
    {
        // Arrange
        JSInterop.SetupVoid("chessTeams.registerConfig");

        // Act
        var cut = Render<TeamsConfig>();

        // Assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("chessTeams.registerConfig", 1));
    }
}
