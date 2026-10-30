using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace ChessPower.Logging.Privacy;

/// <summary>
/// The safety net of the privacy rules. It cleans every log entry just before it is written.
/// Properties with a sensitive name are hidden, and tokens and e-mail addresses inside text are masked.
/// It does NOT replace careful logging: never write personal data into the message text on purpose.
/// </summary>
public sealed class PrivacyEnricher : ILogEventEnricher
{
    public const string Redacted = "[REDACTED]";

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Token", "AccessToken", "IdToken", "RefreshToken", "AuthToken", "Authorization", "Cookie",
        "Password", "Secret", "ClientSecret", "ApiKey",
        "Email", "Mail", "UserName", "DisplayName", "Upn", "UserPrincipalName",
        "IpAddress", "RemoteIpAddress", "ClientIp", "Phone", "PhoneNumber"
    };

    // NonBacktracking: the time to match grows linearly with the text, so no input can make it slow
    private const RegexOptions Options = RegexOptions.NonBacktracking | RegexOptions.CultureInvariant;

    private static readonly Regex BearerToken = new(@"Bearer\s+[A-Za-z0-9._~+/\-]+=*", Options | RegexOptions.IgnoreCase);
    private static readonly Regex JsonWebToken = new(@"\beyJ[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]*", Options);
    private static readonly Regex EmailAddress = new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", Options);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties.ToList())
        {
            LogEventPropertyValue cleaned = IsSensitive(property.Key)
                ? new ScalarValue(Redacted)
                : Scrub(property.Value);
        }
    }

    private static bool IsSensitive(string name) => SensitiveNames.Contains(name);

    private static LogEventPropertyValue Scrub(LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue { Value: string text }:
                return new ScalarValue(ScrubText(text));

            case StructureValue structure:
                return new StructureValue(
                    structure.Properties.Select(member => new LogEventProperty(
                        member.Name,
                        IsSensitive(member.Name) ? new ScalarValue(Redacted) : Scrub(member.Value))),
                    structure.TypeTag);

            case SequenceValue sequence:
                return new SequenceValue(sequence.Elements.Select(Scrub));

            case DictionaryValue dictionary:
                return new DictionaryValue(dictionary.Elements.Select(
                    pair => KeyValuePair.Create(pair.Key, Scrub(pair.Value))));

            default:
                return value;
        }
    }

    private static string ScrubText(string text)
    {
        text = BearerToken.Replace(text, $"Bearer {Redacted}");
        text = JsonWebToken.Replace(text, "[JWT]");
        text = EmailAddress.Replace(text, "[EMAIL]");
        return text;
    }
}
