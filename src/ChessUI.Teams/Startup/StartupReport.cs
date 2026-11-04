using ChessUI.Teams.Startup.Enums;
using System.Runtime.InteropServices;

namespace ChessUI.Teams.Startup;

public static class StartupReport
{
    /// <summary>
    /// Writes the start of the program and the result of the checks into the log
    /// </summary>
    /// <param name="logger">The Logger to use for results</param>
    /// <param name="checks">The Checks to run</param>
    /// <param name="environment">The Environment of the Application</param>
    /// <returns>True if at least one check fails with an Error. False if everything is correct.</returns>
    public static bool Write(ILogger logger, IReadOnlyList<StartupCheck> checks, IHostEnvironment environment)
    {
        logger.LogInformation(
            "Chess Power {Version} started in Environment {Environment} ({Runtime} on {Os})",
            AppInfo.Version, environment.EnvironmentName, RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription);

        foreach (StartupCheck check in checks)
        {
            LogLevel level = check.Status switch
            {
                StartupCheckStatus.Error => LogLevel.Error,
                StartupCheckStatus.Warning => LogLevel.Warning,
                _ => LogLevel.Debug
            };

            logger.Log(level, "Startup check {Check}: {Status}. {Detail}", check.Name, check.Status.ToString(), check.Detail);
        }

        int errors = checks.Count(check => check.Status == StartupCheckStatus.Error);
        int warnings = checks.Count(check => check.Status == StartupCheckStatus.Warning);

        logger.LogInformation(
            "Startup checks finished: {Ok} ok, {Warnings} warnings, {Errors} errors",
            checks.Count - errors - warnings, warnings, errors);

        return errors > 0;
    }
}
