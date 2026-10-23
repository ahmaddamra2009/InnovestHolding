using System.Text.Encodings.Web;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.AspNetCore.Html;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// Turns admin text into markup. Every part is HTML-encoded BEFORE markup is added around it, so no admin value can
/// inject markup (the only trusted-markup paths of the page are the seeder-owned IconSvg columns and the JSON-LD).
/// </summary>
public static class ContentText
{
    /// <summary>Same policy as the app's Razor encoder: letters of every script as is, HTML-sensitive characters encoded.</summary>
    public static readonly HtmlEncoder Encoder = HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

    /// <summary>
    /// "prefix {highlight} suffix" → the three parts. No braces → the whole text is the prefix. Anything else
    /// (two pairs, a lone brace, a reversed pair) is malformed and refused by the admin validation.
    /// </summary>
    public static bool TryParseHighlight(string? value, out string prefix, out string highlight, out string suffix)
    {
        value ??= "";
        prefix = value; highlight = ""; suffix = "";
        var open = value.IndexOf('{');
        var close = value.IndexOf('}');
        if (open < 0 && close < 0) return true;
        if (open < 0 || close < open
            || value.IndexOf('{', open + 1) >= 0 || value.IndexOf('}', close + 1) >= 0) return false;
        prefix = value[..open];
        highlight = value[(open + 1)..close];
        suffix = value[(close + 1)..];
        return true;
    }

    /// <summary>The highlighted title markup: prefix + gold span + suffix (a malformed value renders as plain text).</summary>
    public static IHtmlContent Highlight(LText text)
    {
        var e = Encoder;
        if (!TryParseHighlight(text.Value, out var prefix, out var highlight, out var suffix))
            return new HtmlString(e.Encode(text.Value));
        if (highlight.Length == 0)
            return new HtmlString(e.Encode(prefix + suffix));
        return new HtmlString(e.Encode(prefix) + "<span class=\"gold gold-underline\">" + e.Encode(highlight) + "</span>" + e.Encode(suffix));
    }

    /// <summary>Each line encoded, joined with &lt;br&gt;.</summary>
    public static IHtmlContent Lines(LText text) =>
        new HtmlString(string.Join("<br>", text.Value.Replace("\r\n", "\n").Split('\n').Select(Encoder.Encode)));

    /// <summary>
    /// A list written as one line ("الابتكار، الشراكة، …" or "Innovation, partnership, …") → its trimmed items, split on
    /// the Arabic comma «،» and the Latin comma. Fewer than two items → empty: the text is not a list and is shown as
    /// written. Items stay plain text (encoded by Razor where they are rendered).
    /// </summary>
    public static IReadOnlyList<string> ListItems(string? text)
    {
        var items = (text ?? "").Split(['،', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return items.Length >= 2 ? items : [];
    }

    /// <summary>"{year}" → the current UTC year (invariant digits).</summary>
    public static string WithYear(string value, DateTime utcNow) =>
        value.Replace("{year}", utcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
