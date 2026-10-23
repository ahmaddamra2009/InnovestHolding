namespace InnovestHolding.Infrastructure.Seo;

/// <summary>
/// The absolute base URL of the site (canonical, hreflang, og:url, sitemap). Stored in the SiteContents key
/// <c>site_base_url</c> and set by the admin. Never derived from the Host header outside Development, so a forged
/// Host cannot poison canonicals or the sitemap.
/// </summary>
public static class SiteUrl
{
    public const string ContentKey = "site_base_url";

    /// <summary>
    /// Accepts only an absolute https URL with a host and nothing else (no user info, query, fragment, or a path other
    /// than "/"). Normalised to scheme://host[:port] without a trailing slash.
    /// </summary>
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        if (v.Any(char.IsControl) || HasFormatChar(v) || v.Contains('\\') || v.Any(char.IsWhiteSpace)) return false;
        if (!Uri.TryCreate(v, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host)) return false;
        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        if (uri.AbsolutePath != "/") return false;
        // The original text must not carry a query/fragment marker that Uri dropped (e.g. "https://x/?").
        if (v.Contains('?') || v.Contains('#')) return false;
        normalized = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    /// <summary>
    /// The stored value when valid; otherwise the request's scheme+host in Development only; otherwise null — and
    /// then no absolute canonical/hreflang/og:url is rendered and /sitemap.xml answers 404: a wrong absolute URL is worse for search engines than none.
    /// </summary>
    public static string? Resolve(string? stored, IWebHostEnvironment environment, HttpRequest request)
    {
        if (TryNormalize(stored, out var normalized)) return normalized;
        return environment.IsDevelopment() && request.Host.HasValue
            ? $"{request.Scheme}://{request.Host.Value}"
            : null;
    }

    /// <summary>
    /// A Unicode format character (category Cf: bidi overrides such as U+202E, zero-width U+200B/U+200D, the BOM, soft
    /// hyphen…). Invisible or reordering, so a link could read differently from where it goes (Yasser I-C5F-1).
    /// </summary>
    public static bool HasFormatChar(string value) =>
        value.Any(c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format);

    /// <summary>An absolute https URL (or empty, which is allowed by the caller). No javascript:, no //host, no control
    /// or format (Cf) character anywhere.</summary>
    public static bool IsHttpsUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Any(char.IsControl) && !HasFormatChar(value) && !value.Contains('\\')
        && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

    /// <summary>A site-relative path ("/assets/x.png"): starts with one "/", no "//", no backslash, no scheme.</summary>
    public static bool IsSitePath(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith('/') && !value.StartsWith("//") && !value.Contains('\\')
        && !value.Any(char.IsControl) && !value.Any(char.IsWhiteSpace);
}
