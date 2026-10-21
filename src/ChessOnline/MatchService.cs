using ChessLogic.Services;

namespace ChessOnline;

/// <summary>
/// Registerd as a singleton: one instance for all users, so everybody sees the same matches
/// </summary>
public sealed class MatchService(IRandomSource random)
{
    private readonly object gate = new();
    private readonly Dictionary<string, ChessMatch> matches = [];

    /// <returns>The match of this chat, or null if two other people already play in it</returns>
    public ChessMatch FindOrCreate(string chatId, string userId)
    {
        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch existing) &&
                existing.Snapshot.Status != MatchStatus.Finished)
                return existing.Join(userId) ? existing : null;

            // No match yet, or the last one is over: this user opens a new one and waits
            ChessMatch created = new(userId, random);
            matches[chatId] = created;
            return created;
        }
    }

    public void CancelWaiting(string chatId, string userId)
    {
        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch match) && match.IsWaitingFor(userId))
                matches.Remove(chatId);
        }
    }
}
