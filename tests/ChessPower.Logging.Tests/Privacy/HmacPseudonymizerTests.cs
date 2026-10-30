using ChessPower.Logging.Privacy;

namespace ChessPower.Logging.Tests.Privacy;

[TestFixture]
public class HmacPseudonymizerTests
{
    private const string Key = "0123456789abcdef0123456789abcdef";

    [Test]
    public void Pseudonymize_TheSameValue_GivesTheSameResult()
    {
        // Arrange
        HmacPseudonymizer pseudonymizer = new(Key);
        string sameName = "tenant-1:anna";

        // Act
        string first = pseudonymizer.Pseudonymize(sameName);
        string second = pseudonymizer.Pseudonymize(sameName);

        // Assert
        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Pseudonymize_DifferentValues_GiveDifferentResults()
    {
        // Arrange
        HmacPseudonymizer pseudonymizer = new(Key);

        // Act
        string anna = pseudonymizer.Pseudonymize("tenant-1:anna");
        string ben = pseudonymizer.Pseudonymize("tenant-1:ben");

        // Assert
        Assert.That(anna, Is.Not.EqualTo(ben));
    }

    [Test]
    public void Pseudonymize_WithAnotherKey_GivesAnotherResult()
    {
        // Arrange
        HmacPseudonymizer first = new(Key);
        HmacPseudonymizer second = new("another-secret-key-another-secret-key");

        // Act
        string withFirstKey = first.Pseudonymize("tenant-1:anna");
        string withSecondKey = second.Pseudonymize("tenant-1:anna");

        // Assert
        Assert.That(withFirstKey, Is.Not.EqualTo(withSecondKey));
    }
}
