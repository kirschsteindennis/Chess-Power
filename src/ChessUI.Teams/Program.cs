using ChessLogic.Services;
using ChessOnline;
using ChessUI.Teams.Components;
using ChessUI.Teams.Services;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

// Teams shows this app inside an iframe, so Teams must be allowed to embed it.
// If needed the list needs to be updated with the current domains from the Microsoft documentation
const string FrameAncestors =
    "frame-ancestors 'self' https://teams.microsoft.com https://*.teams.microsoft.com " +
    "https://*.office.com https://*.microsoft365.com https://*.cloud.microsoft https://*.skype.com";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One instance for ALL users: this is where all running matches live
builder.Services.AddSingleton<MatchService>();

builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(_ =>
        new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://login.microsoftonline.com/organizations/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()));
builder.Services.AddSingleton<ITeamsTokenValidator, TeamsTokenValidator>();
builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = FrameAncestors;
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);

app.Run();
