using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// 301s the aliases of the public pages to their canonical URL, in ONE hop (GET/HEAD only — a 301 on a POST would
/// break the forms). Targets are paths of the route table; only the request's query string is appended (no open
/// redirect). Anything that is not an alias of a known page is left alone (and answers 404 if nothing serves it).
/// <code>
/// /Home, /Home/Index, /en/Index (any case, trailing slash) → / or /en
/// /About, /about/, /EN/About/                            → /about, /en/about
/// /ar, /ar/…                                             → the same path without /ar
/// </code>
/// </summary>
public static class CultureUrlNormalizer
{
    private static readonly string[] ToArabicHome = ["/home", "/home/index"];
    private static readonly string[] ToEnglishHome = ["/en/index"];

    /// <summary>The canonical target for <paramref name="path"/>, or null when the path is canonical or unknown.</summary>
    public static string? Target(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var candidate = path.ToLowerInvariant();
        if (candidate.Length > 1) candidate = candidate.TrimEnd('/');
        if (candidate.Length == 0) candidate = "/";

        // A leading /ar segment is the Arabic page itself.
        if (candidate == "/ar") candidate = "/";
        else if (candidate.StartsWith("/ar/", StringComparison.Ordinal)) candidate = candidate[3..];

        if (ToArabicHome.Contains(candidate)) candidate = CultureUrls.ArabicHome;
        else if (ToEnglishHome.Contains(candidate)) candidate = CultureUrls.EnglishHome;

        if (candidate == path) return null;
        return SiteRoutes.MatchCanonical(candidate) is not null ? candidate : null;
    }

    public static IApplicationBuilder UseCultureUrlNormalizer(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
                && Target(request.Path.Value) is { } target)
            {
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = request.PathBase + target + request.QueryString;
                return;
            }
            await next();
        });
}
