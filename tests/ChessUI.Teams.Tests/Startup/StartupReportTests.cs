using ChessPower.Logging;
using ChessUI.Teams.Startup;
using ChessUI.Teams.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace ChessUI.Teams.Tests.Startup;

[TestFixture]
public class StartupReportTests
{
    private const string Key = "a-long-secret-key-for-the-tests-0123456789";

    private CollectingSink sink;
    private SerilogLoggerFactory loggerFactory;
    private Microsoft.Extensions.Logging.ILogger logger;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        sink = new();

        Serilog.ILogger serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .EnrichWithChessDefaults("Tests", "1.0.0")
            .WriteTo.Sink(sink)
            .CreateLogger();

        loggerFactory = new SerilogLoggerFactory(serilog);
        logger = loggerFactory.CreateLogger("Startup");
    }

    [TearDown]
    public void TearDown() => loggerFactory.Dispose();

    private static IReadOnlyList<StartupCheck> Checks(string clientId, string domain, string key)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Teams:ClientId"] = clientId,
                ["Teams:Domain"] = domain,
                ["Privacy:PseudonymKey"] = key
            })
            .Build();

        return StartupChecks.Run(configuration, new FakeHostEnvironment("Production"));
    }

    private static string Property(LogEvent logEvent, string name) => ((ScalarValue)logEvent.Properties[name]).Value.ToString();

    #endregion

    #region Write Tests

    [Test]
    public void Write_AlwaysStartsWithTheVersionAndTheEnvironment()
    {
        // Arrange
        var checks = Checks("11111111-1111-1111-1111-111111111111", "chess.example.com", Key);

        // Act
        StartupReport.Write(logger, checks, new FakeHostEnvironment("Production"));

        // Assert
        LogEvent started = sink.Events[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(started.Level, Is.EqualTo(LogEventLevel.Information));
            Assert.That(started.MessageTemplate.Text, Does.StartWith("Chess Power {Version} started"));
            Assert.That(Property(started, "Environment"), Is.EqualTo("Production"));
            Assert.That(Property(started, "Version"), Is.EqualTo("18.10.1"));
        }
    }

    [Test]
    public void Write_ACorrectConfiguration_ReportsNoErrorAndOnlyDebugLinesForTheChecks()
    {
        // Arrange
        var checks = Checks("11111111-1111-1111-1111-111111111111", "chess.example.com", Key);

        // Act
        bool hasErrors = StartupReport.Write(logger, checks, new FakeHostEnvironment("Production"));

        // Assert
        List<LogEvent> checkLines = [.. sink.Events.Where(e => e.MessageTemplate.Text.StartsWith("Startup check"))];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasErrors, Is.False);
            Assert.That(checkLines, Has.Count.EqualTo(5));
        }
        Assert.That(checkLines.All(e => e.Level == LogEventLevel.Debug), Is.True);
    }

    #endregion
}
