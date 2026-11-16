using System.Reflection;

namespace ChessUI.Teams.Startup;

public static class AppInfo
{
    /// <summary>The Version of the running process. e.g. "1.2.0+3f2a9c1" (Version + Build Commit)</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly;

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
