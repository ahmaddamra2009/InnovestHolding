using System.Globalization;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers;

// The ecosystem lists and profile pages shared by the Forum and TV controllers: cards for the lists, the profile page
// of a person (speaker, guest), the related cards of a detail page. Every text is a database value for the page
// language (Arabic fallback) or a resx label.
public abstract partial class PublicControllerBase
{
    private static string N(int n) => n.ToString(CultureInfo.InvariantCulture);

    protected EntityCardVm PersonCard(SiteViews v, PersonRow p, string detailKey)
    {
        var name = v.P(p.NameAr, p.NameEn);
        return new EntityCardVm(name, CultureUrls.Detail(detailKey, p.Slug, v.English), v.Image(p.Photo, name), LText.Empty,
            v.P(p.JobTitleAr, p.JobTitleEn), v.P(p.OrganizationAr, p.OrganizationEn), []);
    }

    /// <summary>"Season 2 · Episode 5", "Episode 5", "Season 2" or nothing.</summary>
    protected LText EpisodeLabel(int? season, int? number) => (season, number) switch
    {
        ({ } s, { } n) => Ui(MessageKeys.Ui.SeasonEpisode, N(s), N(n)),
        (null, { } n) => Ui(MessageKeys.Ui.EpisodeNumber, N(n)),
        ({ } s, null) => Ui(MessageKeys.Ui.SeasonNumber, N(s)),
        _ => LText.Empty,
    };

    protected static LText Aired(SiteViews v, DateOnly? date) =>
        date is { } d ? new LText(EventDates.Date(d, v.English), false) : LText.Empty;

    protected EntityCardVm EpisodeCard(SiteViews v, EpisodeRow e)
    {
        var title = v.P(e.TitleAr, e.TitleEn);
        return new EntityCardVm(title, CultureUrls.Detail(DetailKeys.TvEpisode, e.Slug, v.English), v.Image(e.Thumbnail, title),
            EpisodeLabel(e.Season, e.Number), Aired(v, e.AiredOn), v.P(e.SummaryAr, e.SummaryEn), []);
    }

    protected EntityCardVm OpportunityCard(SiteViews v, OpportunityRow o)
    {
        var title = v.P(o.TitleAr, o.TitleEn);
        var chips = new[] { v.P(o.StageTextAr, o.StageTextEn), v.P(o.AskTextAr, o.AskTextEn) }.Where(c => !c.IsEmpty).ToList();
        return new EntityCardVm(title, CultureUrls.Detail(DetailKeys.NetworkOpportunity, o.Slug, v.English), v.Image(o.Image, title),
            v.P(o.SectorAr, o.SectorEn), LText.Empty, v.P(o.SummaryAr, o.SummaryEn), chips,
            Ui(o.IsOpen ? MessageKeys.Ui.OpportunityOpen : MessageKeys.Ui.OpportunityClosed));
    }

    protected IReadOnlyList<EntityCardVm> EventCards(SiteViews v, IEnumerable<EventRow> rows, DateTimeOffset now) =>
        v.EventCards(rows, now).Select(e => new EntityCardVm(e.Title, e.Href, e.Image,
            new LText(N(e.Day) + " " + e.Month.Value, e.Month.IsFallback), e.Location, LText.Empty, [],
            e.Status == EventStatus.Cancelled ? Ui(MessageKeys.Ui.EventCancelled) : e.Past ? Ui(MessageKeys.Ui.EventPast) : LText.Empty)).ToList();

    /// <summary>The active packages of a unit, or null when it has none.</summary>
    protected PackagesBlock? Packages(SiteViews v, string unit)
    {
        var rows = v.Snapshot.Ecosystem.PackagesOf(unit);
        if (rows.Count == 0) return null;
        return new PackagesBlock(Ui(MessageKeys.Ui.SponsorshipPackages),
            rows.Select(p => new PackageVm(v.P(p.TitleAr, p.TitleEn), v.P(p.PriceTextAr, p.PriceTextEn),
                SiteViews.Lines(v.P(p.BenefitsAr, p.BenefitsEn)))).ToList());
    }

    /// <summary>
    /// A list page of the ecosystem: the requested page of <paramref name="rows"/> (ordered, published) as cards, the
    /// pager, and the admin's empty text with <paramref name="emptyAction"/> while there is none; then the optional
    /// <paramref name="after"/> block (e.g. a lead form).
    /// </summary>
    protected async Task<IActionResult> EntityListAsync<T>(string key, IQueryable<T> rows, Func<SiteViews, T, EntityCardVm> card,
        bool people, Func<SiteViews, LinkVm> emptyAction, Func<SiteViews, BlockVm?>? after = null)
    {
        var v = await ViewsAsync();
        var size = ContentKeys.PageSize(v.Snapshot.Content);
        var total = SeoPolicy.ListCountOf(key, v.Snapshot).Total;
        if (PageNumber(total, size, out var redirect) is not { } n) return redirect ?? await NotFoundPageAsync();
        var page = await rows.Skip((n - 1) * size).Take(size).ToListAsync(HttpContext.RequestAborted);
        var cards = page.Select(r => card(v, r)).ToList();
        return Page(key, v,
            [cards.Count == 0 ? null : new EntityGridBlock(key, cards, ForumController.Pager(total, size, n, v.Url(key)), people), after?.Invoke(v)],
            emptyAction(v));
    }

    /// <summary>
    /// The profile page of a published speaker or guest (an old slug 301s to the current one): role and organisation,
    /// portrait, bio, profiles elsewhere, the related cards; schema.org Person.
    /// </summary>
    protected async Task<IActionResult> PersonPageAsync<T>(AppDbContext db, string detailKey, string slug,
        Func<SiteViews, int, Task<RelatedVm?>> related, Func<SiteViews, IReadOnlyList<LinkVm>> actions) where T : PersonProfile
    {
        var ct = HttpContext.RequestAborted;
        var p = await db.Set<T>().AsNoTracking().Where(x => x.Slug == slug && x.IsActive)
            .Select(x => new
            {
                x.Id, x.Slug, x.NameAr, x.NameEn, x.JobTitleAr, x.JobTitleEn, x.OrganizationAr, x.OrganizationEn, x.BioAr, x.BioEn,
                x.LinkedInUrl, x.XUrl, x.MetaTitleAr, x.MetaTitleEn, x.MetaDescriptionAr, x.MetaDescriptionEn,
                Photo = x.Photo == null ? null : new MediaRef(x.Photo.FileKey, x.Photo.Format, x.Photo.Width, x.Photo.Height, x.Photo.AltAr, x.Photo.AltEn),
            })
            .FirstOrDefaultAsync(ct);
        if (p is null)
            return await OldSlugAsync(db, detailKey, slug);

        var v = await ViewsAsync();
        var name = v.P(p.NameAr, p.NameEn);
        var job = v.P(p.JobTitleAr, p.JobTitleEn);
        var org = v.P(p.OrganizationAr, p.OrganizationEn);
        var bio = v.P(p.BioAr, p.BioEn);
        var available = EcosystemQueries.PersonHasEnglish(p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn);

        var facts = new List<FactVm>();
        if (!job.IsEmpty) facts.Add(new FactVm("briefcase", Ui(MessageKeys.Ui.JobTitle), job));
        if (!org.IsEmpty) facts.Add(new FactVm("building", Ui(MessageKeys.Ui.Organization), org));
        var profiles = new List<SocialVm>();
        if (SiteUrl.IsHttpsUrl(p.LinkedInUrl)) profiles.Add(new SocialVm("linkedin", p.LinkedInUrl!.Trim()));
        if (SiteUrl.IsHttpsUrl(p.XUrl)) profiles.Add(new SocialVm("twitter", p.XUrl!.Trim()));

        var route = SiteRoutes.Detail(detailKey);
        var baseUrl = BaseUrl(v);
        var role = string.Join(" — ", new[] { job, org }.Where(t => !t.IsEmpty).Select(t => t.Value));
        var node = new Dictionary<string, object> { ["@type"] = "Person" };
        SeoBuilder.Set(node, "name", name.Value);
        SeoBuilder.Set(node, "jobTitle", job.Value);
        if (!org.IsEmpty) node["worksFor"] = new Dictionary<string, object> { ["@type"] = "Organization", ["name"] = org.Value };
        SeoBuilder.Set(node, "description", SeoBuilder.Shorten(bio.IsEmpty ? role : bio.Value, 300));
        SeoBuilder.Set(node, "sameAs", profiles.Select(x => x.Url).ToArray());
        var metaDescription = v.P(p.MetaDescriptionAr, p.MetaDescriptionEn);
        // Indexed only with a description (bio or meta description) in Arabic, and the English page also in English.
        var describedAr = EcosystemQueries.PersonHasDescription(p.BioAr, p.MetaDescriptionAr);
        var describedEn = available && EcosystemQueries.PersonHasDescription(p.BioEn, p.MetaDescriptionEn);
        var seo = SeoBuilder.BuildDetail(route, p.Slug, v, baseUrl, describedEn, name, v.P(p.MetaTitleAr, p.MetaTitleEn).Value,
            !metaDescription.IsEmpty ? metaDescription.Value : role.Length > 0 ? role : bio.Value, p.Photo, "profile", node,
            indexable: describedAr);

        var rel = await related(v, p.Id);
        return Detail(v, route, p.Slug, seo, new DetailPageVm
        {
            Seo = seo,
            Kind = "person",
            Title = name,
            Lead = LText.Empty,
            Body = SiteViews.Paragraphs(bio),
            Image = v.Image(p.Photo, name),
            Portrait = true,
            Facts = facts,
            Profiles = profiles,
            Actions = actions(v),
            Related = rel is null ? [] : [rel],
            ArabicOnly = v.English && !available,
            Back = new LinkVm(v.Url(route.Parent), v.Title(route.Parent)),
        });
    }

    /// <summary>schema.org Person references (name + profile URL) for an event's performers or an episode's actors.</summary>
    protected static List<Dictionary<string, object>> PersonRefs(string? baseUrl, IEnumerable<EntityCardVm> people) =>
        people.Select(c =>
        {
            var node = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = c.Title.Value };
            if (baseUrl is not null) node["url"] = baseUrl + c.Href;
            return node;
        }).ToList();
}
