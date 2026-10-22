namespace InnovestHolding.Infrastructure.Seo;

public static class RobotsHeader
{
    public const string Value = "noindex, nofollow";
    public const string FollowValue = "noindex, follow";

    /// <summary>
    /// <c>X-Robots-Tag</c> on every HTML or JSON response, from the decision the page put in
    /// <c>HttpContext.Items[SeoPolicy.ItemKey]</c> (<see cref="SeoPolicy.Decide"/>): <see cref="RobotsDecision.Index"/> →
    /// no header, <see cref="RobotsDecision.NoIndexFollow"/> → <c>noindex, follow</c>. Anything else — no decision
    /// (login, admin, error pages, form JSON, a page someone forgot) or a status other than 200 — gets
    /// <c>noindex, nofollow</c>. Fail-closed: a page is indexable only when its controller says so. Static files,
    /// robots.txt and the sitemap are neither HTML nor JSON and get no header.
    /// </summary>
    public static IApplicationBuilder UseRobotsHeader(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var type = context.Response.ContentType ?? "";
                var isPage = type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                             || type.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
                if (!isPage) return Task.CompletedTask;
                var decision = context.Response.StatusCode == StatusCodes.Status200OK
                    && context.Items.TryGetValue(SeoPolicy.ItemKey, out var value) && value is RobotsDecision d
                        ? d
                        : RobotsDecision.NoIndexNoFollow;
                if (decision != RobotsDecision.Index)
                    context.Response.Headers["X-Robots-Tag"] = decision == RobotsDecision.NoIndexFollow ? FollowValue : Value;
                return Task.CompletedTask;
            });
            await next();
        });
}
