namespace InnovestHolding.Infrastructure;

public static class SecurityMiddlewareExtensions
{
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
            h["Content-Security-Policy"] = "frame-ancestors 'none'";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next();
        });
}
