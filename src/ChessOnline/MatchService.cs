using ChessLogic.Clock;
using ChessLogic.Diagnostics;
using ChessLogic.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace ChessOnline;

/// <summary>
/// Registerd as a singleton: one instance for all users, so everybody sees the same matches
/// </summary>
public sealed class MatchService
{
    private readonly object gate = new();
    private readonly Dictionary<string, ChessMatch> matches = [];
    private readonly IRandomSource random;
    private readonly IPseudonymizer pseudonymizer;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly TimeProvider timeProvider;

    /// <param name="random">A Random Source</param>
    /// <param name="loggerFactory">Used to Create a logger</param>
    /// <param name="pseudonymizer">Makes id's of chats and people safe for the log. Without it, nothing about them is logged.</param>
    /// <param name="timeProvider">The source of the time for the chess clock. Only tests replace it</param>
    public MatchService(IRandomSource random, IPseudonymizer pseudonymizer = null, ILoggerFactory loggerFactory = null, TimeProvider timeProvider = null)
    {
        ILoggerFactory factory = loggerFactory ?? NullLoggerFactory.Instance;
        
        this.random = random;
        this.pseudonymizer = pseudonymizer ?? HiddenPseudonymizer.Instance;
        this.loggerFactory = factory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        logger = factory.CreateLogger<MatchService>();
    }

    /// <summary>
    /// Raised when a match in a chat starts or stops waiting for an opponent (the chatId is the argument)
    /// Screens of that chat can use it to show the waiting match
    /// </summary>
    public event Action<string> WaitingChanged;

    /// <summary>Finds or crates a match</summary>
    /// <param name="chatId">The ID of the Chat</param>
    /// <param name="userId">The ID of the User</param>
    /// <param name="timeControl">The time control for a NEW match, or null for no clock. Whoever joins a waiting match plays with the time control of that match</param>
    /// <returns>The match of this chat, or null if two other people already play in it</returns>
    public ChessMatch FindOrCreate(string chatId, string userId, TimeControl timeControl = null)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.FindOrCreate");

        ChessMatch result;
        bool waitingChanged;

        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch existing) &&
                existing.Snapshot.Status != MatchStatus.Finished)
            {
                activity?.SetTag("chess.match_id", existing.Id);

                bool wasWaiting = existing.Snapshot.Status == MatchStatus.Waiting;

                if (existing.Join(userId))
                {
                    result = existing;
                    waitingChanged = wasWaiting && existing.Snapshot.Status != MatchStatus.Waiting;
                }
                else
                {
                    logger.LogInformation(
                        "Player {PlayerRef} could not join match {MatchId}: the match is full",
                        pseudonymizer.Pseudonymize(userId), existing.Id);

                    result = null;
                    waitingChanged = false;
                }
            }
            else
            {
                // no match yet, or the last one is over: this user opens a new one and waits
                ChessMatch created = new(userId, random, loggerFactory.CreateLogger<ChessMatch>(), timeControl, timeProvider);
                matches[chatId] = created;
                activity?.SetTag("chess.match_id", created.Id);

                logger.LogInformation(
                    "Match {MatchId} created, waiting for an opponent (chat {ChatRef}, player {PlayerRef}",
                    created.Id, pseudonymizer.Pseudonymize(chatId), pseudonymizer.Pseudonymize(userId));

                result = created;
                waitingChanged = true;
            }
        }

        if (waitingChanged)
            WaitingChanged?.Invoke(chatId);

        return result;
    }

    /// <summary>
    /// The match of this chat that waits for somebody else to join, or null if there is none
    /// (or if the waiting match is the one of this user)
    /// </summary>
    /// <param name="chatId">The ID of the Chat</param>
    /// <param name="userId">The ID of the User</param>
    public WaitingMatch FindWaiting(string chatId, string userId)
    {
        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch match) &&
                match.Snapshot.Status == MatchStatus.Waiting &&
                !match.IsWaitingFor(userId))
                return new WaitingMatch(match.TimeControl);

            return null;
        }
    }

    public void CancelWaiting(string chatId, string userId)
    {
        using Activity activity = ChessTelemetry.Source.StartActivity("Match.CancelWaiting");

        bool cancelled = false;

        lock (gate)
        {
            if (matches.TryGetValue(chatId, out ChessMatch match) && match.IsWaitingFor(userId))
            {
                matches.Remove(chatId);
                cancelled = true;
                activity?.SetTag("chess.match_id", match.Id);
                logger.LogInformation("Waiting match {MatchId} cancelled by its creator", match.Id);
            }
        }

        if (cancelled)
            WaitingChanged?.Invoke(chatId);
    }
}
