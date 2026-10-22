namespace InnovestHolding.Infrastructure;

public static class SecurityMiddlewareExtensions
{
    /// <summary>The origin of the episode player (Infrastructure/Content/YouTube.EmbedUrl).</summary>
    public const string YouTubeFrameOrigin = "https://www.youtube-nocookie.com";

    /// <summary>
    /// Adds the baseline security headers to every response, static files included (register it before
    /// UseStaticFiles). A full script/style CSP is not set: Index.cshtml still has inline script/style.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            // frame-src: the only frame the site creates is the episode player (privacy-enhanced YouTube, built by
            // main.js on a click); any other frame is refused by the browser.
            h["Content-Security-Policy"] = "frame-ancestors 'none'; frame-src " + YouTubeFrameOrigin;
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next();
        });
}
