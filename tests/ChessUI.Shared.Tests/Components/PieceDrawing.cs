using AngleSharp.Dom;
using System.Text.RegularExpressions;

namespace ChessUI.Shared.Tests.Components;

/// <summary>
/// Helps to recognise which chess piece a rendererd element shows.
/// The Browser (and bUnit) re-writes SVG markup when it parses it, so we compare the
/// path data and fill colors and never the raw text
/// </summary>
internal static class PieceDrawing
{
    /// <summary>The "d" attributes of all paths in the markup that the app generates</summary>
    /// <param name="svgMarkup">The Markup of the SVG</param>
    public static string[] PathDataOf(string svgMarkup)
    {
        return [.. Regex.Matches(svgMarkup, "\\sd=\"([^\"]+)\"").Select(match => match.Groups[1].Value)];
    }

    /// <summary>The "d" attributes of all paths of a rendered element</summary>
    /// <param name="element">The Element to search</param>
    /// <returns>All "d" Attributes of all paths of the rendered element</returns>
    public static string[] PathDataOf(IElement element)
    {
        return [.. element.QuerySelectorAll("path").Select(path => path.GetAttribute("d"))];
    }

    /// <summary>The fill colors of all paths of a rendered element (rim first, then main color)</summary>
    /// <param name="element">The Element to parse</param>
    /// <returns>First Rim Color, then main color</returns>
    public static string[] FillsOf(IElement element)
    {
        return [.. element.QuerySelectorAll("path").Select(path => path.GetAttribute("fill"))];
    }
}
