using ChessLogic.Diagnostics;
using ChessLogic.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace ChessOnline;

/// <summary>
/// Registerd as a singleton: one instance for all users, so everybody sees the same matches
/// </summary>
/// <param name="random">A Random Source</param>
/// <param name="loggerFactory">Used to Create a logger</param>
/// <param name="pseudonymizer">Makes id's of chats and people safe for the log. Without it, nothing about them is logged.</param>
public sealed class MatchService(IRandomSource random, IPseudonymizer pseudonymizer = null, ILoggerFactory loggerFactory = null)
{
    private readonly object gate = new();
    private readonly Dictionary<string, ChessMatch> matches = [];
    private readonly IRandomSource random = random;
    private readonly IPseudonymizer pseudonymizer = pseudonymizer ?? HiddenPseudonymizer.Instance;
    private readonly ILoggerFactory loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    private readonly ILogger logger = loggerFactory.CreateLogger<MatchService>();

    /// <returns>The match of this chat, or null if two other people already play in it</returns>
    public ChessMatch FindOrCreate(string chatId, string userId)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.FindOrCreate");

        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch existing) &&
                existing.Snapshot.Status != MatchStatus.Finished)
            {
                activity?.SetTag("chess.match_id", existing.Id);

                if (existing.Join(userId))
                    return existing;

                logger.LogInformation(
                    "Player {PlayerRef} could not join match {MatchId}: the match is full",
                    pseudonymizer.Pseudonymize(userId), existing.Id);
                return null;
            }

            // no match yet, or the last one is over: this user opens a new one and waits
            ChessMatch created = new(userId, random, loggerFactory.CreateLogger<ChessMatch>());
            matches[chatId] = created;
            activity?.SetTag("chess.match_id", created.Id);

            logger.LogInformation(
                "Match {MatchId} created, waiting for an opponent (chat {ChatRef}, player {PlayerRef}",
                created.Id, pseudonymizer.Pseudonymize(chatId), pseudonymizer.Pseudonymize(userId));
            return created;
        }
    }

    public void CancelWaiting(string chatId, string userId)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.CancelWaiting");

        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch match) && match.IsWaitingFor(userId))
            {
                matches.Remove(chatId);
                activity?.SetTag("chess.match_id", match.Id);
                logger.LogInformation("Waiting match {MatchId} cancelled by its creator", match.Id);
            }
        }
    }
}
