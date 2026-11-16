namespace ChessLogic.Services;

/// <summary>
/// Turns an Identifier of a person (user, company, chat) into a value that may be written to a log
/// The same input always gives the same output, so log entries can still be connected with each other
/// </summary>
public interface IPseudonymizer
{
    string Pseudonymize(string value);
}
