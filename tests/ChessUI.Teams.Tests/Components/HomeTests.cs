using Bunit;
using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.Components.Pages;
using ChessUI.Teams.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ChessUI.Teams.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class HomeTests : BunitContext
{
    private const string Chat = "chat-1";
    private const string Tenant = "tenant-1";

    private Mock<IRandomSource> random;
    private Mock<ITeamsTokenValidator> validator;
    private Mock<IWebHostEnvironment> environment;
    private MatchService matches;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        // The creator of a match plays white, so the tests know who has which color
        random = new Mock<IRandomSource>();
        random.Setup(r => r.Next(2)).Returns(0);
        matches = new MatchService(random.Object);

        // By default Teams delivers a valid token of the user "anna" in the tenant "tenant-1"
        validator = new Mock<ITeamsTokenValidator>();
        validator.Setup(v => v.ValidateAsync("token")).ReturnsAsync(new TeamsIdentity(Tenant, "anna"));

        environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Production");

        Services.AddSingleton(matches);
        Services.AddSingleton(validator.Object);
        Services.AddSingleton(environment.Object);
    }

    [TearDown]
    public void TearDown() => Dispose();

    // The page runs inside a Teams chat: the JavaScript part delivers the chat id and a token
    private void RunInsideTeams(string chatId = Chat, string token = "token")
    {
        JSInterop.Setup<TeamsContext>("chessTeams.getContext").SetResult(new TeamsContext(chatId, null));
        JSInterop.Setup<string>("chessTeams.getAuthToken").SetResult(token);
    }

    // The page runs in a normal browser: the JavaScript part reports "no Teams here"
    private void RunOutsideTeams()
    {
        JSInterop.Setup<TeamsContext>("chessTeams.getContext").SetResult(null);
    }

    // The page first connects to Teams (asynchronously) and only then shows its real content
    private IRenderedComponent<Home> RenderHome()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Not.Contain("Connecting to Teams")));
        return cut;
    }

    private static string Message(IRenderedComponent<Home> cut) => cut.Find(".menu-reason").TextContent;

    private static void ClickButton(IRenderedComponent<Home> cut, string text)
    {
        cut.FindAll("button.menu-button").Single(button => button.TextContent == text).Click();
    }

    // The key under which the match service stores the match of this chat
    private static string ChatKey => $"{Tenant}:{Chat}";

    #endregion

    #region Starting up

    [Test]
    public void Render_InATeamsChatWithAValidToken_OffersToFindAnOpponent()
    {
        // Arrange
        RunInsideTeams();

        // Act
        var cut = RenderHome();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".menu-title").TextContent, Is.EqualTo("Chess Power"));
            Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find Opponent"));
        }
    }

    [Test]
    public void Render_InATeamsChat_SendsTheTokenThatTeamsDeliveredToTheValidator()
    {
        // Arrange
        RunInsideTeams(token: "token");

        // Act
        RenderHome();

        // Assert
        validator.Verify(v => v.ValidateAsync("token"), Times.Once());
    }

    [Test]
    public void Render_WhenTheTokenIsNotValid_ShowsASignInErrorAndNoMenu()
    {
        // Arrange
        RunInsideTeams(token: "forged");
        validator.Setup(v => v.ValidateAsync("forged")).ReturnsAsync((TeamsIdentity)null);

        // Act
        var cut = RenderHome();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Message(cut), Is.EqualTo("Sign-in with Teams failed."));
            Assert.That(cut.FindAll("button.menu-button"), Is.Empty);
        }
    }

    [Test]
    public void Render_InATeamsTabWithoutAChat_AsksToOpenItInAChatAndSkipsTheSignIn()
    {
        // Arrange: a personal tab has no chat id
        JSInterop.Setup<TeamsContext>("chessTeams.getContext").SetResult(new TeamsContext(null, null));

        // Act
        var cut = RenderHome();

        // Assert
        Assert.That(Message(cut), Is.EqualTo("Please open this app in a Teams Chat"));
        JSInterop.VerifyNotInvoke("chessTeams.getAuthToken");
        validator.Verify(v => v.ValidateAsync(It.IsAny<string>()), Times.Never());
    }

    [Test]
    public void Render_OutsideOfTeams_AsksToOpenItInATeamsChat()
    {
        // Arrange
        RunOutsideTeams();

        // Act
        var cut = RenderHome();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Message(cut), Is.EqualTo("Please open this app in a Teams chat."));
            Assert.That(cut.FindAll("button.menu-button"), Is.Empty);
        }
    }

    [Test]
    public void Render_OutsideOfTeams_InDevelopmentWithTestParameters_OffersTheMenu()
    {
        // Arrange
        RunOutsideTeams();
        environment.Setup(e => e.EnvironmentName).Returns("Development");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/?chat=demo&user=anna");

        // Act
        var cut = RenderHome();

        // Assert
        Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find Opponent"));
    }

    [Test]
    public void Render_OutsideOfTeams_InProductionWithTestParameters_IgnoresThem()
    {
        // Arrange: anybody could type such an address, so production must not accept it
        RunOutsideTeams();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/?chat=demo&user=anna");

        // Act
        var cut = RenderHome();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Message(cut), Is.EqualTo("Please open this app in a Teams chat."));
            Assert.That(cut.FindAll("button.menu-button"), Is.Empty);
        }
    }

    #endregion

    #region Finding an opponent

    [Test]
    public void FindOpponent_ShowsTheWaitingScreenWithACancelButton()
    {
        // Arrange
        RunInsideTeams();
        var cut = RenderHome();

        // Act
        ClickButton(cut, "Find Opponent");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".menu-title").TextContent, Is.EqualTo("Waiting..."));
            Assert.That(Message(cut), Is.EqualTo("Waiting for another person in this chat to open the game"));
            Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Cancel"));
        }
    }

    [Test]
    public void FindOpponent_KeysTheMatchByTenantAndChat()
    {
        // Arrange
        RunInsideTeams();
        var cut = RenderHome();

        // Act
        ClickButton(cut, "Find Opponent");

        // Assert: a user of the same company in the same chat finds this match and joins it
        ChessMatch match = matches.FindOrCreate(ChatKey, $"{Tenant}:ben");
        Assert.That(match.Snapshot.Status, Is.EqualTo(MatchStatus.Running));
    }

    [Test]
    public void FindOpponent_DoesNotMixUpTwoCompaniesWithTheSameChatId()
    {
        // Arrange
        RunInsideTeams();
        var cut = RenderHome();
        ClickButton(cut, "Find Opponent");

        // Act: a user of ANOTHER company, whose chat happens to have the same id
        ChessMatch other = matches.FindOrCreate("tenant-2:" + Chat, "tenant-2:mallory");

        // Assert
        Assert.That(other.Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
    }

    [Test]
    public void FindOpponent_WhenTheOpponentJoins_ShowsTheBoard()
    {
        // Arrange
        RunInsideTeams();
        var cut = RenderHome();
        ClickButton(cut, "Find Opponent");

        // Act: another person of this chat opens the game
        matches.FindOrCreate(ChatKey, $"{Tenant}:ben");

        // Assert
        cut.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll(".chess-board .square"), Has.Count.EqualTo(64));
                Assert.That(cut.Find(".status").TextContent, Is.EqualTo("You play White - your turn"));
            }
        });
    }

    [Test]
    public void FindOpponent_WhenTwoOtherPeopleAlreadyPlay_ExplainsWhy()
    {
        // Arrange
        matches.FindOrCreate(ChatKey, $"{Tenant}:ben");
        matches.FindOrCreate(ChatKey, $"{Tenant}:carl");
        RunInsideTeams();
        var cut = RenderHome();

        // Act
        ClickButton(cut, "Find Opponent");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Message(cut), Is.EqualTo("Two other people are already playing in this chat."));
            Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find Opponent"));
        }
    }

    [Test]
    public void Cancel_GoesBackToTheMenuAndRemovesTheWaitingMatch()
    {
        // Arrange
        RunInsideTeams();
        var cut = RenderHome();
        ClickButton(cut, "Find Opponent");

        // Act
        ClickButton(cut, "Cancel");

        // Assert: the menu is back, and the next person starts a fresh match instead of joining a ghost
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find Opponent"));
            Assert.That(matches.FindOrCreate(ChatKey, $"{Tenant}:ben").Snapshot.Status, Is.EqualTo(MatchStatus.Waiting));
        }
    }

    #endregion

    #region Playing

    private IRenderedComponent<Home> StartAMatchAsWhite()
    {
        RunInsideTeams();
        var cut = RenderHome();
        ClickButton(cut, "Find Opponent");
        matches.FindOrCreate(ChatKey, $"{Tenant}:ben");
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".chess-board"), Has.Count.EqualTo(1)));
        return cut;
    }

    [Test]
    public void Resign_ShowsAConfirmationFirst()
    {
        // Arrange
        var cut = StartAMatchAsWhite();

        // Act
        ClickButton(cut, "Resign");

        // Assert: nothing happened yet, the user only sees the question
        var buttons = cut.FindAll(".action-row .menu-button").Select(b => b.TextContent).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buttons, Is.EquivalentTo(["Yes, resign", "No"]));
            Assert.That(cut.FindAll(".overlay"), Is.Empty);
        }
    }

    [Test]
    public void Resign_AnsweringNo_KeepsThePlaying()
    {
        // Arrange
        var cut = StartAMatchAsWhite();
        ClickButton(cut, "Resign");

        // Act
        ClickButton(cut, "No");

        // Assert
        var buttons = cut.FindAll(".action-row .menu-button").Select(b => b.TextContent).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buttons, Is.EquivalentTo(["Resign"]));
            Assert.That(cut.FindAll(".overlay"), Is.Empty);
        }
    }

    [Test]
    public void Resign_AnsweringYes_EndsTheGameAndTheOpponentWins()
    {
        // Arrange
        var cut = StartAMatchAsWhite();
        ClickButton(cut, "Resign");

        // Act
        ClickButton(cut, "Yes, resign");

        // Assert: white resigned, so black wins
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find(".overlay .menu-title").TextContent, Is.EqualTo("BLACK WINS!"));
            Assert.That(cut.Find(".overlay .menu-reason").TextContent, Is.EqualTo("WHITE RESIGNED"));
            Assert.That(cut.FindAll(".action-row"), Is.Empty);
        }
    }

    [Test]
    public void Resign_WhenTheOpponentResigns_ShowsTheVictory()
    {
        // Arrange
        var cut = StartAMatchAsWhite();

        // Act: the other player gives up in his own browser
        matches.FindOrCreate(ChatKey, $"{Tenant}:ben").Resign($"{Tenant}:ben");

        // Assert
        cut.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.Find(".overlay .menu-title").TextContent, Is.EqualTo("WHITE WINS!"));
                Assert.That(cut.Find(".overlay .menu-reason").TextContent, Is.EqualTo("BLACK RESIGNED"));
            }
        });
    }

    [Test]
    public void NewGame_AfterTheEnd_StartsWaitingForANewOpponent()
    {
        // Arrange
        var cut = StartAMatchAsWhite();
        ClickButton(cut, "Resign");
        ClickButton(cut, "Yes, resign");

        // Act
        ClickButton(cut, "NEW GAME");

        // Assert
        Assert.That(cut.Find(".menu-title").TextContent, Is.EqualTo("Waiting..."));
    }

    [Test]
    public void Back_AfterTheEnd_GoesToTheMenu()
    {
        // Arrange
        var cut = StartAMatchAsWhite();
        ClickButton(cut, "Resign");
        ClickButton(cut, "Yes, resign");

        // Act
        ClickButton(cut, "BACK");

        // Assert
        Assert.That(cut.Find("button.menu-button").TextContent, Is.EqualTo("Find Opponent"));
    }

    #endregion
}