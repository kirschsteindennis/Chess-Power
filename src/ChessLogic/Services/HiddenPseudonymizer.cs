namespace ChessLogic.Services;

/// <summary>
/// The default for code that was created without a pseudonymizer:
/// nothing about the person reaches the log.
/// </summary>
public sealed class HiddenPseudonymizer : IPseudonymizer
{
    public static readonly HiddenPseudonymizer Instance = new();

    public string Pseudonymize(string value) => "hidden";
}
