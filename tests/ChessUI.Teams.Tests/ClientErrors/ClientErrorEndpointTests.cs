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
    private ILoggerFactory loggerFactory;
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



    #endregion
}
