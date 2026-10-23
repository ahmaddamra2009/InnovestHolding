using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace InnovestHolding.ViewComponents;

/// <summary>
/// The forum pages' stylesheet link (the layout invokes it only when the shell is a forum page): /forum-theme.css with
/// the version of the current output, so a saved identity value is a new URL on the next request. If the snapshot cannot
/// be loaded the static rules' version is linked (the same fallback as the endpoint).
/// </summary>
public sealed class ForumThemeViewComponent(ISiteContentCache cache, ForumThemeStylesheet stylesheet,
    ILogger<ForumThemeViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        SiteSnapshot? snapshot = null;
        try
        {
            snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Forum theme: the site snapshot could not be loaded; the default forum theme is linked.");
        }
        var href = stylesheet.For(snapshot).Href;
        return new HtmlContentViewComponentResult(new Microsoft.AspNetCore.Html.HtmlString(
            "    <link rel=\"stylesheet\" href=\"" + System.Net.WebUtility.HtmlEncode(href) + "\">\n"));
    }
}
