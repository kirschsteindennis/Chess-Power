using ChessUI.Teams.ClientErrors;

namespace ChessUI.Teams.Tests.ClientErrors;

[TestFixture]
public class ClientErrorSanitizerTests
{
    [TestCase("error", "error")]
    [TestCase("circuit-failed", "circuit-failed")]
    [TestCase("teams-auth", "teams-auth")]
    [TestCase("ERROR", "other")]
    [TestCase("something-else", "other")]
    [TestCase(null, "other")]
    public void Kind_OnlyKnownKindsPass(string kind, string expected)
    {
        // Arrange: the values come from the TestCase attributes

        // Act
        string result = ClientErrorSanitizer.Kind(kind);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("/config", "/config")]
    [TestCase("/config?user=anna", "/config")]
    [TestCase("/config#section", "/config")]
    [TestCase("https://host.example.com/a/b?x=1", "/a/b")]
    [TestCase("HTTP://host.example.com/a", "/a")]
    [TestCase("", "-")]
    [TestCase(null, "-")]
    public void Path_KeepsOnlyThePath(string address, string expected)
    {
        // Arrange: the values come from the TestCase attributes

        // Act
        string result = ClientErrorSanitizer.Path(address);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Failed to fetch https://app.example.com/api/x?token=abc123", "Failed to fetch https://app.example.com/api/x")]
    [TestCase("Blocked http://a.example/p#secret here", "Blocked http://a.example/p here")]
    [TestCase("No address in this text", "No address in this text")]
    public void Text_DropsTheQueryOfAddressesInsideTheMessage(string text, string expected)
    {
        // Arrange: the values come from the TestCase attributes

        // Act
        string result = ClientErrorSanitizer.Text(text);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Text_ReplacesControlCharactersBySpaces()
    {
        // Arrange
        string text = "a\tb\nc\u0000d";

        // Act
        string result = ClientErrorSanitizer.Text(text);

        // Assert
        Assert.That(result, Is.EqualTo("a b c d"));
    }
}
