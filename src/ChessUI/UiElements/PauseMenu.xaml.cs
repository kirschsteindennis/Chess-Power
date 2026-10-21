using ChessUI.Enum;
using System.Windows.Controls;

namespace ChessUI.UiElements;

/// <summary>
/// Interaktionslogik für PauseMenu.xaml
/// </summary>
public partial class PauseMenu : UserControl
{
    public event Action<Option> OptionSelected;
    public PauseMenu()
    {
        InitializeComponent();
    }

    private void Continue_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OptionSelected?.Invoke(Option.Continue);
    }

    private void Restart_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OptionSelected?.Invoke(Option.Restart);
    }
}
