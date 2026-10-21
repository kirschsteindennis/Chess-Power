using ChessLogic.Enums;

namespace ChessUI.Core;

public static class ColorPalette
{
    public static string GetMainColor(Player player) => player == Player.White ? "#FFFFFF" : "#000000";
    public static string GetRimColor(Player player) => player == Player.White ? "#000000" : "#FFFFFF";
}
