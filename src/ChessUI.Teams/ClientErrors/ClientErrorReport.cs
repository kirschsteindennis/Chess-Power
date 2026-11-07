namespace ChessUI.Teams.ClientErrors;

/// <summary>What the browser sends when something goes wrong there. Everything in it is untrusted</summary>
/// <param name="Kind"></param>
/// <param name="Message"></param>
/// <param name="Source"></param>
/// <param name="Line"></param>
/// <param name="Column"></param>
/// <param name="Page"></param>
public sealed record ClientErrorReport(string Kind, string Message, string Source, int? Line, int? Column, string Page);
