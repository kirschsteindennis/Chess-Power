using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace ChessUI.Teams.Tests.Support;

/// <summary>What a test token contains. The defaults describe a perfectly valid Teams token.</summary>
internal sealed record TokenOptions
{
    public string Issuer { get; init; } = $"https://login.microsoftonline.com/{TestTokens.TenantId}/v2.0";
    public string Audience { get; init; } = $"api://{TestTokens.Domain}/{TestTokens.ClientId}";

    // null means: this claim is missing in the token
    public string TenantId { get; init; } = TestTokens.TenantId;
    public string UserId { get; init; } = TestTokens.UserId;
    public string Scope { get; init; } = "access_as_user";

    public DateTime NotBefore { get; init; } = DateTime.UtcNow.AddMinutes(-1);
    public DateTime Expires { get; init; } = DateTime.UtcNow.AddHours(1);
}

/// <summary>Creates signed test tokens that look like the ones Teams delivers.</summary>
internal static class TestTokens
{
    public const string ClientId = "11111111-1111-1111-1111-111111111111";
    public const string Domain = "chess.example.com";
    public const string TenantId = "22222222-2222-2222-2222-222222222222";
    public const string UserId = "33333333-3333-3333-3333-333333333333";

    // Creating RSA keys is slow, so all tests share these three key pairs
    public static readonly RsaSecurityKey TrustedKey = NewKey("trusted-key");

    // Same key id as the trusted key, but a different secret: a forged signature
    public static readonly RsaSecurityKey ForgedKey = NewKey("trusted-key");

    // A key that the identity provider never published
    public static readonly RsaSecurityKey UnknownKey = NewKey("unknown-key");

    private static RsaSecurityKey NewKey(string keyId)
    {
        return new RsaSecurityKey(RSA.Create(2048)) { KeyId = keyId };
    }

    /// <summary>The key list that the identity provider publishes: public parts only.</summary>
    public static OpenIdConnectConfiguration ConfigWith(params RsaSecurityKey[] keys)
    {
        OpenIdConnectConfiguration config = new();

        foreach (RsaSecurityKey key in keys)
            config.SigningKeys.Add(new RsaSecurityKey(key.Rsa.ExportParameters(false)) { KeyId = key.KeyId });

        return config;
    }

    /// <summary>A token signed with RS256, like the ones from Microsoft.</summary>
    public static string Create(TokenOptions options = null, RsaSecurityKey signingKey = null)
    {
        SigningCredentials credentials = new(signingKey ?? TrustedKey, SecurityAlgorithms.RsaSha256);
        return Build(options ?? new TokenOptions(), credentials);
    }

    /// <summary>A token without any signature (algorithm "none").</summary>
    public static string CreateUnsigned(TokenOptions options = null)
    {
        return Build(options ?? new TokenOptions(), credentials: null);
    }

    /// <summary>A token signed with a shared secret (HS256), as used in "algorithm confusion" attacks.</summary>
    public static string CreateWithSharedSecret(TokenOptions options = null)
    {
        SymmetricSecurityKey secret = new(Encoding.UTF8.GetBytes(new string('x', 64)));
        SigningCredentials credentials = new(secret, SecurityAlgorithms.HmacSha256);
        return Build(options ?? new TokenOptions(), credentials);
    }

    private static string Build(TokenOptions options, SigningCredentials credentials)
    {
        Dictionary<string, object> claims = [];

        if (options.TenantId is not null) claims["tid"] = options.TenantId;
        if (options.UserId is not null) claims["oid"] = options.UserId;
        if (options.Scope is not null) claims["scp"] = options.Scope;

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            NotBefore = options.NotBefore,
            Expires = options.Expires,
            Claims = claims,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
