namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// 301s the aliases of the two home pages to their canonical URL (GET/HEAD only — a 301 on a POST would break the
/// forms). Targets are fixed local paths; only the request's query string is appended (no open redirect).
/// <code>
/// /ar, /ar/                                → /
/// /Home, /Home/, /Home/Index, /Home/Index/ → /      (any case)
/// /en/, /EN, /En/, …                        → /en    (anything but exactly "/en")
/// /en/Index, /en/Index/                    → /en    (any case)
/// </code>
/// </summary>
public static class CultureUrlNormalizer
{
    private static readonly string[] ToArabicHome = ["/ar", "/ar/", "/home", "/home/", "/home/index", "/home/index/"];
    private static readonly string[] ToEnglishHome = ["/en", "/en/", "/en/index", "/en/index/"];

    /// <summary>The canonical target for <paramref name="path"/>, or null when the path is already canonical.</summary>
    public static string? Target(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var lower = path.ToLowerInvariant();
        if (ToArabicHome.Contains(lower)) return CultureUrls.ArabicHome;
        if (ToEnglishHome.Contains(lower) && path != CultureUrls.EnglishHome) return CultureUrls.EnglishHome;
        return null;
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
