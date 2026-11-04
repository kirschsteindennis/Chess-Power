using ChessLogic.Diagnostics;
using ChessPower.Logging.Tracing;
using ChessUI.Wpf.Logging;
using Serilog;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace ChessUI.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IDisposable activityListener;

    protected override void OnStartup(StartupEventArgs e)
    {
        // The Logging must be ready BEFORE the main window is created (base.OnStartup creates it)
        Log.Logger = DesktopLogging.Create();
        activityListener = ChessActivityListener.Start(ChessTelemetry.SourceName);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // The folder is written with the variable, not with its value: the real path contains the Windows user name
        Log.Information(
            "Chess Power {Version} started ({Runtime} on {Os}). Log files: %LocalAppData%\\ChessPower\\logs",
            DesktopLogging.Version, RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription);

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Chess Power closed (exit code {ExitCode})", e.ApplicationExitCode);
        activityListener?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    // Not marked as handled: the program ends as before, but now the error is in the log
    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled error in the user interface");
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.ExceptionObject as Exception, "Unhandled error (terminating: {IsTerminating})", e.IsTerminating);
        Log.CloseAndFlush();
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "A background task failed and nobody noticed");
        e.SetObserved();
    }
}
