using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.ClientErrors;
using ChessUI.Teams.Components;
using ChessUI.Teams.Logging;
using ChessUI.Teams.Services;
using ChessUI.Teams.Startup;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;

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

    // Limit for the open endpoint of the browser errors: 20 reports per minute and client are plenty
    // for real errors, and too few to flood the log. The address is only used to count, never written anywhere.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(ClientErrorEndpoint.RateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
    });

    var app = builder.Build();

    // The startup of the program and the check of its settings are the first thing in the log
    bool startupFailed = StartupReport.Write(
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup"),
        StartupChecks.Run(app.Configuration, app.Environment),
        app.Environment);

    // A Server with wrong settings must not look as if it was running: Azure then shows the failed start
    // Switch off with the setting Startup:FailOnErros = false
    if (startupFailed && !app.Environment.IsDevelopment() && app.Configuration.GetValue("Startup:FailOnErrors", true))
        throw new InvalidOperationException("The startup checks found errors in the settings, see the log above.");

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

    app.UseRateLimiter();
    app.UseAntiforgery();

    app.MapStaticAssets();

    // For the health check of Azure, and to see which version is really running after a deployment
    app.MapGet("/healthz", () => Results.Json(new { status = "ok", version = AppInfo.Version }));

    app.MapClientErrors();

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
              || path.StartsWithSegments("/_blazor")
              || path.StartsWithSegments("/healthz")
              || path.StartsWithSegments("/client-errors");

    return noise ? LogEventLevel.Debug : LogEventLevel.Information;
}
