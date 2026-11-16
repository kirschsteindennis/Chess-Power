using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;

namespace ChessUI.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        ServiceCollection services = new();
        services.AddWpfBlazorWebView();
        #if DEBUG
            services.AddBlazorWebViewDeveloperTools(); // Use F12 to open dev tools
#endif

        // The Blazor part writes into the same log files as the rest of the program
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(Log.Logger, dispose: false);
        });

        // needs to be before initialize Component to add dynamic resource to xaml
        Resources.Add("services", services.BuildServiceProvider());

        InitializeComponent();
    }
}