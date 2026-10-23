using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers;

/// <summary>
/// GET/HEAD /forum-theme.css: the forum pages' stylesheet (<see cref="ForumThemeStylesheet"/>). Anonymous, no parameter
/// read (the ?v= of the link only makes the URL change with the content), the same bytes for every visitor and
/// language, text/css with an explicit charset, cached as immutable (nosniff comes from the global security headers). A
/// snapshot that cannot be loaded answers the default forum theme (200), never an error page.
/// </summary>
public class ForumThemeController(ISiteContentCache cache, ForumThemeStylesheet stylesheet, ILogger<ForumThemeController> logger) : Controller
{
    [HttpGet(ForumThemeStylesheet.Path), HttpHead(ForumThemeStylesheet.Path)]
    public async Task<IActionResult> Css()
    {
        SiteSnapshot? snapshot = null;
        try
        {
            snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Forum theme: the site snapshot could not be loaded; the default forum theme is served.");
        }
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Content(stylesheet.For(snapshot).Css, "text/css; charset=utf-8");
    }
}
