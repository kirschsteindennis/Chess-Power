using ChessLogic.Services;
using System.Security.Cryptography;
using System.Text;

namespace ChessPower.Logging.Privacy;

/// <summary>
/// Replaces identifiers (user, company, chat) by a short, keyed hash before they reach a log.
/// The same input gives the same output, so log entries can still be connected with each other.
/// Without the secret key nobody can turn the value back into the original or check a guess.
/// </summary>
public sealed class HmacPseudonymizer : IPseudonymizer
{
    private const int MinimumKeyLength = 32;

    private readonly byte[] key;

    public HmacPseudonymizer(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < MinimumKeyLength)
            throw new ArgumentException($"The Key needs at least {MinimumKeyLength} characters.", nameof(secretKey));

        key = Encoding.UTF8.GetBytes(secretKey);
    }

    /// <summary>
    /// A pseudonymizer with a random key that only lives as long as the program runs.
    /// The pseudonyms change with every restart, but nothing can be traced back.
    /// </summary>
    /// <returns>Returns a Pseudonymizer with a random key</returns>
    public static HmacPseudonymizer CreateEphemeral()
    {
        return new HmacPseudonymizer(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
    }

    /// <returns>16 hexadecimal characters, or "-" if there is nothing to hide</returns>
    public string Pseudonymize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "-";

        byte[] hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));

        // 8 bytes are enough to tell people apart in a log, and too short to be useful for anything else
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
