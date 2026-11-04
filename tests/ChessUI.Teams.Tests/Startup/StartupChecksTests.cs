using ChessUI.Teams.Startup;
using ChessUI.Teams.Startup.Enums;
using ChessUI.Teams.Tests.Support;
using Microsoft.Extensions.Configuration;

namespace ChessUI.Teams.Tests.Startup;

[TestFixture]
public class StartupChecksTests
{
    private const string GoodClientId = "11111111-1111-1111-1111-111111111111";
    private const string GoodKey = "a-long-secret-key-for-the-tests-0123456789";

    #region Setup

    // A complete and correct production configuration. Tests change one thing at a time.
    private static Dictionary<string, string> GoodSettings() => new()
    {
        ["Teams:ClientId"] = GoodClientId,
        ["Teams:Domain"] = "chess.example.com",
        ["Privacy:PseudonymKey"] = GoodKey
    };

    private static IReadOnlyList<StartupCheck> Run(Dictionary<string, string> settings, string environment = "Production")
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return StartupChecks.Run(configuration, new FakeHostEnvironment(environment));
    }

    private static StartupCheck Check(IReadOnlyList<StartupCheck> checks, string name) => checks.Single(c => c.Name == name);

    #endregion

    #region Everything fine

    [Test]
    public void Run_ACompleteConfiguration_IsOkEverywhere()
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(checks, Has.Count.EqualTo(4));
        Assert.That(checks.All(c => c.Status == StartupCheckStatus.Ok), Is.True);
    }

    #endregion

    #region Teams:ClientId

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Run_WithoutAClientId_IsAnError(string clientId)
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();
        settings["Teams:ClientId"] = clientId;

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(Check(checks, "Teams:ClientId").Status, Is.EqualTo(StartupCheckStatus.Error));
    }

    [Test]
    public void Run_AClientIdThatIsNoGuid_IsAnError()
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();
        settings["Teams:ClientId"] = "my-app";

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(Check(checks, "Teams:ClientId").Status, Is.EqualTo(StartupCheckStatus.Error));
    }

    #endregion
}
