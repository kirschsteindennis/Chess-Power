using ChessUI.Teams.Startup.Enums;

namespace ChessUI.Teams.Startup;

/// <summary>What is wrong, or what was found. Never contains the value of a secret.</summary>
/// <param name="Name">The Name of the Startup Check</param>
/// <param name="Status">The Status of the Startup Check. See <see cref="StartupCheckStatus"/></param>
/// <param name="Detail">The Details for the Startup Check.</param>
public sealed record StartupCheck(string Name, StartupCheckStatus Status, string Detail);

/// <summary>
/// Checks the Settings, that decide whether the server works at all. A wrong setting is found at startup, not when the first
/// user fails to sign in
/// </summary>
public static class StartupChecks
{
    public static IReadOnlyList<StartupCheck> Run(IConfiguration configuration, IHostEnvironment environment)
    {
        string domain = configuration["Teams:Domain"];

        return
        [
            CheckClientId(configuration["Teams:ClientId"]),
            CheckDomain(domain, environment),
            CheckPseudonymKey(configuration["Privacy:PseudonymKey"]),
            CheckEnvironment(domain, environment)
        ];
    }

    private static StartupCheck CheckClientId(string clientId)
    {
        const string name = "Teams:ClientId";

        if (string.IsNullOrWhiteSpace(clientId))
            return new(name, StartupCheckStatus.Error, "Missing. Nobody can sign in");

        if (!Guid.TryParse(clientId, out _))
            return new(name, StartupCheckStatus.Error,
                "Is not a GUID. Copy the application (client) id from the Entry app registration");

        return new(name, StartupCheckStatus.Ok, "Set.");
    }

    private static StartupCheck CheckDomain(string domain, IHostEnvironment environment)
    {
        const string name = "Teams:Domain";

        if (string.IsNullOrWhiteSpace(domain))
            return new(name, StartupCheckStatus.Error, "Missing. Tokens for this app cannot be accepted");

        if (domain.Contains("://") || domain.Contains('/') || domain.Contains(' '))
            return new(name, StartupCheckStatus.Error,
                "Must be a host name only, without https:// and without a path.");

        if (IsTestAddress(domain) && !environment.IsDevelopment())
            return new(name, StartupCheckStatus.Warning,
                "Looks like a test address (localhost or a development tunnel), but this is not development");

        return new(name, StartupCheckStatus.Ok, "Set.");
    }

    private static StartupCheck CheckPseudonymKey(string key)
    {
        const string name = "Privacy:PseudonymKey";

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            return new(name, StartupCheckStatus.Warning,
                "Missing or shorter than 32 characters. The pseudonyms in the log change with every restart.");

        return new(name, StartupCheckStatus.Ok, "Set.");
    }

    private static StartupCheck CheckEnvironment(string domain, IHostEnvironment environment)
    {
        const string name = "Environment";

        // In dev anyone can pretend to be any user with ?chat=...&user=...
        if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(domain) && !IsTestAddress(domain))
            return new(name, StartupCheckStatus.Warning,
                "The environment is Development, but the domain looks public. " +
                "In Development test identities are accepted without any sign-in.");

        return new(name, StartupCheckStatus.Ok, environment.EnvironmentName);
    }

    private static bool IsTestAddress(string domain)
    {
        return domain.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || domain.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || domain.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase);
    }
}
