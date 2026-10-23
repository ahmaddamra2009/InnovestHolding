using InnovestHolding.Infrastructure.Content;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.ViewComponents;

/// <summary>
/// The brand part of the &lt;head&gt; (theme colour, favicon, apple-touch icon) for the public layout and the login page,
/// read from the cached site snapshot (no query on a cache hit). If the snapshot cannot be loaded — the error page of a
/// database failure — the bundled set is rendered, so the error page never fails because of its icons.
/// </summary>
public sealed class BrandHeadViewComponent(ISiteContentCache cache, ILogger<BrandHeadViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        BrandHeadVm model;
        try
        {
            model = BrandHeadVm.From(await cache.GetAsync(HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Brand head: the site snapshot could not be loaded; the bundled icons are used.");
            model = BrandHeadVm.Default;
        }
        return View("~/Views/Shared/_BrandHead.cshtml", model);
    }
}
