using ChessPower.Logging;
using ChessUI.Teams.ClientErrors;
using ChessUI.Teams.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Text;

namespace ChessUI.Teams.Tests.ClientErrors;

[TestFixture]
public class ClientErrorEndpointTests
{
    private CollectingSink sink;
    private SerilogLoggerFactory loggerFactory;
    private Microsoft.Extensions.Logging.ILogger logger;

    #region Helper Methods

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
        logger = loggerFactory.CreateLogger<ClientErrorLog>();
    }

    [TearDown]
    public void TearDown() => loggerFactory.Dispose();

    private static DefaultHttpContext Request(string body, string contentType = "application/json", bool knownLength = true)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        DefaultHttpContext context = new();
        context.Request.ContentType = contentType;
        context.Request.Body = new MemoryStream(bytes);

        if (knownLength)
            context.Request.ContentLength = bytes.Length;

        return context;
    }

    private async Task<int> Send(DefaultHttpContext context)
    {
        return await ClientErrorEndpoint.ProcessAsync(context.Request, logger);
    }

    private static string Report(string kind = "error", string message = "boom", string source = "/_framework/blazor.web.js",
        int line = 12, int column = 5, string page = "/")
    {
        return $$"""{"kind":"{{kind}}","message":"{{message}}","source":"{{source}}","line":{{line}},"column":{{column}},"page":"{{page}}"}""";
    }

    private static string Property(LogEvent logEvent, string name) => ((ScalarValue)logEvent.Properties[name]).Value.ToString();

    private LogEvent TheLoggedEntry() => sink.Events.Single();

    #endregion

    #region Accepting a Report

    [Test]
    public async Task AValidReport_IsAnsweredWithNoContentAndLoggedAsAWarning()
    {
        // Arrange
        DefaultHttpContext context = Request(Report(kind: "unhandledrejection", message: "Failed to fetch", page: "/"));

        // Act
        int status = await Send(context);

        // Assert
        LogEvent entry = TheLoggedEntry();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(204));
            Assert.That(entry.Level, Is.EqualTo(LogEventLevel.Warning));
            Assert.That(Property(entry, "Kind"), Is.EqualTo("unhandledrejection"));
            Assert.That(Property(entry, "Message"), Is.EqualTo("Failed to fetch"));
            Assert.That(Property(entry, "Page"), Is.EqualTo("/"));
        }
    }

    [Test]
    public async Task AReportWithoutAnyField_IsAcceptedAndShownWithDashes()
    {
        // Arrange
        DefaultHttpContext context = Request("{}");

        // Act
        int status = await Send(context);

        // Assert
        LogEvent entry = TheLoggedEntry();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(204));
            Assert.That(Property(entry, "Message"), Is.EqualTo("-"));
            Assert.That(Property(entry, "Kind"), Is.EqualTo("other"));
        }
    }

    [Test]
    public async Task AReportWithAnUnknownKind_IsLoggedAsOther()
    {
        // Arrange: the kinds are a fixed list, the browser cannot invent new ones
        DefaultHttpContext context = Request(Report(kind: "<script>alert(1)</script>"));

        // Act
        await Send(context);

        // Assert
        Assert.That(Property(TheLoggedEntry(), "Kind"), Is.EqualTo("other"));
    }

    #endregion

    #region Rejecting what does not belong here

    [TestCase("text/plain")]
    [TestCase("application/x-www-form-urlencoded")]
    [TestCase("")]
    public async Task ARequestThatIsNotJson_IsRejectedAndNothingIsLogged(string contentType)
    {
        // Arrange: a form on another website can only send such content types
        DefaultHttpContext context = Request(Report(), contentType);

        // Act
        int status = await Send(context);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(415));
            Assert.That(sink.Events, Is.Empty);
        }
    }

    [Test]
    public async Task ABodyThatIsTooLarge_IsRejectedWhenTheLengthIsKnown()
    {
        // Arrange
        DefaultHttpContext context = Request(Report(message: new string('x', ClientErrorEndpoint.MaxBodyBytes)));

        // Act
        int status = await Send(context);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(413));
            Assert.That(sink.Events, Is.Empty);
        }
    }

    [Test]
    public async Task ABodyThatIsTooLarge_IsRejectedEvenWhenTheLengthIsNotAnnounced()
    {
        // Arrange: a chunked upload does not say how long it is
        DefaultHttpContext context = Request(Report(message: new string('x', ClientErrorEndpoint.MaxBodyBytes)), knownLength: false);

        // Act
        int status = await Send(context);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(413));
            Assert.That(sink.Events, Is.Empty);
        }
    }

    [TestCase("this is not json")]
    [TestCase("{\"kind\": ")]
    [TestCase("null")]
    [TestCase("")]
    public async Task BrokenJson_IsAnsweredWithBadRequest(string body)
    {
        // Arrange
        DefaultHttpContext context = Request(body);

        // Act
        int status = await Send(context);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(status, Is.EqualTo(400));
            Assert.That(sink.Events, Is.Empty);
        }
    }

    #endregion

    #region Cleaning what is accepted

    [Test]
    public async Task ALineBreakInTheMessage_CannotForgeALogLine()
    {
        // Arrange: a message that tries to look like a second, separate log entry
        DefaultHttpContext context = Request("""{"kind":"error","message":"boom\n2026-10-30 12:00:00 [ERR] Admin signed in\r\nmore"}""");

        // Act
        await Send(context);

        // Assert
        string message = Property(TheLoggedEntry(), "Message");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Does.Not.Contain("\n"));
            Assert.That(message, Does.Not.Contain("\r"));
            Assert.That(TheLoggedEntry().RenderMessage(), Does.Not.Contain("\n"));
        }
    }

    [Test]
    public async Task ALongMessage_IsShortened()
    {
        // Arrange
        DefaultHttpContext context = Request(Report(message: new string('x', 600)));

        // Act
        await Send(context);

        // Assert
        string message = Property(TheLoggedEntry(), "Message");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Has.Length.EqualTo(ClientErrorSanitizer.MaxMessageLength + 3));
            Assert.That(message, Does.Contain("..."));
        }
    }

    [Test]
    public async Task AddressesLoseTheirQueryTheirFragmentAndTheirHost()
    {
        // Arrange: a query can hold a token or a user id
        DefaultHttpContext context = Request(Report(
            source: "https://app.example.com/_framework/blazor.web.js?token=abc123#top",
            page: "/config?user=anna&chat=19:secret"));

        // Act
        await Send(context);

        // Assert
        LogEvent entry = TheLoggedEntry();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Property(entry, "Source"), Is.EqualTo("/_framework/blazor.web.js"));
            Assert.That(Property(entry, "Page"), Is.EqualTo("/config"));
            Assert.That(entry.RenderMessage(), Does.Not.Contain("anna"));
            Assert.That(entry.RenderMessage(), Does.Not.Contain("abc123"));
        }
    }

    [Test]
    public async Task ATokenAndAnEmailAddressInTheMessage_AreMasked()
    {
        // Arrange
        DefaultHttpContext context = Request(Report(
            message: "Failed for anna@example.com with Bearer abc123.def456 and eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln"));

        // Act
        await Send(context);

        // Assert
        string message = Property(TheLoggedEntry(), "Message");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Does.Not.Contain("anna@example.com"));
            Assert.That(message, Does.Not.Contain("abc123"));
            Assert.That(message, Does.Not.Contain("eyJ"));
        }
    }

    [Test]
    public async Task NumbersOutsideOfARealRange_AreNotLogged()
    {
        // Arrange
        DefaultHttpContext context = Request(Report(line: -5, column: 2000000000));

        // Act
        await Send(context);

        // Assert
        LogEvent entry = TheLoggedEntry();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Properties["Line"].ToString(), Is.EqualTo("null"));
            Assert.That(entry.Properties["Column"].ToString(), Is.EqualTo("null"));
        }
    }

    #endregion
}
