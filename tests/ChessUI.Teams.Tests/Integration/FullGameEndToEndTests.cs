using AngleSharp.Dom;
using Bunit;
using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.Components.Pages;
using ChessUI.Teams.Services;
using ChessUI.Teams.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace ChessUI.Teams.Tests.Integration;

/// <summary>
/// The whole chain, nothing in between is replaces: the Teams JavaScript part (faked), the real sign-in
/// check with real signed tokens, the pages, the board, the sessions, the match and the match service.
/// Every user has a browser of his own (a BunitContext); all browsers talk to the same server objects.
/// </summary>
[TestFixture]
public class FullGameEndToEndTests
{
    private const string Chat = "chat-1";

    private MatchService matches;
    private TeamsTokenValidator validator;
    private readonly List<BunitContext> browsers = [];

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        // The person who clicks "Find Opponent" first plays white
        Mock<IRandomSource> random = new();
        random.Setup(r => r.Next(2)).Returns(0);
        matches = new MatchService(random.Object);

        // The identity provider publishes exactly one key: the trusted one
        Mock<IConfigurationManager<OpenIdConnectConfiguration>> configManager = new();
        configManager.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestTokens.ConfigWith(TestTokens.TrustedKey));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Teams:ClientId"] = TestTokens.ClientId,
                ["Teams:Domain"] = TestTokens.Domain
            })
            .Build();

        validator = new TeamsTokenValidator(configManager.Object, configuration, NullLogger<TeamsTokenValidator>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (BunitContext browser in browsers)
            browser.Dispose();
    }

    // A user opens the tab in his own Teams window. Teams delivers a token that is signed with the given key.
    private IRenderedComponent<Home> OpenTab(
        string userId,
        string tenantId = TestTokens.TenantId,
        RsaSecurityKey signingKey = null,
        string audience = null)
    {
        BunitContext browser = new();
        browsers.Add(browser);

        Mock<IWebHostEnvironment> environment = new();
        environment.Setup(e => e.EnvironmentName).Returns("Production");

        browser.Services.AddSingleton(matches);
        browser.Services.AddSingleton<ITeamsTokenValidator>(validator);
        browser.Services.AddSingleton(environment.Object);

        TokenOptions options = new()
        {
            UserId = userId,
            TenantId = tenantId,
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0"
        };
        if (audience is not null)
            options = options with { Audience = audience };

        browser.JSInterop.Setup<TeamsContext>("chessTeams.getContext").SetResult(new TeamsContext(Chat, null));
        browser.JSInterop.Setup<string>("chessTeams.getAuthToken").SetResult(TestTokens.Create(options, signingKey));

        IRenderedComponent<Home> tab = browser.Render<Home>();
        tab.WaitForAssertion(() => Assert.That(tab.Markup, Does.Not.Contain("Connecting to Teams")));
        return tab;
    }

    private static void ClickButton(IRenderedComponent<Home> tab, string text)
    {
        tab.FindAll("button.menu-button").Single(button => button.TextContent == text).Click();
    }

    // Row by row from the top: white sees rank 8 first, black sees rank 1 first
    private static IElement Square(IRenderedComponent<Home> tab, string square, bool blackPerspective)
    {
        int row = 8 - (square[1] - '0');
        int column = square[0] - 'a';
        int index = blackPerspective ? (7 - row) * 8 + (7 - column) : row * 8 + column;

        return tab.FindAll(".chess-board .square")[index];
    }

    private static void Move(IRenderedComponent<Home> tab, string from, string to, bool blackPerspective = false)
    {
        Square(tab, from, blackPerspective).Click();
        Square(tab, to, blackPerspective).Click();
    }

    private static bool HasPieceOn(IRenderedComponent<Home> tab, string square, bool blackPerspective)
    {
        return Square(tab, square, blackPerspective).QuerySelectorAll(".piece").Count == 1;
    }

    private static string Status(IRenderedComponent<Home> tab) => tab.Find(".status").TextContent;

    private static string Title(IRenderedComponent<Home> tab) => tab.Find(".menu-title").TextContent;

    // Anna (white) and Ben (black) are playing: Anna clicked first, Ben joined
    private (IRenderedComponent<Home> Anna, IRenderedComponent<Home> Ben) StartTheGame()
    {
        IRenderedComponent<Home> anna = OpenTab("anna");
        IRenderedComponent<Home> ben = OpenTab("ben");

        ClickButton(anna, "Find Opponent");
        ClickButton(ben, "Find Opponent");

        anna.WaitForAssertion(() => Assert.That(anna.FindAll(".chess-board").Count, Is.EqualTo(1)));
        return (anna, ben);
    }

    #endregion

    #region Meeting each other

    [Test]
    public void TwoColleaguesInOneChat_MeetAndGetOppositeColors()
    {
        // Arrange
        IRenderedComponent<Home> anna = OpenTab("anna");
        IRenderedComponent<Home> ben = OpenTab("ben");

        // Act
        ClickButton(anna, "Find Opponent");
        string annaWhileWaiting = Title(anna);
        ClickButton(ben, "Find Opponent");

        // Assert
        anna.WaitForAssertion(() => Assert.That(Status(anna), Is.EqualTo("You play White - your turn")));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(annaWhileWaiting, Is.EqualTo("Waiting..."));
            Assert.That(Status(ben), Is.EqualTo("You play Black - waiting for opponent"));
        }
    }

    [Test]
    public void TheBoardOfBlack_IsTurnedAround()
    {
        // Arrange
        var (anna, ben) = StartTheGame();

        // Act
        bool annaSeesAWhiteRookOnA1 = HasPieceOn(anna, "a1", blackPerspective: false);
        bool benSeesTheSameFromTheOtherSide = HasPieceOn(ben, "a1", blackPerspective: true);

        // Assert: the first square on the screen is a8 for white and h1 for black
        using (Assert.EnterMultipleScope())
        {
            Assert.That(annaSeesAWhiteRookOnA1, Is.True);
            Assert.That(benSeesTheSameFromTheOtherSide, Is.True);
            Assert.That(anna.FindAll(".chess-board .square")[0].ClassList.Contains("light"), Is.True);
        }
    }

    #endregion

    #region Playing

    [Test]
    public void AMoveOfAnna_AppearsOnBensBoardWithoutAnyActionOfBen()
    {
        // Arrange
        var (anna, ben) = StartTheGame();

        // Act
        Move(anna, "e2", "e4");

        // Assert
        ben.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(HasPieceOn(ben, "e4", blackPerspective: true), Is.True);
                Assert.That(HasPieceOn(ben, "e2", blackPerspective: true), Is.False);
                Assert.That(Status(ben), Is.EqualTo("You play Black - your turn"));
            }
        });
        Assert.That(Status(anna), Is.EqualTo("You play White - waiting for opponent"));
    }

    [Test]
    public void BenCannotMoveWhileItIsAnnasTurn()
    {
        // Arrange
        var (_, ben) = StartTheGame();

        // Act
        Move(ben, "e7", "e5", blackPerspective: true);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(HasPieceOn(ben, "e7", blackPerspective: true), Is.True);
            Assert.That(HasPieceOn(ben, "e5", blackPerspective: true), Is.False);
        }
    }

    [Test]
    public void AFullGame_EndsWithCheckmateOnBothScreens()
    {
        // Arrange
        var (anna, ben) = StartTheGame();

        // Act: the fastest checkmate there is
        Move(anna, "f2", "f3");
        ben.WaitForAssertion(() => Assert.That(Status(ben), Is.EqualTo("You play Black - your turn")));
        Move(ben, "e7", "e5", blackPerspective: true);
        anna.WaitForAssertion(() => Assert.That(Status(anna), Is.EqualTo("You play White - your turn")));
        Move(anna, "g2", "g4");
        ben.WaitForAssertion(() => Assert.That(Status(ben), Is.EqualTo("You play Black - your turn")));
        Move(ben, "d8", "h4", blackPerspective: true);

        // Assert
        anna.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(anna.Find(".overlay .menu-title").TextContent, Is.EqualTo("BLACK WINS!"));
                Assert.That(anna.Find(".overlay .menu-reason").TextContent, Is.EqualTo("CHECKMATE - WHITE CAN'T MOVE"));
            }
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ben.Find(".overlay .menu-title").TextContent, Is.EqualTo("BLACK WINS!"));
            Assert.That(Status(ben), Is.EqualTo("Game Over"));
            Assert.That(Status(anna), Is.EqualTo("Game Over"));
        }
    }

    [Test]
    public void ARematch_BringsBothBackToAFreshBoard()
    {
        // Arrange: a finished game
        var (anna, ben) = StartTheGame();
        ClickButton(ben, "Resign");
        ClickButton(ben, "Yes, resign");
        anna.WaitForAssertion(() => Assert.That(anna.FindAll(".overlay").Count, Is.EqualTo(1)));

        // Act
        ClickButton(anna, "NEW GAME");
        string annaWhileWaiting = Title(anna);
        ClickButton(ben, "NEW GAME");

        // Assert
        anna.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(anna.FindAll(".overlay"), Is.Empty);
                Assert.That(anna.FindAll(".chess-board .piece"), Has.Count.EqualTo(32));
            }
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(annaWhileWaiting, Is.EqualTo("Waiting..."));
            Assert.That(ben.FindAll(".overlay"), Is.Empty);
            Assert.That(ben.FindAll(".chess-board .piece"), Has.Count.EqualTo(32));
        }
    }

    [Test]
    public void WhenBenResigns_AnnaSeesTheVictory()
    {
        // Arrange
        var (anna, ben) = StartTheGame();

        // Act
        ClickButton(ben, "Resign");
        ClickButton(ben, "Yes, resign");

        // Assert
        anna.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(anna.Find(".overlay .menu-title").TextContent, Is.EqualTo("WHITE WINS!"));
                Assert.That(anna.Find(".overlay .menu-reason").TextContent, Is.EqualTo("BLACK RESIGNED"));
            }
        });
        Assert.That(ben.Find(".overlay .menu-title").TextContent, Is.EqualTo("WHITE WINS!"));
    }

    [Test]
    public void AnnaReloadingHerTab_ReturnsToTheRunningGame()
    {
        // Arrange
        var (anna, _) = StartTheGame();
        Move(anna, "e2", "e4");

        // Act: Anna opens the tab again, the old screen is gone
        IRenderedComponent<Home> reloaded = OpenTab("anna");
        ClickButton(reloaded, "Find Opponent");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(HasPieceOn(reloaded, "e4", blackPerspective: false), Is.True);
            Assert.That(Status(reloaded), Is.EqualTo("You play White - waiting for opponent"));
        }
    }

    [Test]
    public void AThirdColleagueInTheChat_IsToldThatTheGameIsFull()
    {
        // Arrange
        StartTheGame();
        IRenderedComponent<Home> carl = OpenTab("carl");

        // Act
        ClickButton(carl, "Find Opponent");

        // Assert
        Assert.That(carl.Find(".menu-reason").TextContent, Is.EqualTo("Two other people are already playing in this chat."));
    }

    #endregion

    #region Companies

    [Test]
    public void TwoCompaniesWithTheSameChatId_NeverPlayAgainstEachOther()
    {
        // Arrange
        IRenderedComponent<Home> annaOfCompanyA = OpenTab("anna", tenantId: "company-a");
        IRenderedComponent<Home> eveOfCompanyB = OpenTab("eve", tenantId: "company-b");

        // Act
        ClickButton(annaOfCompanyA, "Find Opponent");
        ClickButton(eveOfCompanyB, "Find Opponent");

        // Assert: both wait, because nobody of their own company is there
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Title(annaOfCompanyA), Is.EqualTo("Waiting..."));
            Assert.That(Title(eveOfCompanyB), Is.EqualTo("Waiting..."));
        }
    }

    [Test]
    public void ColleaguesOfTheSameCompany_MeetEvenWhenAnotherCompanyUsesTheSameChatId()
    {
        // Arrange
        IRenderedComponent<Home> annaOfCompanyA = OpenTab("anna", tenantId: "company-a");
        IRenderedComponent<Home> eveOfCompanyB = OpenTab("eve", tenantId: "company-b");
        IRenderedComponent<Home> benOfCompanyA = OpenTab("ben", tenantId: "company-a");
        ClickButton(annaOfCompanyA, "Find Opponent");
        ClickButton(eveOfCompanyB, "Find Opponent");

        // Act
        ClickButton(benOfCompanyA, "Find Opponent");

        // Assert
        annaOfCompanyA.WaitForAssertion(() => Assert.That(annaOfCompanyA.FindAll(".chess-board"), Has.Count.EqualTo(1)));
        Assert.That(Title(eveOfCompanyB), Is.EqualTo("Waiting..."));
    }

    #endregion

    #region Attacks

    [Test]
    public void ATokenWithAForgedSignature_IsRejectedAndOpensNoGame()
    {
        // Arrange
        IRenderedComponent<Home> mallory = OpenTab("mallory", signingKey: TestTokens.ForgedKey);

        // Act
        string message = mallory.Find(".menu-reason").TextContent;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Is.EqualTo("Sign-in with Teams failed."));
            Assert.That(mallory.FindAll("button.menu-button"), Is.Empty);
        }
    }

    [Test]
    public void ATokenForAnotherApp_IsRejected()
    {
        // Arrange
        IRenderedComponent<Home> mallory = OpenTab("mallory", audience: "api://other.example.com/99999999-9999-9999-9999-999999999999");

        // Act
        string message = mallory.Find(".menu-reason").TextContent;

        // Assert
        Assert.That(message, Is.EqualTo("Sign-in with Teams failed."));
    }

    [Test]
    public void AForgedToken_CannotTakeTheSeatOfARealPlayer()
    {
        // Arrange: Anna waits in the chat, Mallory claims to be Ben but cannot sign like Microsoft
        IRenderedComponent<Home> anna = OpenTab("anna");
        ClickButton(anna, "Find Opponent");
        IRenderedComponent<Home> mallory = OpenTab("ben", signingKey: TestTokens.ForgedKey);

        // Act
        string malloryMessage = mallory.Find(".menu-reason").TextContent;

        // Assert: Anna is still waiting, nobody joined her
        using (Assert.EnterMultipleScope())
        {
            Assert.That(malloryMessage, Is.EqualTo("Sign-in with Teams failed."));
            Assert.That(Title(anna), Is.EqualTo("Waiting..."));
        }
    }

    #endregion
}
