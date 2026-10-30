using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.Components;
using ChessUI.Teams.Logging;
using ChessUI.Teams.Services;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Serilog.Events;

// Teams shows this app inside an iframe, so Teams must be allowed to embed it.
// If needed the list needs to be updated with the current domains from the Microsoft documentation
const string FrameAncestors =
    "frame-ancestors 'self' https://teams.microsoft.com https://*.teams.microsoft.com " +
    "https://*.office.com https://*.microsoft365.com https://*.cloud.microsoft https://*.skype.com";

// Creating a simple logger to catch errors in startup process
Log.Logger = ChessLoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddChessLogging();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();

    builder.Services.AddSingleton<MatchService>();

    // The signing keys of Microsoft's identity platform. The manager caches them and refreshes them from time to time.
    builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(_ =>
        new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://login.microsoftonline.com/organizations/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()));

    builder.Services.AddSingleton<ITeamsTokenValidator, TeamsTokenValidator>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    // One log entry per request instead of many small ones from the framework
    app.UseSerilogRequestLogging(options => options.GetLevel = RequestLevel);

    app.Use(async (context, next) =>
    {
        context.Response.Headers.ContentSecurityPolicy = FrameAncestors;
        await next();
    });

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        // null = Blazor sets no own frame-ancestors policy, ours above applies instead
        .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "The application stopped unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Files of the framework and the connection of the page are not interesting
static LogEventLevel RequestLevel(HttpContext context, double elapsed, Exception exception)
{
    if (exception is not null || context.Response.StatusCode >= 500)
        return LogEventLevel.Error;

    PathString path = context.Request.Path;
    bool noise = path.StartsWithSegments("/_framework")
              || path.StartsWithSegments("/_content")
              || path.StartsWithSegments("/_blazor");

    return noise ? LogEventLevel.Debug : LogEventLevel.Information;
}
