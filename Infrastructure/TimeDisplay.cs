using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Renders a stored UTC timestamp as <c>&lt;time datetime="2026-10-22T09:30:00Z"&gt;2026/10/22 09:30 UTC&lt;/time&gt;</c>.
/// wwwroot/js/local-time.js rewrites the text in the VIEWER's own time zone (Intl.DateTimeFormat, page language);
/// without JavaScript the UTC text stays, labelled UTC. The server never needs to know any time zone.
/// </summary>
public static class TimeDisplay
{
    public static string Iso(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static IHtmlContent Utc(DateTime utc)
    {
        var value = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var text = value.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
        var e = HtmlEncoder.Default;
        return new HtmlString($"<time class=\"utc-time\" datetime=\"{e.Encode(Iso(value))}\">{e.Encode(text)}</time>");
    }
}
