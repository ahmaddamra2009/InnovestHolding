using System.Diagnostics;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers;

public class HomeController : PublicControllerBase
{
    /// <summary>GET / (Arabic) and GET /en (English). Data from the cached snapshot: 0 queries on a cache hit.</summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        var baseUrl = BaseUrl(v);
        var seo = SeoBuilder.Build(SiteRoutes.Get(PageKeys.Home), v, baseUrl);
        SetRobots(seo.Robots);
        return View(HomeViewModel.Create(v, v.Shell(PageKeys.Home, baseUrl), seo));
    }

    /// <summary>
    /// The branded 404 for every path nothing else serves (MapFallbackToController; missing static files keep the
    /// plain 404). It runs on the original path, so /en/… gets the English page. Any method: an unknown POST without a
    /// valid antiforgery token is refused (400) before it gets here.
    /// </summary>
    [AllowAnonymous]
    public Task<IActionResult> NotFoundPage() => NotFoundPageAsync();

    /// <summary>
    /// Target of UseExceptionHandler. The handler re-executes the ORIGINAL method, so a failed POST
    /// arrives here as a POST without a valid token — hence the only [IgnoreAntiforgeryToken] in the app.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
