using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using C = InnovestHolding.Infrastructure.Content.ContentItemGroups;

namespace InnovestHolding.Controllers;

// The public pages of the route table (Infrastructure/Site/SiteRoutes). Routes are mapped in Program.cs from the table,
// GET/HEAD only; every text comes from the database through the cached snapshot (list pages query their rows).

public class SitePagesController : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> About()
    {
        var v = await ViewsAsync();
        var c = v.Content;
        var founder = c.Text("founder_message");
        return Page(PageKeys.About, v,
        [
            new VisionBlock(
            [
                (c.Text("vision_title"), c.Text("vision_text"), Icons.Svg("eye")!),
                (c.Text("mission_title"), c.Text("mission_text"), Icons.Svg("target")!),
            ]),
            v.ItemsSection(C.Values),
            v.ItemsSection(C.WhyPoints, compact: true),
            v.Snapshot.Stats.Count == 0 ? null : new StatsBlock(c.Text("impact_title"), c.Text("impact_intro"), v.Stats()),
            v.ItemsSection(C.Sectors, compact: true),
            v.ItemsSection(C.PartnersInSuccess),
            founder.IsEmpty ? null : new FounderBlock(c.Text("founder_heading"), c.Text("founder_name"), c.Text("founder_title"), founder),
            v.CtaBand(),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Ecosystem()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Ecosystem, v,
        [
            new CardsBlock(v.Content.Text("ecosystem_title"), v.Content.Text("ecosystem_subtitle"), v.Cards(PageKeys.Ecosystem, true), true),
            v.ItemsSection(C.EcosystemPillars, compact: true),
            v.Snapshot.Platforms.Count == 0 ? null : new ServicesBlock(v.Header("platforms", SiteViews.BadgeGrid), v.Services(), v.Content.Text("platforms_read_more")),
            v.ItemsSection(C.Programs),
            v.ItemsSection(C.GovernmentServices),
            v.ItemsSection(C.UniversityServices),
            v.CtaBand(),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Contact()
    {
        var v = await ViewsAsync();
        var faqs = v.Faqs();
        return Page(PageKeys.Contact, v,
        [
            // The page intro already introduces the channels: the block repeats only the details.
            new ContactBlock(v.ContactForm("contact") with { Intro = LText.Empty }),
            faqs.Count == 0 ? null : new FaqBlock(v.Header("faq", SiteViews.BadgeQuestion), faqs),
        ]);
    }
}

public class ForumController(AppDbContext db) : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        var events = v.EventCards(v.Snapshot.Events.Take(3));
        return Page(PageKeys.Forum, v,
        [
            new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Forum, false), false),
            v.ItemsSection(C.ForumComponents),
            events.Count == 0 ? null : new EventsBlock(events, null, new LinkVm(v.Url(PageKeys.ForumEvents), v.Title(PageKeys.ForumEvents))),
            v.CtaBand(),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Events()
    {
        var v = await ViewsAsync();
        var size = ContentKeys.PageSize(v.Snapshot.Content);
        var total = v.Snapshot.EventsCount.Total;
        if (PageNumber(total, size, out var redirect) is not { } n) return redirect ?? await NotFoundPageAsync();

        var rows = await db.Events.AsNoTracking().Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id).Skip((n - 1) * size).Take(size)
            .Select(e => new EventRow(e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn, e.EventDay,
                e.EventMonthAr, e.EventMonthEn, e.LocationAr, e.LocationEn, e.TimeAr, e.TimeEn))
            .ToListAsync(HttpContext.RequestAborted);
        var register = new LinkVm(v.Url(PageKeys.ForumRegistration), v.Title(PageKeys.ForumRegistration));
        return Page(PageKeys.ForumEvents, v,
            [rows.Count == 0 ? null : new EventsBlock(v.EventCards(rows), Pager(total, size, n, v.Url(PageKeys.ForumEvents)), register)],
            register);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Speakers()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.ForumSpeakers, v, [], new LinkVm(v.Url(PageKeys.ForumRegistration), v.Title(PageKeys.ForumRegistration)));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Partners()
    {
        var v = await ViewsAsync();
        var partners = v.Partners();
        return Page(PageKeys.ForumPartners, v,
        [
            partners.Count == 0
                ? new EmptyBlock(v.P(v.Snapshot.Page(PageKeys.ForumPartners)?.EmptyTextAr, v.Snapshot.Page(PageKeys.ForumPartners)?.EmptyTextEn),
                    new LinkVm(v.Url(PageKeys.Contact), v.Title(PageKeys.Contact)))
                : new PartnersBlock(v.Content.Text("partners_title"), partners),
            v.ItemsSection(C.PartnerTypes),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Registration()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.ForumRegistration, v, [new RegisterBlock(v.RegisterForm("forum-reg"))]);
    }

    internal static PagerVm? Pager(int total, int size, int page, string url)
    {
        var count = (total + size - 1) / size;
        return count > 1 ? new PagerVm(page, count, url) : null;
    }
}

public class NetworkController : PublicControllerBase
{
    private LinkVm Join(SiteViews v) => new(v.Url(PageKeys.NetworkJoin), v.Title(PageKeys.NetworkJoin));

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Network, v,
        [
            new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Network, false), false),
            v.ItemsSection(C.InvestorServices),
            v.ItemsSection(C.NetworkSteps),
            v.CtaBand(),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Investors()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.NetworkInvestors, v,
            [v.ItemsSection(C.InvestorServices), new CtaBlock(v.Title(PageKeys.NetworkJoin), v.Summary(PageKeys.NetworkJoin), [Join(v)])]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Startups()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.NetworkStartups, v,
        [
            v.ItemsSection(C.StartupServices),
            v.ItemsSection(C.Sectors, compact: true),
            new CtaBlock(v.Title(PageKeys.NetworkJoin), v.Summary(PageKeys.NetworkJoin), [Join(v)]),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Opportunities()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.NetworkOpportunities, v, [], Join(v));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Join()
    {
        var v = await ViewsAsync();
        // The application form arrives in a later phase: until then the page sends visitors to the contact form.
        return Page(PageKeys.NetworkJoin, v,
            [new CtaBlock(v.Title(PageKeys.Contact), v.Summary(PageKeys.Contact), [new LinkVm(v.Url(PageKeys.Contact), v.Title(PageKeys.Contact))])]);
    }
}

public class TvController : PublicControllerBase
{
    private static LinkVm Sponsor(SiteViews v) => new(v.Url(PageKeys.TvSponsorship), v.Title(PageKeys.TvSponsorship));

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Tv, v, [new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Tv, false), false)], Sponsor(v));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Episodes()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.TvEpisodes, v, [], Sponsor(v));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Guests()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.TvGuests, v, [], Sponsor(v));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Sponsorship()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.TvSponsorship, v, [], new LinkVm(v.Url(PageKeys.Contact), v.Title(PageKeys.Contact)));
    }
}

public class NewsroomController(AppDbContext db) : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        var size = ContentKeys.PageSize(v.Snapshot.Content);
        var total = v.Snapshot.NewsCount.Total;
        if (PageNumber(total, size, out var redirect) is not { } n) return redirect ?? await NotFoundPageAsync();

        var rows = await db.News.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip((n - 1) * size).Take(size)
            .Select(x => new NewsRow(x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.CategoryAr, x.CategoryEn, x.NewsDateAr, x.NewsDateEn, x.LinkUrl))
            .ToListAsync(HttpContext.RequestAborted);
        return Page(PageKeys.News, v,
            [rows.Count == 0 ? null : new NewsBlock(v.NewsCards(rows), ForumController.Pager(total, size, n, v.Url(PageKeys.News)), v.Content.Text("news_read_more"))],
            new LinkVm(v.Url(PageKeys.Contact), v.Title(PageKeys.Contact)));
    }
}
