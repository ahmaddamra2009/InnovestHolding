using System.Globalization;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Forms;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        var s = v.Snapshot;
        return Page(PageKeys.About, v,
        [
            ("vision", new VisionBlock(
            [
                (c.Text("vision_title"), c.Text("vision_text"), v.Icon("vision_icon")),
                (c.Text("mission_title"), c.Text("mission_text"), v.Icon("mission_icon")),
            ])),
            ("values", v.ItemsSection(C.Values)),
            ("why", v.ItemsSection(C.WhyPoints, compact: true)),
            ("stats", s.Stats.Count == 0 ? null : new StatsBlock(c.Text("impact_title"), c.Text("impact_intro"), v.Stats())),
            ("sectors", v.ItemsSection(C.Sectors, compact: true)),
            ("partners_in_success", v.ItemsSection(C.PartnersInSuccess)),
            ("founder", founder.IsEmpty ? null : new FounderBlock(c.Text("founder_heading"), c.Text("founder_name"), c.Text("founder_title"), founder,
                v.Image(s.FounderPhoto, c.Text("founder_name")))),
            ("cta_band", v.CtaBand()),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Ecosystem()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Ecosystem, v,
        [
            ("units", new CardsBlock(v.Content.Text("ecosystem_title"), v.Content.Text("ecosystem_subtitle"), v.Cards(PageKeys.Ecosystem, true), true)),
            ("pillars", v.ItemsSection(C.EcosystemPillars, compact: true)),
            ("services", v.Snapshot.Platforms.Count == 0 ? null : new ServicesBlock(
                v.Header("platforms"), v.Services(), v.Content.Text("platforms_read_more"))),
            ("programs", v.ItemsSection(C.Programs)),
            ("government", v.ItemsSection(C.GovernmentServices)),
            ("universities", v.ItemsSection(C.UniversityServices)),
            ("cta_band", v.CtaBand()),
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
            ("contact", new ContactBlock(v.ContactForm("contact") with { Intro = LText.Empty })),
            ("faq", faqs.Count == 0 ? null : new FaqBlock(v.Header("faq"), faqs)),
        ]);
    }

    /// <summary>
    /// GET /privacy (and /en/privacy): the policy is the page's own intro and body (plain text, encoded; a blank line =
    /// a new paragraph) — no sections. Unpublished → the branded 404. While the admin has emptied it, the page shows its
    /// "coming soon" text with a button to the Contact page (its robots decision is SeoPolicy's: noindex, follow).
    /// </summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Privacy()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Privacy, v, [], v.PageLink(PageKeys.Contact));
    }
}

public class ForumController(AppDbContext db, TimeProvider time) : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        var events = v.EventCards(v.Snapshot.Events.Take(v.Count(ContentKeys.ForumEventsLimit)), time.GetUtcNow());
        // Every section is a hideable, movable PageSections block (order: PageSectionRegistry.Arrange); an empty one renders
        // nothing.
        return Page(PageKeys.Forum, v,
        [
            ("about", v.ForumAbout()),
            ("units", new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Forum, false), false)),
            ("figures", v.Figures(C.ForumFigures)),
            ("audiences", v.BulletCards(C.ForumAudiences, ContentKeys.Forum.StrategicLinkTitle, ContentKeys.Forum.StrategicLinkText)),
            ("pillars", v.BulletCards(C.ForumPillars)),
            ("goals", v.Numbered(C.ForumGoals)),
            ("sectors", v.Numbered(C.ForumSectors)),
            ("components", v.ItemsSection(C.ForumComponents)),
            // The block's link is "all events" (not the event action).
            ("events", events.Count == 0 ? null : new EventsBlock(events, null, v.PageLink(PageKeys.ForumEvents))),
            ("editions", v.EditionsTeaser()),
            ("patronage", v.Patronage()),
            ("contact", v.ForumContact()),
            ("cta_band", v.CtaBand()),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Events()
    {
        var v = await ViewsAsync();
        if (Unpublished(v, PageKeys.ForumEvents)) return NotFoundPage(v);
        var size = ContentKeys.PageSize(v.Snapshot.Content);
        var total = v.Snapshot.EventsCount.Total;
        if (PageNumber(total, size, out var redirect) is not { } n) return redirect ?? NotFoundPage(v);

        var now = time.GetUtcNow();
        var rows = await EventQueries.Ordered(db.Events.AsNoTracking().Where(e => e.IsActive), now)
            .Skip((n - 1) * size).Take(size)
            .Select(EventQueries.Row)
            .ToListAsync(HttpContext.RequestAborted);
        var action = v.EventsAction();
        return Page(PageKeys.ForumEvents, v,
            [("list", rows.Count == 0 ? null : new EventsBlock(v.EventCards(rows, now), Pager(total, size, n, v.Url(PageKeys.ForumEvents)), action))],
            action);
    }

    /// <summary>GET /forum/events/{slug} (and /en/…): an active event; an old slug 301s to the current one.</summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Event(string slug)
    {
        var e = await db.Events.AsNoTracking().Where(x => x.Slug == slug && x.IsActive)
            .Select(x => new
            {
                Row = new EventRow(x.TitleAr, x.TitleEn, x.DescriptionAr, x.DescriptionEn, x.EventDay, x.EventMonthAr, x.EventMonthEn,
                    x.LocationAr, x.LocationEn, x.TimeAr, x.TimeEn, x.Slug, x.SummaryAr, x.SummaryEn, x.StartsAt, x.EndsAt, x.Status,
                    x.Image == null ? null : new MediaRef(x.Image.FileKey, x.Image.Format, x.Image.Width, x.Image.Height, x.Image.AltAr, x.Image.AltEn),
                    x.DateIsMonth),
                x.Id, x.AttendanceMode, x.CityAr, x.CityEn, x.AddressAr, x.AddressEn, x.OnlineUrl, x.RegistrationOpen,
                x.MetaTitleAr, x.MetaTitleEn, x.MetaDescriptionAr, x.MetaDescriptionEn,
            })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (e is null)
            return await OldSlugAsync(db, DetailKeys.ForumEvent, slug);

        var v = await ViewsAsync();
        var r = e.Row;
        var english = v.English;
        var now = time.GetUtcNow();
        var past = EventQueries.IsPast(r.StartsAt, r.EndsAt, now);
        var title = v.P(r.TitleAr, r.TitleEn);
        var summary = v.P(r.SummaryAr, r.SummaryEn);
        var description = v.P(r.DescriptionAr, r.DescriptionEn);
        var available = EventQueries.HasEnglish(r.TitleEn, r.SummaryEn, r.DescriptionEn);
        var venue = v.P(r.LocationAr, r.LocationEn);
        var city = v.P(e.CityAr, e.CityEn);
        var address = v.P(e.AddressAr, e.AddressEn);
        var online = e.AttendanceMode != AttendanceMode.Offline && SiteUrl.IsHttpsUrl(e.OnlineUrl) ? e.OnlineUrl!.Trim() : null;

        var facts = new List<FactVm>();
        // F10: an event known to the month only: "June 2026", no day and no clock time.
        var monthOnly = r.DateIsMonth && r.StartsAt is not null;
        if (monthOnly)
        {
            facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.EventDate), new LText(EventDates.MonthYear(r.StartsAt!.Value, english), false)));
            var time0 = v.P(r.TimeAr, r.TimeEn);
            if (!time0.IsEmpty) facts.Add(new FactVm("clock", Ui(MessageKeys.Ui.EventTime), time0));
        }
        else if (r.StartsAt is { } start)
        {
            facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.EventDate), new LText(EventDates.DateLine(start, r.EndsAt, english), false)));
            var time0 = v.P(r.TimeAr, r.TimeEn);
            facts.Add(new FactVm("clock", Ui(MessageKeys.Ui.EventTime),
                time0.IsEmpty ? new LText(EventDates.TimeRange(start, r.EndsAt, english), false) : time0));
        }
        else
        {
            var day = r.EventDay.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var month = v.P(r.EventMonthAr, r.EventMonthEn);
            facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.EventDate), new LText(day + " " + month.Value, month.IsFallback)));
            var time0 = v.P(r.TimeAr, r.TimeEn);
            if (!time0.IsEmpty) facts.Add(new FactVm("clock", Ui(MessageKeys.Ui.EventTime), time0));
        }
        var place = string.Join(" — ", new[] { venue, address, city }.Where(t => !t.IsEmpty).Select(t => t.Value));
        if (place.Length > 0)
            facts.Add(new FactVm("pin", Ui(MessageKeys.Ui.EventVenue),
                new LText(place, english && new[] { venue, address, city }.Any(t => t.IsFallback))));
        if (e.AttendanceMode != AttendanceMode.Offline)
            facts.Add(new FactVm("globe", Ui(MessageKeys.Ui.EventOnline), Ui(MessageKeys.Ui.EventJoinOnline), online));

        var badge = r.Status switch
        {
            EventStatus.Cancelled => Ui(MessageKeys.Ui.EventCancelled),
            EventStatus.Postponed => Ui(MessageKeys.Ui.EventPostponed),
            _ when past => Ui(MessageKeys.Ui.EventPast),
            _ => LText.Empty,
        };
        var actions = new List<LinkVm>();
        if (e.RegistrationOpen && !past && r.Status != EventStatus.Cancelled && v.EventsAction() is { } register)
            actions.Add(register);

        // schema.org Event needs a real start date; an event that only has the free-text day/month is described as a
        // web page (no fabricated date).
        var route = SiteRoutes.Detail(DetailKeys.ForumEvent);
        var baseUrl = BaseUrl(v);
        var lead = summary.IsEmpty ? description : summary;
        var metaDescription = v.P(e.MetaDescriptionAr, e.MetaDescriptionEn);
        // F10: an event known to the month only has no real start date either (WebPage, no Event markup).
        var exact = r.StartsAt is not null && !monthOnly;
        var node = new Dictionary<string, object> { ["@type"] = exact ? "Event" : "WebPage" };
        SeoBuilder.Set(node, "name", title.Value);
        SeoBuilder.Set(node, "description", SeoBuilder.Shorten(lead.Value, 300));
        if (exact && r.StartsAt is { } s1)
        {
            node["startDate"] = EventDates.Iso(s1);
            if (r.EndsAt is { } e1) node["endDate"] = EventDates.Iso(e1);
            node["eventStatus"] = "https://schema.org/" + r.Status switch
            {
                EventStatus.Cancelled => "EventCancelled",
                EventStatus.Postponed => "EventPostponed",
                _ => "EventScheduled",
            };
            node["eventAttendanceMode"] = "https://schema.org/" + e.AttendanceMode switch
            {
                AttendanceMode.Online => "OnlineEventAttendanceMode",
                AttendanceMode.Mixed => "MixedEventAttendanceMode",
                _ => "OfflineEventAttendanceMode",
            };
            var locations = new List<object>();
            if (e.AttendanceMode != AttendanceMode.Online && !venue.IsEmpty)
            {
                var placeNode = new Dictionary<string, object> { ["@type"] = "Place", ["name"] = venue.Value };
                var postal = new Dictionary<string, object> { ["@type"] = "PostalAddress" };
                SeoBuilder.Set(postal, "streetAddress", address.Value);
                SeoBuilder.Set(postal, "addressLocality", city.Value);
                SeoBuilder.Set(placeNode, "address", postal);
                locations.Add(placeNode);
            }
            if (online is not null) locations.Add(new Dictionary<string, object> { ["@type"] = "VirtualLocation", ["url"] = online });
            if (locations.Count == 1) node["location"] = locations[0];
            else if (locations.Count > 1) node["location"] = locations;
            SeoBuilder.Set(node, "organizer", SeoBuilder.ForumOrganizationRef(baseUrl, English));
        }

        // The event's published speakers, in the order of the speakers list (the admin's picker shows that order).
        var speakers = (await db.EventSpeakers.AsNoTracking()
                .Where(es => es.EventId == e.Id && es.Speaker!.IsActive)
                .OrderBy(es => es.Speaker!.SortOrder).ThenBy(es => es.SpeakerId)
                .Select(es => es.Speaker!)
                .Select(EcosystemQueries.PersonCard<Speaker>())
                .ToListAsync(HttpContext.RequestAborted))
            .Select(s => PersonCard(v, s, DetailKeys.ForumSpeaker)).ToList();
        if (exact && speakers.Count > 0) node["performer"] = PersonRefs(baseUrl, speakers);
        // F10: an event without an Arabic summary or description is thin in both languages: noindex,follow, no hreflang,
        // out of the sitemap (SeoController uses the same rule).
        var seo = SeoBuilder.BuildDetail(route, r.Slug, v, baseUrl, available, title, v.P(e.MetaTitleAr, e.MetaTitleEn).Value,
            metaDescription.IsEmpty ? lead.Value : metaDescription.Value, r.Image, "website", node,
            indexable: EventQueries.HasText(r.SummaryAr, r.DescriptionAr));
        return Detail(v, route, r.Slug, seo, new DetailPageVm
        {
            Seo = seo,
            Kind = "event",
            Title = title,
            Lead = summary,
            Body = SiteViews.Paragraphs(description),
            Image = v.Image(r.Image, title),
            Badge = badge,
            Facts = facts,
            Actions = actions,
            Notice = past && r.Status != EventStatus.Cancelled ? Ui(MessageKeys.Ui.EventEndedNotice) : LText.Empty,
            ArabicOnly = english && !available,
            Back = new LinkVm(v.Url(PageKeys.ForumEvents), v.Title(PageKeys.ForumEvents)),
            Related = speakers.Count == 0 ? [] : [new RelatedVm(v.Title(PageKeys.ForumSpeakers), speakers, true)],
        });
    }

    [AcceptVerbs("GET", "HEAD")]
    public Task<IActionResult> Speakers() =>
        EntityListAsync(PageKeys.ForumSpeakers,
            EcosystemQueries.PeopleOrdered(db.Speakers.AsNoTracking().Where(s => s.IsActive)).Select(EcosystemQueries.PersonCard<Speaker>()),
            (v, s) => PersonCard(v, s, DetailKeys.ForumSpeaker), true, v => v.EventsAction());

    /// <summary>GET /forum/speakers/{slug} (and /en/…): a published speaker and the published events they speak at.</summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Speaker(string slug)
    {
        var now = time.GetUtcNow();
        return await PersonPageAsync<Speaker>(db, DetailKeys.ForumSpeaker, slug, async (v, id) =>
        {
            var events = await EventQueries.Ordered(
                    db.Events.AsNoTracking().Where(e => e.IsActive && db.EventSpeakers.Any(es => es.EventId == e.Id && es.SpeakerId == id)), now)
                .Select(EventQueries.Row).ToListAsync(HttpContext.RequestAborted);
            return events.Count == 0 ? null : new RelatedVm(v.Title(PageKeys.ForumEvents), EventCards(v, events, now), false);
        }, v => Actions(v.EventsAction()));
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Partners()
    {
        var v = await ViewsAsync();
        var groups = v.PartnerGroups();
        var row = v.Snapshot.Page(PageKeys.ForumPartners);
        return Page(PageKeys.ForumPartners, v,
        [
            ("partners", groups.Count == 0
                ? new EmptyBlock(v.P(row?.EmptyTextAr, row?.EmptyTextEn),
                    v.EmptyAction(PageKeys.ForumPartners, v.PageLink(PageKeys.Contact)))
                : new PartnersBlock(v.Content.Text("partners_title"), groups)),
            ("packages", Packages(v, SponsorshipUnits.Forum)),
            ("partner_types", v.ItemsSection(C.PartnerTypes)),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Registration()
    {
        var v = await ViewsAsync();
        if (Unpublished(v, PageKeys.ForumRegistration)) return NotFoundPage(v);
        // F10: only events that have not passed (the POST checks the same list).
        var events = await LeadFormChoices.EventsAsync(db, time.GetUtcNow(), HttpContext.RequestAborted);
        return Page(PageKeys.ForumRegistration, v,
        [
            ("form", new LeadFormBlock(v.ForumRegistrationForm(events, v.Content.Text("cta_register")))),
            ("contact", v.ForumContact()),
        ]);
    }

    /// <summary>
    /// GET /forum/editions (and /en/…), F10: every active past edition with its photos (a gallery; no detail pages, no
    /// paging). None → the admin's empty text. Thin (noindex,follow, out of the sitemap) while no edition is active
    /// (English: none with English text) — SeoPolicy.
    /// </summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Editions()
    {
        var v = await ViewsAsync();
        if (Unpublished(v, PageKeys.ForumEditions)) return NotFoundPage(v);
        var row = v.Snapshot.Page(PageKeys.ForumEditions);
        BlockVm list = v.Snapshot.Editions.Count == 0
            ? new EmptyBlock(v.P(row?.EmptyTextAr, row?.EmptyTextEn), v.EmptyAction(PageKeys.ForumEditions, v.PageLink(PageKeys.Forum)))
            : new EditionsBlock(LText.Empty, v.Snapshot.Editions.Select(e => v.Edition(e)).ToList(), null, false);
        return Page(PageKeys.ForumEditions, v, [("list", list)]);
    }

    internal static PagerVm? Pager(int total, int size, int page, string url)
    {
        var count = (total + size - 1) / size;
        return count > 1 ? new PagerVm(page, count, url) : null;
    }
}

public class NetworkController(AppDbContext db, IStringLocalizer<SharedResource> l) : PublicControllerBase
{
    /// <summary>
    /// The Join page, with the applicant type pre-selected when <paramref name="type"/> is given (?type=); null while the
    /// Join page is unpublished (no link to a 404).
    /// </summary>
    private static LinkVm? Join(SiteViews v, string? type = null) =>
        v.PageLink(PageKeys.NetworkJoin) is { } join ? join with { Href = join.Href + (type is null ? "" : "?type=" + type) } : null;

    /// <summary>The call to join of the Investors/Startups pages (hideable section "join_cta"); null without a Join link.</summary>
    private static CtaBlock? JoinCta(SiteViews v, string type) =>
        Join(v, type) is { } join ? new CtaBlock(v.Title(PageKeys.NetworkJoin), v.Summary(PageKeys.NetworkJoin), [join]) : null;

    /// <summary>
    /// The "express interest" action of an opportunity page: the interest form under the opportunities list (its section
    /// anchor) with this opportunity pre-selected (?opportunity=id; the list page offers only open, published ones).
    /// </summary>
    private LinkVm InterestAction(SiteViews v, int id) =>
        new(v.Url(PageKeys.NetworkOpportunities) + "?" + OpportunityQuery + "=" + id.ToString(CultureInfo.InvariantCulture)
            + "#" + InterestFormId + "-section", Ui(MessageKeys.Ui.ExpressInterest));

    /// <summary>The id prefix of the interest form (LeadFormViews.InterestForm).</summary>
    private const string InterestFormId = "interest";

    /// <summary>The query parameter that pre-selects an opportunity in the interest form.</summary>
    public const string OpportunityQuery = "opportunity";

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Network, v,
        [
            ("units", new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Network, false), false)),
            ("investor_services", v.ItemsSection(C.InvestorServices)),
            ("steps", v.ItemsSection(C.NetworkSteps)),
            ("cta_band", v.CtaBand()),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Investors()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.NetworkInvestors, v,
        [
            ("investor_services", v.ItemsSection(C.InvestorServices)),
            ("join_cta", JoinCta(v, Models.NetworkApplication.Investor)),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Startups()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.NetworkStartups, v,
        [
            ("startup_services", v.ItemsSection(C.StartupServices)),
            ("sectors", v.ItemsSection(C.Sectors, compact: true)),
            ("join_cta", JoinCta(v, Models.NetworkApplication.Startup)),
        ]);
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Opportunities()
    {
        // ?opportunity=id pre-selects an open opportunity in the form; any other value selects nothing (never an error).
        var query = Request.Query[OpportunityQuery];
        int? selected = query.Count == 1 && int.TryParse(query[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
        return await EntityListAsync(PageKeys.NetworkOpportunities,
            EcosystemQueries.OpportunitiesOrdered(db.InvestmentOpportunities.AsNoTracking().Where(o => o.IsActive)).Select(EcosystemQueries.OpportunityCard),
            OpportunityCard, false, v => Join(v),
            // The "express interest" form under the list (not content: an empty list stays noindex).
            // Its select comes from the cached snapshot (open + published, invalidated on every admin save); the POST
            // re-checks the chosen id against the database (ILeadReferences.OpenOpportunityAsync).
            ("interest_form", v => new LeadFormBlock(v.InterestForm(v.Snapshot.Ecosystem.OpenOpportunityChoices, selected,
                new LText(l[MessageKeys.Ui.FormSubmitInterest].Value, false)), IsContent: false)));
    }

    /// <summary>
    /// GET /angel-network/opportunities/{slug} (and /en/…): a published opportunity (a draft is 404); an old slug 301s.
    /// Only the figures the admin wrote are shown; schema.org WebPage (no offer or financial-product claim).
    /// </summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Opportunity(string slug)
    {
        var ct = HttpContext.RequestAborted;
        var o = await db.InvestmentOpportunities.AsNoTracking().Where(x => x.Slug == slug && x.IsActive)
            .Select(x => new
            {
                x.Id, x.Slug, x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.BodyAr, x.BodyEn, x.StageTextAr, x.StageTextEn,
                x.AskTextAr, x.AskTextEn, x.IsOpen, x.MetaTitleAr, x.MetaTitleEn, x.MetaDescriptionAr, x.MetaDescriptionEn,
                SectorAr = x.SectorItem == null ? null : x.SectorItem.TextAr,
                SectorEn = x.SectorItem == null ? null : x.SectorItem.TextEn,
                Image = x.Image == null ? null : new MediaRef(x.Image.FileKey, x.Image.Format, x.Image.Width, x.Image.Height, x.Image.AltAr, x.Image.AltEn),
            })
            .FirstOrDefaultAsync(ct);
        if (o is null)
            return await OldSlugAsync(db, DetailKeys.NetworkOpportunity, slug);

        var v = await ViewsAsync();
        var title = v.P(o.TitleAr, o.TitleEn);
        var summary = v.P(o.SummaryAr, o.SummaryEn);
        var available = EcosystemQueries.OpportunityHasEnglish(o.TitleEn, o.SummaryEn);
        var facts = new List<FactVm>();
        var sector = v.P(o.SectorAr, o.SectorEn);
        var stage = v.P(o.StageTextAr, o.StageTextEn);
        var ask = v.P(o.AskTextAr, o.AskTextEn);
        if (!sector.IsEmpty) facts.Add(new FactVm("layers", Ui(MessageKeys.Ui.Sector), sector));
        if (!stage.IsEmpty) facts.Add(new FactVm("rocket", Ui(MessageKeys.Ui.Stage), stage));
        if (!ask.IsEmpty) facts.Add(new FactVm("coins", Ui(MessageKeys.Ui.Ask), ask));

        var actions = new List<LinkVm>();
        // "Express interest" opens the form under the opportunities list: not while the admin hides that form.
        if (o.IsOpen && PageSectionRegistry.IsVisible(v.Snapshot, PageKeys.NetworkOpportunities, "interest_form"))
            actions.Add(InterestAction(v, o.Id));
        if (Join(v) is { } join) actions.Add(join);

        var route = SiteRoutes.Detail(DetailKeys.NetworkOpportunity);
        var baseUrl = BaseUrl(v);
        var node = new Dictionary<string, object> { ["@type"] = "WebPage" };
        SeoBuilder.Set(node, "name", title.Value);
        SeoBuilder.Set(node, "description", SeoBuilder.Shorten(summary.Value, 300));
        var metaDescription = v.P(o.MetaDescriptionAr, o.MetaDescriptionEn);
        var seo = SeoBuilder.BuildDetail(route, o.Slug, v, baseUrl, available, title, v.P(o.MetaTitleAr, o.MetaTitleEn).Value,
            metaDescription.IsEmpty ? summary.Value : metaDescription.Value, o.Image, "website", node);
        return Detail(v, route, o.Slug, seo, new DetailPageVm
        {
            Seo = seo,
            Kind = "opportunity",
            Title = title,
            Lead = summary,
            Body = SiteViews.Paragraphs(v.P(o.BodyAr, o.BodyEn)),
            Image = v.Image(o.Image, title),
            Badge = Ui(o.IsOpen ? MessageKeys.Ui.OpportunityOpen : MessageKeys.Ui.OpportunityClosed),
            Facts = facts,
            Actions = actions,
            ArabicOnly = v.English && !available,
            Back = new LinkVm(v.Url(PageKeys.NetworkOpportunities), v.Title(PageKeys.NetworkOpportunities)),
        });
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Join()
    {
        var v = await ViewsAsync();
        if (Unpublished(v, PageKeys.NetworkJoin)) return NotFoundPage(v);
        var choices = await LeadFormChoices.LoadAsync(db, HttpContext.RequestAborted);
        var type = Request.Query["type"].Count == 1 ? Request.Query["type"][0] : null;
        return Page(PageKeys.NetworkJoin, v,
            [("form", new LeadFormBlock(v.JoinForm(choices, type, new LText(l[MessageKeys.Ui.FormSubmitApplication].Value, false))))]);
    }
}

public class TvController(AppDbContext db, IStringLocalizer<SharedResource> l) : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        return Page(PageKeys.Tv, v, [("units", new CardsBlock(LText.Empty, LText.Empty, v.Cards(PageKeys.Tv, false), false))], v.TvAction());
    }

    [AcceptVerbs("GET", "HEAD")]
    public Task<IActionResult> Episodes() =>
        EntityListAsync(PageKeys.TvEpisodes,
            EcosystemQueries.EpisodesOrdered(db.TvEpisodes.AsNoTracking().Where(e => e.IsActive)).Select(EcosystemQueries.EpisodeCard),
            EpisodeCard, false, v => v.TvAction());

    /// <summary>
    /// GET /tv/episodes/{slug} (and /en/…): a published episode, its video (click-to-load: no request to YouTube
    /// before the click) and its published guests; schema.org TVEpisode (+ VideoObject only with a stored video URL).
    /// </summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Episode(string slug)
    {
        var ct = HttpContext.RequestAborted;
        var e = await db.TvEpisodes.AsNoTracking().Where(x => x.Slug == slug && x.IsActive)
            .Select(x => new
            {
                x.Id, x.Slug, x.Season, x.Number, x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.BodyAr, x.BodyEn, x.AiredOn, x.VideoUrl,
                x.MetaTitleAr, x.MetaTitleEn, x.MetaDescriptionAr, x.MetaDescriptionEn,
                Thumbnail = x.Thumbnail == null ? null : new MediaRef(x.Thumbnail.FileKey, x.Thumbnail.Format, x.Thumbnail.Width, x.Thumbnail.Height, x.Thumbnail.AltAr, x.Thumbnail.AltEn),
            })
            .FirstOrDefaultAsync(ct);
        if (e is null)
            return await OldSlugAsync(db, DetailKeys.TvEpisode, slug);

        var v = await ViewsAsync();
        var title = v.P(e.TitleAr, e.TitleEn);
        var summary = v.P(e.SummaryAr, e.SummaryEn);
        var available = EcosystemQueries.EpisodeHasEnglish(e.TitleEn, e.SummaryEn);
        var label = EpisodeLabel(e.Season, e.Number);
        var aired = Aired(v, e.AiredOn);
        var facts = new List<FactVm>();
        if (!aired.IsEmpty) facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.AiredOn), aired));

        var thumbnail = v.Image(e.Thumbnail, title);
        var youTubeId = YouTube.VideoId(e.VideoUrl);
        var video = youTubeId is not null
            ? new VideoVm(youTubeId, e.VideoUrl!.Trim(), thumbnail, title)
            : SiteUrl.IsHttpsUrl(e.VideoUrl) ? new VideoVm(null, e.VideoUrl!.Trim(), null, title) : null;

        var guests = (await db.TvEpisodeGuests.AsNoTracking()
                .Where(g => g.TvEpisodeId == e.Id && g.TvGuest!.IsActive)
                .OrderBy(g => g.TvGuest!.SortOrder).ThenBy(g => g.TvGuestId)
                .Select(g => g.TvGuest!)
                .Select(EcosystemQueries.PersonCard<TvGuest>())
                .ToListAsync(ct))
            .Select(g => PersonCard(v, g, DetailKeys.TvGuest)).ToList();

        var route = SiteRoutes.Detail(DetailKeys.TvEpisode);
        var baseUrl = BaseUrl(v);
        var node = new Dictionary<string, object> { ["@type"] = "TVEpisode" };
        SeoBuilder.Set(node, "name", title.Value);
        SeoBuilder.Set(node, "description", SeoBuilder.Shorten(summary.Value, 300));
        if (e.Number is { } number) node["episodeNumber"] = number;
        if (e.Season is { } season) node["partOfSeason"] = new Dictionary<string, object> { ["@type"] = "TVSeason", ["seasonNumber"] = season };
        if (e.AiredOn is { } on) node["datePublished"] = on.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        SeoBuilder.Set(node, "partOfSeries", SeoBuilder.TvSeriesRef(v, baseUrl));
        if (guests.Count > 0) node["actor"] = PersonRefs(baseUrl, guests);
        if (video is not null)
        {
            var videoNode = new Dictionary<string, object> { ["@type"] = "VideoObject", ["name"] = title.Value };
            SeoBuilder.Set(videoNode, "description", SeoBuilder.Shorten(summary.Value, 300));
            if (youTubeId is not null) videoNode["embedUrl"] = YouTube.EmbedUrl(youTubeId);
            else videoNode["url"] = video.LinkUrl!;
            if (thumbnail is not null && baseUrl is not null) videoNode["thumbnailUrl"] = baseUrl + thumbnail.Src;
            if (node.TryGetValue("datePublished", out var date)) videoNode["uploadDate"] = date;
            node["video"] = videoNode;
        }
        var metaDescription = v.P(e.MetaDescriptionAr, e.MetaDescriptionEn);
        var seo = SeoBuilder.BuildDetail(route, e.Slug, v, baseUrl, available, title, v.P(e.MetaTitleAr, e.MetaTitleEn).Value,
            metaDescription.IsEmpty ? summary.Value : metaDescription.Value, e.Thumbnail, video is null ? "website" : "video.episode", node);
        return Detail(v, route, e.Slug, seo, new DetailPageVm
        {
            Seo = seo,
            Kind = "episode",
            Title = title,
            Lead = summary,
            Body = SiteViews.Paragraphs(v.P(e.BodyAr, e.BodyEn)),
            // The YouTube player shows the thumbnail as its poster: not twice.
            Image = video?.YouTubeId is null ? thumbnail : null,
            Video = video,
            Badge = label,
            Facts = facts,
            Actions = Actions(v.TvAction()),
            Related = guests.Count == 0 ? [] : [new RelatedVm(v.Title(PageKeys.TvGuests), guests, true)],
            ArabicOnly = v.English && !available,
            Back = new LinkVm(v.Url(PageKeys.TvEpisodes), v.Title(PageKeys.TvEpisodes)),
        });
    }

    [AcceptVerbs("GET", "HEAD")]
    public Task<IActionResult> Guests() =>
        EntityListAsync(PageKeys.TvGuests,
            EcosystemQueries.PeopleOrdered(db.TvGuests.AsNoTracking().Where(g => g.IsActive)).Select(EcosystemQueries.PersonCard<TvGuest>()),
            (v, g) => PersonCard(v, g, DetailKeys.TvGuest), true, v => v.TvAction());

    /// <summary>GET /tv/guests/{slug} (and /en/…): a published guest and the published episodes they appear in.</summary>
    [AcceptVerbs("GET", "HEAD")]
    public Task<IActionResult> Guest(string slug) =>
        PersonPageAsync<TvGuest>(db, DetailKeys.TvGuest, slug, async (v, id) =>
        {
            var episodes = await EcosystemQueries.EpisodesOrdered(
                    db.TvEpisodes.AsNoTracking().Where(x => x.IsActive && db.TvEpisodeGuests.Any(g => g.TvEpisodeId == x.Id && g.TvGuestId == id)))
                .Select(EcosystemQueries.EpisodeCard).ToListAsync(HttpContext.RequestAborted);
            return episodes.Count == 0 ? null : new RelatedVm(v.Title(PageKeys.TvEpisodes), episodes.Select(x => EpisodeCard(v, x)).ToList(), false);
        }, v => Actions(v.TvAction()));

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Sponsorship()
    {
        var v = await ViewsAsync();
        var unit = Request.Query["unit"].Count == 1 ? Request.Query["unit"][0] : null;
        // The package select comes from the cached snapshot (the packages block above uses the same rows): 0 queries
        // when warm. The POST re-checks the chosen package against the database (ILeadReferences.PackagesAsync).
        var packages = v.Snapshot.Ecosystem;
        var form = v.SponsorshipForm(unit, packages.PackageChoices(Models.SponsorshipEnquiry.Forum),
            packages.PackageChoices(Models.SponsorshipEnquiry.Tv), new LText(l[MessageKeys.Ui.FormSubmitEnquiry].Value, false));
        // The TV packages (when published), then the enquiry form.
        return Page(PageKeys.TvSponsorship, v, [("packages", Packages(v, SponsorshipUnits.Tv)), ("form", new LeadFormBlock(form))],
            v.PageLink(PageKeys.Contact));
    }
}

public class NewsroomController(AppDbContext db) : PublicControllerBase
{
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Index()
    {
        var v = await ViewsAsync();
        if (Unpublished(v, PageKeys.News)) return NotFoundPage(v);
        var size = ContentKeys.PageSize(v.Snapshot.Content);
        var total = v.Snapshot.NewsCount.Total;
        if (PageNumber(total, size, out var redirect) is not { } n) return redirect ?? NotFoundPage(v);

        var rows = await db.News.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip((n - 1) * size).Take(size)
            .Select(x => new NewsRow(x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.CategoryAr, x.CategoryEn, x.NewsDateAr, x.NewsDateEn, x.LinkUrl,
                x.Slug, x.Image == null ? null : new MediaRef(x.Image.FileKey, x.Image.Format, x.Image.Width, x.Image.Height, x.Image.AltAr, x.Image.AltEn)))
            .ToListAsync(HttpContext.RequestAborted);
        return Page(PageKeys.News, v,
            [("list", rows.Count == 0 ? null : new NewsBlock(v.NewsCards(rows), ForumController.Pager(total, size, n, v.Url(PageKeys.News)), v.Content.Text("news_read_more")))],
            v.PageLink(PageKeys.Contact));
    }

    /// <summary>GET /news/{slug} (and /en/…): an active news item; an old slug 301s to the current one.</summary>
    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Item(string slug)
    {
        var n = await db.News.AsNoTracking().Where(x => x.Slug == slug && x.IsActive)
            .Select(x => new
            {
                x.Slug, x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.BodyAr, x.BodyEn, x.CategoryAr, x.CategoryEn,
                x.NewsDateAr, x.NewsDateEn, x.LinkUrl, x.PublishedOn, x.CreatedAt, x.UpdatedAt,
                x.MetaTitleAr, x.MetaTitleEn, x.MetaDescriptionAr, x.MetaDescriptionEn,
                Image = x.Image == null ? null : new MediaRef(x.Image.FileKey, x.Image.Format, x.Image.Width, x.Image.Height, x.Image.AltAr, x.Image.AltEn),
            })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (n is null)
            return await OldSlugAsync(db, DetailKeys.NewsItem, slug);

        var v = await ViewsAsync();
        var english = v.English;
        var title = v.P(n.TitleAr, n.TitleEn);
        var summary = v.P(n.SummaryAr, n.SummaryEn);
        var available = EventQueries.NewsHasEnglish(n.TitleEn, n.SummaryEn);
        var date = v.P(n.NewsDateAr, n.NewsDateEn);
        var facts = new List<FactVm>();
        if (!date.IsEmpty) facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.PublishedOn), date));
        else if (n.PublishedOn is { } on)
            facts.Add(new FactVm("calendar", Ui(MessageKeys.Ui.PublishedOn),
                new LText(EventDates.DateLine(new DateTimeOffset(on.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), null, english), false)));
        var actions = new List<LinkVm>();
        if (!string.IsNullOrWhiteSpace(n.LinkUrl)) actions.Add(new LinkVm(n.LinkUrl.Trim(), Ui(MessageKeys.Ui.NewsSource)));

        var route = SiteRoutes.Detail(DetailKeys.NewsItem);
        var baseUrl = BaseUrl(v);
        var metaDescription = v.P(n.MetaDescriptionAr, n.MetaDescriptionEn);
        var headline = title.Value.Length <= 110 ? title.Value : SeoBuilder.Shorten(title.Value, 110);
        var published = n.PublishedOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                        ?? TimeDisplay.Iso(n.CreatedAt);
        var node = new Dictionary<string, object> { ["@type"] = "NewsArticle", ["headline"] = headline };
        SeoBuilder.Set(node, "description", SeoBuilder.Shorten(summary.Value, 300));
        node["datePublished"] = published;
        node["dateModified"] = TimeDisplay.Iso(n.UpdatedAt);
        SeoBuilder.Set(node, "author", SeoBuilder.OrganizationRef(baseUrl));
        SeoBuilder.Set(node, "publisher", SeoBuilder.OrganizationRef(baseUrl));
        if (baseUrl is not null)
            node["mainEntityOfPage"] = baseUrl + CultureUrls.Detail(route.Key, n.Slug, english && available);
        var seo = SeoBuilder.BuildDetail(route, n.Slug, v, baseUrl, available, title, v.P(n.MetaTitleAr, n.MetaTitleEn).Value,
            metaDescription.IsEmpty ? summary.Value : metaDescription.Value, n.Image, "article", node);
        return Detail(v, route, n.Slug, seo, new DetailPageVm
        {
            Seo = seo,
            Kind = "news",
            Title = title,
            Lead = summary,
            Body = SiteViews.Paragraphs(v.P(n.BodyAr, n.BodyEn)),
            Image = v.Image(n.Image, title),
            Badge = v.P(n.CategoryAr, n.CategoryEn),
            Facts = facts,
            Actions = actions,
            ArabicOnly = english && !available,
            Back = new LinkVm(v.Url(PageKeys.News), v.Title(PageKeys.News)),
        });
    }
}
