using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers;

/// <summary>
/// GET/HEAD /forum-theme.css: the forum pages' stylesheet (<see cref="ForumThemeStylesheet"/>). Anonymous, the same
/// bytes for every visitor, language and query; text/css with an explicit charset; immutable only when ?v= is the current
/// version, else no-cache (nosniff comes from the global security headers). A
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
        var output = stylesheet.For(snapshot);
        // Cached forever only under the URL of the current version (the one the pages link). Any other ?v= (an old page
        // still open after a save, a restored tab) gets the current CSS without being kept, so an old version URL that
        // becomes current again after a reset is never pinned to the wrong content. The value is only compared, never
        // written into the response.
        Response.Headers.CacheControl = Request.Query["v"] is [var v] && v == output.Version
            ? "public, max-age=31536000, immutable"
            : "no-cache";
        return Content(output.Css, "text/css; charset=utf-8");
    }
}
