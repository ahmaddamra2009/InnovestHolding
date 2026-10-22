using InnovestHolding.Infrastructure.Localization;

namespace InnovestHolding.Infrastructure.Seo;

public static class RobotsHeader
{
    public const string Value = "noindex, nofollow";

    /// <summary>
    /// <c>X-Robots-Tag: noindex, nofollow</c> on every HTML or JSON response whose path is not in
    /// <see cref="CultureUrls.Indexable"/> (login, admin, error pages, form JSON). Static files, robots.txt and the
    /// sitemap are neither HTML nor JSON and get no header. Fail-closed for SEO: a future public page is noindex until
    /// it is added to the indexable list.
    /// </summary>
    public static IApplicationBuilder UseRobotsHeader(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var type = context.Response.ContentType ?? "";
                var isPage = type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                             || type.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
                var indexable = CultureUrls.IsIndexable(context.Request.Path) && context.Response.StatusCode == StatusCodes.Status200OK;
                if (isPage && !indexable)
                    context.Response.Headers["X-Robots-Tag"] = Value;
                return Task.CompletedTask;
            });
            await next();
        });
}
