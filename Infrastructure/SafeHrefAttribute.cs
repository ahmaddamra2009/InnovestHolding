using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Accepts a fragment (<c>#…</c>), a site-relative path (<c>/…</c>), a relative path (<c>page.html</c>,
/// <c>about/team</c>), an absolute http/https URL, or a <c>mailto:</c> / <c>tel:</c> link. Rejects every other
/// scheme (<c>javascript:</c>, <c>data:</c>, <c>vbscript:</c>, …), protocol-relative <c>//host</c>, any
/// backslash (browsers read <c>\</c> as <c>/</c>, so <c>/\host</c> is <c>//host</c>) and control characters.
/// Empty values are left to <c>[Required]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SafeHrefAttribute() : LocalizedValidationAttribute(Localization.MessageKeys.UnsafeHref)
{
    protected override bool IsValueValid(object? value) => IsSafe(value as string);

    public static bool IsSafe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        if (v.Any(char.IsControl) || v.Contains('\\')) return false;
        if (v.StartsWith('#')) return true;
        if (v.StartsWith("//")) return false;

        // A scheme is whatever precedes the first ':' — but only when no '/', '?' or '#' comes before it
        // (then the ':' belongs to a path, query or fragment and the value is relative).
        var colon = v.IndexOf(':');
        var firstDelimiter = v.IndexOfAny(['/', '?', '#']);
        var hasScheme = colon >= 0 && (firstDelimiter < 0 || colon < firstDelimiter);
        if (!hasScheme) return true; // site-relative ("/x") or relative ("page.html")

        var scheme = v[..colon];
        if (scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase) ||
            scheme.Equals("tel", StringComparison.OrdinalIgnoreCase))
            return v.Length > colon + 1;

        return Uri.TryCreate(v, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// True for an absolute http(s) address — a link to another site, which the public pages render with
    /// <c>rel="noopener noreferrer"</c> (SEO-C5 §4). Site paths, fragments, mailto: and tel: are not external.
    /// </summary>
    public static bool IsExternal(string? href)
    {
        var v = href?.Trim();
        return !string.IsNullOrEmpty(v)
               && (v.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || v.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The rel of a link: <c>noopener noreferrer</c> for an external address, else null (Razor omits it).</summary>
    public static string? RelFor(string? href) => IsExternal(href) ? "noopener noreferrer" : null;

}
