using System.Text.RegularExpressions;

namespace ChessUI.Teams.ClientErrors;

/// <summary>
/// The browser can send anything, so nothing of it goes into the log unchecked:
/// The Kinds are a fixed list, texts are single-line and short, addresses lose their query
/// </summary>
public static class ClientErrorSanitizer
{
    public const int MaxMessageLength = 500;
    public const int MaxPathLength = 200;

    private static readonly HashSet<string> Kinds = new(StringComparer.Ordinal)
    {
        "error", "unhandledrejection", "circuit-failed", "circuit-rejected", "teams-sdk", "teams-auth", "manual"
    };

    public static string Kind(string kind)
    {
        return kind is not null && Kinds.Contains(kind) ? kind : "other";
    }

    // Error messages of browsers often contain whole addresses ("Failed to fetch https://...?token=...")
    private static readonly Regex AddressWithQuery = new(@"(https?://[^\s?#]+)[?#]\S*",
        RegexOptions.NonBacktracking | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// One line without control characters (so nobody can forge log lines), addresses without their query,
    /// shortened
    /// </summary>
    /// <param name="text">The Text of the Log Message</param>
    /// <param name="maxLength">The maximal Length of the Log Message</param>
    /// <returns>Returns a sanitized, one line Log Message</returns>
    public static string Text(string text, int maxLength = MaxMessageLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "-";

        text = AddressWithQuery.Replace(text, "$1");
        string cleaned = new(text.Select(c => char.IsControl(c) ? ' ' : c).ToArray().Trim());

        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength] + "...";
    }

    /// <summary>
    /// Only the Path of the Address. The query (it could hold tokens) and the fragment are dropped,
    /// and so is the host name
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static string Path(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "-";

        string path = address.Trim();
        bool isWebAddress = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isWebAddress && Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            path = uri.AbsolutePath;
        else
        {
            int cut = path.IndexOfAny(['?', '#']);

            if (cut >= 0)
                path = path[..cut];
        }

        return Text(path, MaxPathLength);
    }

    public static int? Number(int? value)
    {
        return value is >= 0 and <= 1_000_000 ? value : null;
    }
}
