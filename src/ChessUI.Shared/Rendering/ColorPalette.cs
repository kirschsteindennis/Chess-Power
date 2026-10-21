using ChessLogic.Enums;

namespace ChessUI.Shared.Rendering;

/// <summary>
/// The Color Palette defining the Colors of the player
/// </summary>
public static class ColorPalette
{
    public static string GetMainColor(Player player) => player == Player.White ? "#FFFFFF" : "#000000";
    public static string GetRimColor(Player player) => player == Player.White ? "#000000" : "#FFFFFF";
}
