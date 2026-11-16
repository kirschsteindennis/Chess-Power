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

    #region Teams:Domain

    [Test]
    public void Run_WithoutADomain_IsAnError()
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();
        settings.Remove("Teams:Domain");

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(Check(checks, "Teams:Domain").Status, Is.EqualTo(StartupCheckStatus.Error));
    }

    [TestCase("https://chess.example.com")]
    [TestCase("chess.example.com/app")]
    [TestCase("chess example")]
    public void Run_ADomainThatIsNotJustAHostName_IsAnError(string domain)
    {
        // Arrange: the domain is used to build "api://{domain}/{clientId}", so a scheme or a path breaks it
        Dictionary<string, string> settings = GoodSettings();
        settings["Teams:Domain"] = domain;

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(Check(checks, "Teams:Domain").Status, Is.EqualTo(StartupCheckStatus.Error));
    }

    [TestCase("localhost")]
    [TestCase("127.0.0.1")]
    [TestCase("abc123-7193.euw.devtunnels.ms")]
    public void Run_ATestAddressInProduction_IsAWarning(string domain)
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();
        settings["Teams:Domain"] = domain;

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings, "Production");

        // Assert
        Assert.That(Check(checks, "Teams:Domain").Status, Is.EqualTo(StartupCheckStatus.Warning));
    }

    [Test]
    public void Run_ATestAddressInDevelopment_IsFine()
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();
        settings["Teams:Domain"] = "abc123-7193.euw.devtunnels.ms";

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings, "Development");

        // Assert
        Assert.That(checks.All(c => c.Status == StartupCheckStatus.Ok), Is.True);
    }

    #endregion

    #region Pseudonym Key

    [TestCase(null)]
    [TestCase("")]
    [TestCase("too-short")]
    public void Run_WithoutAGoodPseudonymKey_IsAWarning(string key)
    {
        // Arrange: not an error, because the program then uses a random key and stays safe
        Dictionary<string, string> settings = GoodSettings();
        settings["Privacy:PseudonymKey"] = key;

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        Assert.That(Check(checks, "Privacy:PseudonymKey").Status, Is.EqualTo(StartupCheckStatus.Warning));
    }

    #endregion

    #region Environment

    [Test]
    public void Run_DevelopmentWithAPublicDomain_IsAWarning()
    {
        // Arrange: in Development anybody can pretend to be any user, so this must never run on a public server
        Dictionary<string, string> settings = GoodSettings();

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings, "Development");

        // Assert
        Assert.That(Check(checks, "Environment").Status, Is.EqualTo(StartupCheckStatus.Warning));
    }

    [Test]
    public void Run_Production_ShowsTheEnvironmentName()
    {
        // Arrange
        Dictionary<string, string> settings = GoodSettings();

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings, "Production");

        // Assert
        Assert.That(Check(checks, "Environment").Detail, Is.EqualTo("Production"));
    }

    #endregion

    #region Secrets

    [Test]
    public void Run_NeverRepeatsTheValuesOfTheSettings()
    {
        // Arrange: a check may say that a secret is missing or set, but never what it is
        Dictionary<string, string> settings = GoodSettings();

        // Act
        IReadOnlyList<StartupCheck> checks = Run(settings);

        // Assert
        string everything = string.Join("\n", checks.Select(c => c.Detail));
        Assert.That(everything, Does.Not.Contain(GoodKey));
        Assert.That(everything, Does.Not.Contain(GoodClientId));
    }

    #endregion
}
