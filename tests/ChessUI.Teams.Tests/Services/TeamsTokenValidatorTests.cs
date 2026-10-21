using ChessUI.Teams.Services;
using ChessUI.Teams.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;

namespace ChessUI.Teams.Tests.Services;

[TestFixture]
public class TeamsTokenValidatorTests
{
    private Mock<IConfigurationManager<OpenIdConnectConfiguration>> configManager;
    private TeamsTokenValidator validator;

    #region Helper Functions

    [SetUp]
    public void SetUp()
    {
        // By default the identity provider publishes exactly one key: the trusted one
        configManager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        configManager.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestTokens.ConfigWith(TestTokens.TrustedKey));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Teams:ClientId"] = TestTokens.ClientId,
                ["Teams:Domain"] = TestTokens.Domain
            })
            .Build();

        validator = new TeamsTokenValidator(
            configManager.Object, configuration, NullLogger<TeamsTokenValidator>.Instance);
    }

    #endregion

    #region Valid Tokens

    [Test]
    public async Task ValidateAsync_AValidToken_ReturnsTenantAndUser()
    {
        // Arrange
        string token = TestTokens.Create();

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(identity.TenantId, Is.EqualTo(TestTokens.TenantId));
            Assert.That(identity.UserId, Is.EqualTo(TestTokens.UserId));
        }
    }

    [TestCase("api://chess.example.com/11111111-1111-1111-1111-111111111111")]
    [TestCase("11111111-1111-1111-1111-111111111111")]
    public async Task ValidateAsync_BothAudienceFormsOfOurApp_AreAccepted(string audience)
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions { Audience = audience });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Not.Null);
    }

    [Test]
    public async Task ValidateAsync_ATokenFromAnotherCompany_ReturnsThatCompanysTenant()
    {
        // Arrange: the service is multi-tenant, so every company's tenant id must be accepted
        const string otherTenant = "99999999-9999-9999-9999-999999999999";
        string token = TestTokens.Create(new TokenOptions
        {
            TenantId = otherTenant,
            Issuer = $"https://login.microsoftonline.com/{otherTenant}/v2.0"
        });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Not.Null);
        Assert.That(identity.TenantId, Is.EqualTo(otherTenant));
    }

    [Test]
    public async Task ValidateAsync_ScopeAmongOthers_IsAccepted()
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions { Scope = "profile access_as_user email" });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Not.Null);
    }

    [Test]
    public async Task ValidateAsync_AValidToken_DoesNotRefreshTheKeys()
    {
        // Arrange
        string token = TestTokens.Create();

        // Act
        await validator.ValidateAsync(token);

        // Assert
        configManager.Verify(m => m.RequestRefresh(), Times.Never());
    }

    #endregion

    #region Missing or broken Tokens

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task ValidateAsync_WithoutAToken_ReturnsNullWithoutAskingForKeys(string token)
    {
        // Arrange: the token comes from the TestCase attributes

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
        configManager.Verify(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestCase("not-a-token")]
    [TestCase("a.b.c")]
    [TestCase("eyJhbGciOiJSUzI1NiJ9.e30.")]
    public async Task ValidateAsync_WithGarbage_ReturnsNull(string token)
    {
        // Arrange: the token comes from the TestCase attributes

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    #endregion

    #region Signature

    [Test]
    public async Task ValidateAsync_ASignatureFromTheWrongKey_ReturnsNull()
    {
        // Arrange: the attacker uses the id of the real key, but has another private key
        string token = TestTokens.Create(signingKey: TestTokens.ForgedKey);

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_ATokenWithoutSignature_ReturnsNull()
    {
        // Arrange: "alg: none" tokens claim that no signature is needed
        string token = TestTokens.CreateUnsigned();

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_ATokenSignedWithASharedSecret_ReturnsNull()
    {
        // Arrange: an "algorithm confusion" attack: HS256 instead of RS256
        string token = TestTokens.CreateWithSharedSecret();

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_ATamperedPayload_ReturnsNull()
    {
        // Arrange: take a valid token and swap in the payload of another user
        string valid = TestTokens.Create();
        string foreign = TestTokens.Create(new TokenOptions { UserId = "00000000-0000-0000-0000-000000000000" });
        string[] validParts = valid.Split('.');
        string[] foreignParts = foreign.Split('.');
        string tampered = $"{validParts[0]}.{foreignParts[1]}.{validParts[2]}";

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(tampered);

        // Assert
        Assert.That(identity, Is.Null);
    }

    #endregion

    #region Lifetime

    [Test]
    public async Task ValidateAsync_AnExpiredToken_ReturnsNull()
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions
        {
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = DateTime.UtcNow.AddHours(-1)
        });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_ATokenThatIsNotValidYet_ReturnsNull()
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions
        {
            NotBefore = DateTime.UtcNow.AddHours(1),
            Expires = DateTime.UtcNow.AddHours(2)
        });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    #endregion

    #region Audience, Issuer, Scope and Claims

    [TestCase("api://other.example.com/11111111-1111-1111-1111-111111111111")]
    [TestCase("api://chess.example.com/99999999-9999-9999-9999-999999999999")]
    [TestCase("https://graph.microsoft.com")]
    public async Task ValidateAsync_ATokenForAnotherApp_ReturnsNull(string audience)
    {
        // Arrange: a token that Microsoft issued for a different API must not open our door
        string token = TestTokens.Create(new TokenOptions { Audience = audience });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_WhenTheIssuerBelongsToAnotherTenantThanTheClaim_ReturnsNull()
    {
        // Arrange: tid says company A, but the issuer says company B
        string token = TestTokens.Create(new TokenOptions
        {
            Issuer = "https://login.microsoftonline.com/99999999-9999-9999-9999-999999999999/v2.0"
        });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [TestCase("https://evil.example.com/22222222-2222-2222-2222-222222222222/v2.0")]
    [TestCase("https://sts.windows.net/22222222-2222-2222-2222-222222222222/")]
    [TestCase("https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222/v2.0/")]
    public async Task ValidateAsync_AnUnexpectedIssuer_ReturnsNull(string issuer)
    {
        // Arrange: only the exact v2.0 issuer of the token's own tenant is accepted
        string token = TestTokens.Create(new TokenOptions { Issuer = issuer });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_WithoutATenantClaim_ReturnsNull()
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions { TenantId = null });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_WithoutAUserClaim_ReturnsNull()
    {
        // Arrange
        string token = TestTokens.Create(new TokenOptions { UserId = null });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("User.Read")]
    [TestCase("access_as_user_not")]
    public async Task ValidateAsync_WithoutTheRequiredScope_ReturnsNull(string scope)
    {
        // Arrange: the scope comes from the TestCase attributes
        string token = TestTokens.Create(new TokenOptions { Scope = scope });

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    #endregion

    #region Signing keys of the identity provider

    [Test]
    public async Task ValidateAsync_WhenTheKeysCannotBeLoaded_ReturnsNullInsteadOfThrowing()
    {
        // Arrange: the identity provider is not reachable
        configManager.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        string token = TestTokens.Create();

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_AfterAKeyRotation_RefreshesTheKeysOnceAndAcceptsTheToken()
    {
        // Arrange: the token is signed with a new key that our cached key list does not contain yet
        int loads = 0;
        configManager.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++loads == 1
                ? TestTokens.ConfigWith(TestTokens.TrustedKey)
                : TestTokens.ConfigWith(TestTokens.TrustedKey, TestTokens.UnknownKey));
        string token = TestTokens.Create(signingKey: TestTokens.UnknownKey);

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert
        Assert.That(identity, Is.Not.Null);
        configManager.Verify(m => m.RequestRefresh(), Times.Once());
        Assert.That(loads, Is.EqualTo(2));
    }

    [Test]
    public async Task ValidateAsync_WithAKeyThatNeverAppears_RefreshesOnlyOnceAndReturnsNull()
    {
        // Arrange: even after the refresh the key is unknown, so the token is forged
        string token = TestTokens.Create(signingKey: TestTokens.UnknownKey);

        // Act
        TeamsIdentity identity = await validator.ValidateAsync(token);

        // Assert: one retry, not a loop
        Assert.That(identity, Is.Null);
        configManager.Verify(m => m.RequestRefresh(), Times.Once());
        configManager.Verify(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task ValidateAsync_AForgedSignatureOfAKnownKey_DoesNotTriggerARefresh()
    {
        // Arrange: the key id is known, so a refresh would not help and must not be abused
        string token = TestTokens.Create(signingKey: TestTokens.ForgedKey);

        // Act
        await validator.ValidateAsync(token);

        // Assert
        configManager.Verify(m => m.RequestRefresh(), Times.Never());
    }

    #endregion
}
