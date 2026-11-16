using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ChessUI.Teams.Tests.Support;

/// <summary>A hosting environment for tests: only the name matters</summary>
/// <param name="environmentName">The name of the environment</param>
internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "Tests";
    public string ContentRootPath { get; set; } = "/";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
