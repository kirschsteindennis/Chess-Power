using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;

namespace ChessUI.Teams.Services;

/// <summary>Who is behind a Teams token: company (tenant) and user.</summary>
/// <param name="TenantId">Which company is behind the token</param>
/// <param name="UserId">Which user is behind the token</param>
public sealed record TeamsIdentity(string TenantId, string UserId);

/// <summary>CHecks the token that Teams hands to the page. Exists so that pages can be testes with a mock</summary>
public interface ITeamsTokenValidator
{
    /// <summary>The identity, or null if the token is missing or not valid</summary>
    /// <param name="token">The Token to check</param>
    /// <returns>The identity or null if token is invalid or missing</returns>
    Task<TeamsIdentity> ValidateAsync(string token);
}

public sealed class TeamsTokenValidator : ITeamsTokenValidator
{
    // The scope that we defined in the Entry app registration
    private const string RequiredScope = "access_as_user";

    private readonly IConfigurationManager<OpenIdConnectConfiguration> configManager;
    //private readonly ConfigurationManager<OpenIdConnectConfiguration> configManager =
        //new(MetadataUrl, new OpenIdConnectConfigurationRetriever());

    private readonly JsonWebTokenHandler handler = new();
    private readonly string[] validAudiences;
    private readonly ILogger<TeamsTokenValidator> logger;

    public TeamsTokenValidator(
        IConfigurationManager<OpenIdConnectConfiguration> configManager,
        IConfiguration configuration,
        ILogger<TeamsTokenValidator> logger)
    {
        this.configManager = configManager;
        this.logger = logger;

        string clientId = configuration["Teams:ClientId"];
        string domain = configuration["Teams:Domain"];

        // Depends on token version. Audience is either the clientId or the api:// uri
        validAudiences = [clientId, $"api://{domain}/{clientId}"];
    }

    /// <summary>Validates the Identity</summary>
    /// <param name="token">The Token that needs to be validated</param>
    /// <returns>The identity or null, if the token is missing or invalid</returns>
    public async Task<TeamsIdentity> ValidateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            TokenValidationResult result = await ValidateTokenAsync(token);

            if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
            {
                configManager.RequestRefresh();
                result = await ValidateTokenAsync(token);
            }

            if (!result.IsValid)
            {
                logger.LogWarning(result.Exception, "Teams token is not valid");
                return null;
            }

            if (!HasRequiredScope(result))
            {
                logger.LogWarning("Teams token does not contain the scope {Scope}", RequiredScope);
                return null;
            }

            string tenantId = result.ClaimsIdentity.FindFirst("tid")?.Value;
            string userId = result.ClaimsIdentity.FindFirst("oid")?.Value;

            if (tenantId is null || userId is null)
                return null;

            return new TeamsIdentity(tenantId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Teams token validation failed");
            return null;
        }
    }

    private async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        OpenIdConnectConfiguration config = await configManager.GetConfigurationAsync(CancellationToken.None);

        TokenValidationParameters parameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ValidateAudience = true,
            ValidAudiences = validAudiences,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            IssuerValidator = ValidateIssuer
        };

        return await handler.ValidateTokenAsync(token, parameters);
    }

    private static bool HasRequiredScope(TokenValidationResult result)
    {
        string scopes = result.ClaimsIdentity.FindFirst("scp")?.Value;

        return scopes is not null &&
            scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(RequiredScope);
    }

    /// <summary>
    /// Every company has its own issuer. It is valid if it matches the tenant id INSIDE the token.
    /// Not a fixed list of companies: the audience check above makes sure the token is meant for our app
    /// </summary>
    /// <param name="issuer">The Issuer we expect</param>
    /// <param name="token">The Token we got</param>
    /// <param name="parameters">The Parameters for Token Validation</param>
    /// <returns></returns>
    private static string ValidateIssuer(string issuer, SecurityToken token, TokenValidationParameters parameters)
    {
        if (token is JsonWebToken jwt &&
            jwt.TryGetPayloadValue<string>("tid", out string tenantId) &&
            issuer == $"https://login.microsoftonline.com/{tenantId}/v2.0")
            return issuer;

        throw new SecurityTokenInvalidIssuerException("Unexpected Token Issuer");
    }
}
