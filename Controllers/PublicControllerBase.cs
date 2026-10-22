using InnovestHolding.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers;

/// <summary>
/// Base of the public page controllers: the cached snapshot, the language of the URL, the page model of
/// Views/Site/Page.cshtml, the robots decision and the branded 404. Public controllers never share a name with an
/// admin controller (test PublicAndAdminControllerNamesDiffer).
/// </summary>
public abstract partial class PublicControllerBase : Controller
{
    protected bool English => Cultures.IsEnglishRequest;

    protected async Task<SiteViews> ViewsAsync()
    {
        var cache = HttpContext.RequestServices.GetRequiredService<ISiteContentCache>();
        return new SiteViews(await cache.GetAsync(HttpContext.RequestAborted), English);
    }

    protected string? BaseUrl(SiteViews v) => SiteUrl.Resolve(v.Content.Neutral(SiteUrl.ContentKey),
        HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>(), Request);

    /// <summary>Records the page's robots decision for the X-Robots-Tag middleware.</summary>
    protected void SetRobots(RobotsDecision decision) => HttpContext.Items[SeoPolicy.ItemKey] = decision;

    /// <summary>
    /// Renders a page of the route table: its title, intro and body from the Pages row, then <paramref name="blocks"/>.
    /// A page with no text and no content block (links and calls to action do not count) shows its admin-managed empty text with <paramref name="emptyAction"/>.
    /// </summary>
    protected IActionResult Page(string key, SiteViews v, IEnumerable<BlockVm?> blocks, LinkVm? emptyAction = null)
    {
        var page = SiteRoutes.Get(key);
        var row = v.Snapshot.Page(key);
        var baseUrl = BaseUrl(v);
        var list = blocks.OfType<BlockVm>().ToList();

        var intro = row is null ? LText.Empty : v.P(row.IntroAr, row.IntroEn);
        var body = row is null ? [] : SiteViews.Paragraphs(v.P(row.BodyAr, row.BodyEn));
        var hasContent = list.Any(b => b is not (CtaBlock or CardsBlock or LeadFormBlock { IsContent: false })) || !intro.IsEmpty || body.Count > 0;
        if (!hasContent && row is not null)
            list.Insert(0, new EmptyBlock(v.P(row.EmptyTextAr, row.EmptyTextEn), emptyAction));

        // The unit (Forum / Network / TV) the page belongs to: its tab bar.
        var unit = SiteRoutes.Chain(key).FirstOrDefault(p => p.Parent == PageKeys.Ecosystem);
        IReadOnlyList<LinkVm> tabs = unit is null
            ? []
            : new[] { unit }.Concat(SiteRoutes.Children(unit.Key)).Where(p => p.Key == unit.Key || v.InMenu(p.Key))
                .OrderBy(p => p.Key == unit.Key ? -1 : v.Snapshot.Page(p.Key)?.SortOrder ?? 0)
                .Select(p => new LinkVm(v.Url(p.Key), v.Title(p.Key), p.Key == key)).ToList();

        IReadOnlyList<(string, string?)>? listed = key switch
        {
            PageKeys.ForumEvents when list.OfType<EventsBlock>().FirstOrDefault() is { } paged =>
                paged.Events.Select(e => (e.Title.Value, (string?)e.Href)).ToList(),
            PageKeys.News when list.OfType<NewsBlock>().FirstOrDefault() is { } news =>
                news.News.Select(n => (n.Title.Value, (string?)n.Href)).ToList(),
            _ when page.Kind == PageKind.List && list.OfType<EntityGridBlock>().FirstOrDefault() is { } grid =>
                grid.Cards.Select(c => (c.Title.Value, (string?)c.Href)).ToList(),
            _ => null,
        };
        var seo = SeoBuilder.Build(page, v, baseUrl, listedNames: listed);
        SetRobots(seo.Robots);
        return View("~/Views/Site/Page.cshtml", new SitePageVm
        {
            Shell = v.Shell(key, baseUrl),
            Seo = seo,
            Key = key,
            Title = v.Title(key),
            Intro = intro,
            Body = body,
            UnitTabs = tabs,
            UnitTitle = unit is null ? LText.Empty : v.Title(unit.Key),
            Blocks = list,
        });
    }

    /// <summary>A static UI string (resx) in the page language.</summary>
    protected LText Ui(string key) =>
        new(HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>()[key].Value, false);

    /// <summary>A static UI string with arguments (numbers are passed already formatted).</summary>
    protected LText Ui(string key, params object[] args) =>
        new(HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>()[key, args].Value, false);

    /// <summary>
    /// Renders a detail page (Views/Site/Detail.cshtml): the shell has the list page active and the item as the last
    /// breadcrumb step; the language switch goes to the same item in the other language.
    /// </summary>
    protected IActionResult Detail(SiteViews v, DetailRoute route, string slug, SeoVm seo, DetailPageVm page)
    {
        var baseUrl = BaseUrl(v);
        var own = CultureUrls.Detail(route.Key, slug, English);
        page.Shell = v.Shell(route.Parent, baseUrl, CultureUrls.Detail(route.Key, slug, !English),
            new BreadcrumbVm(page.Title, null, baseUrl is null ? null : baseUrl + own));
        SetRobots(seo.Robots);
        return View("~/Views/Site/Detail.cshtml", page);
    }

    /// <summary>
    /// An unknown slug: when it is an old slug of an item that is still published, 301 to the item's current URL in
    /// the same language (the target is built from the route table and the stored slug only); otherwise the 404 page.
    /// </summary>
    protected async Task<IActionResult> OldSlugAsync(Data.AppDbContext db, string detailKey, string slug)
    {
        var route = SiteRoutes.Detail(detailKey);
        if (await DetailSlugs.CurrentAsync(db, route, slug, HttpContext.RequestAborted) is { } current && current != slug)
            return RedirectPermanent(Request.PathBase + CultureUrls.Detail(detailKey, current, English));
        return await NotFoundPageAsync();
    }

    /// <summary>
    /// The requested page of a list: null when the request must be answered otherwise (<paramref name="redirect"/> set
    /// for ?page=1, else 404 for anything that is not a page number in range).
    /// </summary>
    protected int? PageNumber(int total, int pageSize, out IActionResult? redirect)
    {
        redirect = null;
        var raw = Request.Query["page"];
        if (raw.Count == 0) return 1;
        if (raw.Count > 1 || !int.TryParse(raw[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n) || n < 1)
            return null;
        if (n == 1)
        {
            redirect = RedirectPermanent(Request.PathBase + Request.Path);
            return null;
        }
        var pageCount = Math.Max(1, (total + pageSize - 1) / pageSize);
        return n <= pageCount ? n : null;
    }

    private const string NotFoundKey = "404";

    /// <summary>The branded 404 (status 404, noindex), in the language of the URL.</summary>
    protected async Task<IActionResult> NotFoundPageAsync()
    {
        var v = await ViewsAsync();
        var baseUrl = BaseUrl(v);
        var page = SiteRoutes.Get(PageKeys.Home);
        var seo = SeoBuilder.Build(page, v, baseUrl, RobotsDecision.NoIndexNoFollow) with
        {
            Title = $"{HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>()[MessageKeys.Ui.NotFoundTitle].Value} | {v.Content.Text("site_name").Value}",
            Canonical = null,
            JsonLd = null,
        };
        Response.StatusCode = StatusCodes.Status404NotFound;
        SetRobots(RobotsDecision.NoIndexNoFollow);
        var links = new[] { PageKeys.Home, PageKeys.Ecosystem, PageKeys.Contact }
            .Select(k => new LinkVm(v.Url(k), v.Title(k))).ToList();
        // Not a page of the table: no breadcrumb, no active menu item; the language switch goes to the other home.
        return View("~/Views/Site/NotFound.cshtml", new NotFoundVm(v.Shell(NotFoundKey, baseUrl, CultureUrls.Home(!English)), seo, links));
    }
}
