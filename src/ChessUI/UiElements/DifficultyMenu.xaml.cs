using ChessLogic.Enums;
using System.Windows;
using System.Windows.Controls;

namespace ChessUI.UiElements;

/// <summary>
/// Interaktionslogik für DifficultyMenu.xaml
/// </summary>
public partial class DifficultyMenu : UserControl
{
    public event Action<AIDifficulty> DifficultySelected;
    public event Action BackToMainMenu;

    public DifficultyMenu()
    {
        InitializeComponent();
    }

    private void Easy_Click(object sender, RoutedEventArgs e)
    {
        DifficultySelected?.Invoke(AIDifficulty.Easy);
    }

    private void Medium_Click(object sender, RoutedEventArgs e)
    {
        DifficultySelected?.Invoke(AIDifficulty.Medium);
    }

    private void Hard_Click(object sender, RoutedEventArgs e)
    {
        DifficultySelected?.Invoke(AIDifficulty.Hard);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        BackToMainMenu?.Invoke();
    }
}
