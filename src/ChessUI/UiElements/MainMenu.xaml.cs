using ChessUI.Enum;
using System.Windows;
using System.Windows.Controls;

namespace ChessUI.UiElements;

/// <summary>
/// Interaktionslogik für MainMenu.xaml
/// </summary>
public partial class MainMenu : UserControl
{
    public event Action<GameMode> OptionSelected;
    public MainMenu()
    {
        InitializeComponent();
    }

    private void LocalGame_Click(object sender, RoutedEventArgs e)
    {
        OptionSelected?.Invoke(GameMode.Local);
    }

    private void ComputerGame_Click(object sender, RoutedEventArgs e)
    {
        OptionSelected?.Invoke(GameMode.VsComputer);
    }

    private void Multiplayer_Click(object sender, RoutedEventArgs e)
    {
        OptionSelected?.Invoke(GameMode.Online);
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
