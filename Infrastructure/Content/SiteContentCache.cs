using InnovestHolding.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InnovestHolding.Infrastructure.Content;

// Immutable, culture-independent rows of everything the shell (header, footer, breadcrumbs) and the static pages show,
// in both languages. Never EF entities: the snapshot is shared across requests and threads.
public sealed record TextPair(string Ar, string? En);
/// <summary>An active extra menu link. Header/drawer: <see cref="ShowInHeader"/>; the automatic footer lists every one.</summary>
public sealed record NavRow(int Id, string TitleAr, string? TitleEn, string Href, bool ShowInHeader);
/// <summary>An uploaded image as the pages need it (Infrastructure/Media).</summary>
public sealed record MediaRef(string FileKey, string Format, int Width, int Height, string AltAr, string? AltEn)
{
    public string Url => Media.MediaStore.Url(FileKey, Format);
}
public sealed record PageRow(string Key, string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn, string IntroAr, string? IntroEn,
    string BodyAr, string? BodyEn, string EmptyTextAr, string? EmptyTextEn, string MetaTitleAr, string? MetaTitleEn,
    string MetaDescriptionAr, string? MetaDescriptionEn, bool ShowInMenu, int SortOrder, DateTime UpdatedAt, MediaRef? Image = null,
    bool IsPublished = true, bool ShowInFooter = true, string? EmptyActionPage = null);
public sealed record ItemRow(string TextAr, string? TextEn, string? DescriptionAr, string? DescriptionEn, string? IconKey);
public sealed record PlatformRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn,
    string? ShortTitleAr, string? ShortTitleEn, string? LinkUrl, bool ShowInFooter, string IconSvg, string? IconKey = null);
public sealed record StatRow(string LabelAr, string? LabelEn, string? ShortLabelAr, string? ShortLabelEn, int Value, string Suffix, string IconSvg);
public sealed record NewsRow(string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn, string CategoryAr, string? CategoryEn,
    string NewsDateAr, string? NewsDateEn, string? LinkUrl, string Slug, MediaRef? Image);
public sealed record EventRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn, int EventDay,
    string EventMonthAr, string? EventMonthEn, string LocationAr, string? LocationEn, string TimeAr, string? TimeEn,
    string Slug, string SummaryAr, string? SummaryEn, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Models.EventStatus Status, MediaRef? Image,
    bool DateIsMonth);
public sealed record PartnerRow(string NameAr, string? NameEn, MediaRef? Logo, string? Url, int? GroupId, bool ShowOnHome);
/// <param name="ShowOnForumHub">F10: the group is listed in the forum hub's patronage strip.</param>
public sealed record PartnerGroupRow(int Id, string NameAr, string? NameEn, bool ShowOnForumHub = false);
/// <summary>F10: an active past edition of the forum with its photos in order (the first is the cover).</summary>
public sealed record EditionRow(int Id, short Year, string LabelAr, string? LabelEn, string TitleAr, string? TitleEn, string PlaceAr,
    string? PlaceEn, string TextAr, string? TextEn, DateTime UpdatedAt, IReadOnlyList<MediaRef> Photos);
public sealed record FaqRow(string QuestionAr, string? QuestionEn, string AnswerAr, string? AnswerEn);
public sealed record InterestRow(int Id, string NameAr, string? NameEn);
/// <summary>An active link with a URL. <see cref="Label"/>/<see cref="IconKey"/> are set on "custom" rows only.</summary>
public sealed record SocialRow(string Network, string Url, string? Label = null, string? IconKey = null);
/// <summary>The admin's switch and order of one section of a page (Models/PageSection).</summary>
public sealed record SectionRow(string Key, bool IsVisible, int SortOrder);
/// <summary>A link of a customised footer column: a page key OR an extra menu link id.</summary>
public sealed record FooterLinkRow(string? PageKey, int? NavItemId);
/// <summary>A customised footer column, links in order. No row at all = the automatic footer.</summary>
public sealed record FooterColumnRow(int Id, string TitleAr, string? TitleEn, IReadOnlyList<FooterLinkRow> Links);

/// <summary>How many active rows a list page has, overall and with the English text its cards need.</summary>
public sealed record ListCount(int Total, int English);

/// <summary>
/// The published speakers, guests, episodes and opportunities (counted, never loaded), the active packages, and the
/// published opportunities open for interest (id + title only: the interest form's select).
/// </summary>
public sealed record EcosystemData(ListCount Speakers, ListCount Guests, ListCount Episodes, ListCount Opportunities,
    IReadOnlyList<PackageRow> Packages, IReadOnlyList<Forms.FormChoice>? OpenOpportunities = null)
{
    public static readonly EcosystemData None = new(new(0, 0), new(0, 0), new(0, 0), new(0, 0), []);

    public IReadOnlyList<PackageRow> PackagesOf(string unit) => Packages.Where(p => p.Unit == unit).ToList();

    /// <summary>The active packages of a unit as form choices, in display order (the GET of the enquiry form).</summary>
    public IReadOnlyList<Forms.FormChoice> PackageChoices(string unit) =>
        Packages.Where(p => p.Unit == unit).Select(p => new Forms.FormChoice(p.Id, p.TitleAr, p.TitleEn)).ToList();

    /// <summary>IsActive AND IsOpen opportunities in the public list's order (the GET of the interest form).</summary>
    public IReadOnlyList<Forms.FormChoice> OpenOpportunityChoices => OpenOpportunities ?? [];
}

public sealed record SiteSnapshot(
    IReadOnlyDictionary<string, TextPair> Content,
    IReadOnlyDictionary<string, PageRow> Pages,
    IReadOnlyDictionary<string, IReadOnlyList<ItemRow>> Items,
    IReadOnlyList<NavRow> Nav,
    IReadOnlyList<PlatformRow> Platforms,
    IReadOnlyList<StatRow> Stats,
    IReadOnlyList<NewsRow> News,
    IReadOnlyList<EventRow> Events,
    IReadOnlyList<PartnerRow> Partners,
    IReadOnlyList<FaqRow> Faqs,
    IReadOnlyList<InterestRow> Interests,
    IReadOnlyList<TextPair> Benefits,
    IReadOnlyList<SocialRow> Socials,
    ListCount NewsCount,
    ListCount EventsCount,
    ListCount PartnersCount,
    IReadOnlyList<PartnerGroupRow> PartnerGroups,
    MediaRef? FounderPhoto = null,
    EcosystemData? EcosystemData = null,
    IReadOnlyDictionary<string, MediaRef>? Images = null,
    IReadOnlyDictionary<string, IReadOnlyList<SectionRow>>? SectionData = null,
    IReadOnlyList<FooterColumnRow>? FooterData = null,
    IReadOnlyList<EditionRow>? EditionData = null,
    ListCount? EditionsCountData = null)
{
    /// <summary>F10: the active past editions in public order, with their photos.</summary>
    public IReadOnlyList<EditionRow> Editions => EditionData ?? [];

    /// <summary>F10: active editions, and those whose English content exists (<see cref="ForumEditionQueries.HasEnglish"/>).</summary>
    public ListCount EditionsCount => EditionsCountData ?? new ListCount(0, 0);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<SectionRow>> NoSections =
        new Dictionary<string, IReadOnlyList<SectionRow>>(StringComparer.Ordinal);

    /// <summary>Page key → its stored section rows, in stored order (SortOrder, Id). A page without rows is absent.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SectionRow>> Sections => SectionData ?? NoSections;

    /// <summary>The customised footer columns in order; empty = the automatic footer.</summary>
    public IReadOnlyList<FooterColumnRow> Footer => FooterData ?? [];

    /// <summary>The stored section rows of a page (empty when none).</summary>
    public IReadOnlyList<SectionRow> SectionsOf(string pageKey) => Sections.GetValueOrDefault(pageKey) ?? [];

    /// <summary>
    /// The page and every ancestor in the route table are published (a page without a Pages row counts as published;
    /// the home page always is). Unknown keys are not published.
    /// </summary>
    public bool IsPublished(string key)
    {
        if (!Site.SiteRoutes.IsKey(key)) return false;
        return Site.SiteRoutes.Chain(key).All(p => p.Key == Site.PageKeys.Home || (Page(p.Key)?.IsPublished ?? true));
    }

    /// <summary>Counts of the ecosystem lists and the sponsorship packages (empty when not loaded).</summary>
    public EcosystemData Ecosystem => EcosystemData ?? EcosystemData.None;

    /// <summary>An uploaded site image (<see cref="SiteImages"/>), or null while none is uploaded (the caller falls back).</summary>
    public MediaRef? Image(string key) => Images?.GetValueOrDefault(key);

    public IReadOnlyList<ItemRow> ItemsOf(string group) => Items.GetValueOrDefault(group) ?? [];

    public PageRow? Page(string key) => Pages.GetValueOrDefault(key);
}

public interface ISiteContentCache
{
    Task<SiteSnapshot> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the snapshot; the next request reloads it from the database.</summary>
    void Invalidate();
}

/// <summary>
/// One cached snapshot of the site's content (singleton over IMemoryCache). A cache hit costs 0 queries — the home page,
/// every static page and the shell of every page are served from it; a miss runs the projections below on one context,
/// behind a semaphore so an expiry never causes a stampede. Invalidated on every admin save by
/// <see cref="ContentCacheInvalidationInterceptor"/>; the absolute expiry is only a safety net for edits made outside
/// the app (e.g. SQL run on the server).
/// </summary>
public sealed class SiteContentCache(IMemoryCache cache, IServiceScopeFactory scopes, TimeProvider time) : ISiteContentCache
{
    private const string CacheKey = "site-snapshot";
    public static readonly TimeSpan SafetyExpiry = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _generation;

    public async Task<SiteSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteSnapshot? hit) && hit is not null) return hit;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(CacheKey, out hit) && hit is not null) return hit;
            var generation = Interlocked.Read(ref _generation);
            var snapshot = await LoadAsync(cancellationToken);
            // An invalidation that happened while loading means this snapshot may already be stale: serve it to
            // this request but do not cache it.
            if (generation == Interlocked.Read(ref _generation))
                cache.Set(CacheKey, snapshot, SafetyExpiry);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        cache.Remove(CacheKey);
    }

    private static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);

    private async Task<SiteSnapshot> LoadAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var content = await db.SiteContents.AsNoTracking()
            .Select(c => new { c.SectionKey, c.ContentAr, c.ContentEn })
            .ToDictionaryAsync(c => c.SectionKey, c => new TextPair(c.ContentAr, c.ContentEn), ct);

        var pages = await db.Pages.AsNoTracking()
            .Select(p => new PageRow(p.Key, p.TitleAr, p.TitleEn, p.SummaryAr, p.SummaryEn, p.IntroAr, p.IntroEn, p.BodyAr, p.BodyEn,
                p.EmptyTextAr, p.EmptyTextEn, p.MetaTitleAr, p.MetaTitleEn, p.MetaDescriptionAr, p.MetaDescriptionEn,
                p.ShowInMenu, p.SortOrder, p.UpdatedAt,
                p.Image == null ? null : new MediaRef(p.Image.FileKey, p.Image.Format, p.Image.Width, p.Image.Height, p.Image.AltAr, p.Image.AltEn),
                p.IsPublished, p.ShowInFooter, p.EmptyActionPage))
            .ToDictionaryAsync(p => p.Key, StringComparer.Ordinal, ct);

        var itemRows = await db.ContentItems.AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Group).ThenBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new { i.Group, i.TextAr, i.TextEn, i.DescriptionAr, i.DescriptionEn, i.IconKey })
            .ToListAsync(ct);
        var items = itemRows
            .GroupBy(i => i.Group, StringComparer.Ordinal)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyList<ItemRow>)g.Select(x => new ItemRow(x.TextAr, x.TextEn, x.DescriptionAr, x.DescriptionEn, x.IconKey)).ToList(),
                StringComparer.Ordinal);

        var nav = await db.NavItems.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
            .Select(n => new NavRow(n.Id, n.TitleAr, n.TitleEn, n.Href, n.ShowInHeader))
            .ToListAsync(ct);

        var platforms = await db.Platforms.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new PlatformRow(p.TitleAr, p.TitleEn, p.DescriptionAr, p.DescriptionEn, p.ShortTitleAr,
                p.ShortTitleEn, p.LinkUrl, p.ShowInFooter, p.IconSvg, p.IconKey))
            .ToListAsync(ct);

        var stats = await db.Stats.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new StatRow(s.LabelAr, s.LabelEn, s.ShortLabelAr, s.ShortLabelEn, s.Value, s.Suffix, s.IconSvg))
            .ToListAsync(ct);

        // Bounded: the admin-set number of home-page cards (one small read of a value already loaded above).
        var newsLimit = ContentKeys.ParseCount(content.GetValueOrDefault(ContentKeys.HomeNewsLimit)?.Ar, ContentKeys.HomeNewsLimitFallback);
        var news = await db.News.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
            .Take(newsLimit)
            .Select(n => new NewsRow(n.TitleAr, n.TitleEn, n.SummaryAr, n.SummaryEn, n.CategoryAr, n.CategoryEn,
                n.NewsDateAr, n.NewsDateEn, n.LinkUrl, n.Slug,
                n.Image == null ? null : new MediaRef(n.Image.FileKey, n.Image.Format, n.Image.Width, n.Image.Height, n.Image.AltAr, n.Image.AltEn)))
            .ToListAsync(ct);

        // News can grow: counted in the database, not loaded.
        var newsTotal = await db.News.CountAsync(n => n.IsActive, ct);
        var newsEnglish = await db.News.CountAsync(n => n.IsActive && n.TitleEn != null && n.TitleEn != ""
                                                        && n.SummaryEn != null && n.SummaryEn != "", ct);

        // Upcoming first (by date, undated ones first in their admin order), then past ones (most recent first).
        var events = await EventQueries.Ordered(db.Events.AsNoTracking().Where(e => e.IsActive), time.GetUtcNow())
            .Select(EventQueries.Row)
            .ToListAsync(ct);

        var partners = await db.Partners.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new PartnerRow(p.NameAr, p.NameEn,
                p.Logo == null ? null : new MediaRef(p.Logo.FileKey, p.Logo.Format, p.Logo.Width, p.Logo.Height, p.Logo.AltAr, p.Logo.AltEn),
                p.Url, p.PartnerGroupId, p.ShowOnHome))
            .ToListAsync(ct);

        // The site images (logos, hero, founder photo): each key holds the FileKey of an uploaded image; one small read
        // for those that are set, none when no image is uploaded.
        var imageKeys = SiteImages.All
            .Select(i => (i.Key, FileKey: content.GetValueOrDefault(i.Key)?.Ar))
            .Where(x => !string.IsNullOrEmpty(x.FileKey)).ToList();
        var fileKeys = imageKeys.Select(x => x.FileKey!).Distinct().ToList();
        var files = fileKeys.Count == 0
            ? new Dictionary<string, MediaRef>()
            : await db.MediaFiles.AsNoTracking().Where(m => fileKeys.Contains(m.FileKey))
                .Select(m => new MediaRef(m.FileKey, m.Format, m.Width, m.Height, m.AltAr, m.AltEn))
                .ToDictionaryAsync(m => m.FileKey, StringComparer.Ordinal, ct);
        var images = imageKeys.Where(x => files.ContainsKey(x.FileKey!))
            .ToDictionary(x => x.Key, x => files[x.FileKey!], StringComparer.Ordinal);
        var founderPhoto = images.GetValueOrDefault(ContentKeys.FounderPhoto);

        var partnerGroups = await db.PartnerGroups.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .Select(g => new PartnerGroupRow(g.Id, g.NameAr, g.NameEn, g.ShowOnForumHub))
            .ToListAsync(ct);

        var faqs = await db.Faqs.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Id)
            .Select(f => new FaqRow(f.QuestionAr, f.QuestionEn, f.AnswerAr, f.AnswerEn))
            .ToListAsync(ct);

        var interests = await db.InterestOptions.AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
            .Select(o => new InterestRow(o.Id, o.NameAr, o.NameEn))
            .ToListAsync(ct);

        var benefits = await db.RegisterBenefits.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Id)
            .Select(b => new TextPair(b.TextAr, b.TextEn))
            .ToListAsync(ct);

        var socials = await db.SocialLinks.AsNoTracking()
            .Where(s => s.IsActive && s.Url != null && s.Url != "")
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new SocialRow(s.Network, s.Url!, s.Label, s.IconKey))
            .ToListAsync(ct);

        // ~75 rows: loaded whole, grouped per page in stored order.
        var sectionRows = await db.PageSections.AsNoTracking()
            .OrderBy(x => x.PageKey).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new { x.PageKey, x.SectionKey, x.IsVisible, x.SortOrder })
            .ToListAsync(ct);
        var sections = sectionRows
            .GroupBy(x => x.PageKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyList<SectionRow>)g.Select(x => new SectionRow(x.SectionKey, x.IsVisible, x.SortOrder)).ToList(),
                StringComparer.Ordinal);

        // Customised footer (at most 4 columns × 8 links); no column on a database the admin has not customised, and
        // then no second query.
        var columns = await db.FooterColumns.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new { c.Id, c.TitleAr, c.TitleEn })
            .ToListAsync(ct);
        var footerLinks = columns.Count == 0
            ? []
            : await db.FooterLinks.AsNoTracking()
                .OrderBy(l => l.FooterColumnId).ThenBy(l => l.SortOrder).ThenBy(l => l.Id)
                .Select(l => new { l.FooterColumnId, l.PageKey, l.NavItemId })
                .ToListAsync(ct);
        var footer = columns
            .Select(c => new FooterColumnRow(c.Id, c.TitleAr, c.TitleEn,
                footerLinks.Where(l => l.FooterColumnId == c.Id).Select(l => new FooterLinkRow(l.PageKey, l.NavItemId)).ToList()))
            .ToList();

        var ecosystem = await LoadEcosystemAsync(db, ct);

        // F10: the past editions (a short admin list, ~6 rows and their photos): one query; the cover is the first photo.
        var editions = await ForumEditionQueries.Ordered(db.ForumEditions.AsNoTracking().Where(e => e.IsActive))
            .Select(e => new EditionRow(e.Id, e.Year, e.EditionLabelAr, e.EditionLabelEn, e.TitleAr, e.TitleEn, e.PlaceAr, e.PlaceEn,
                e.TextAr, e.TextEn, e.UpdatedAt,
                e.Photos.OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                    .Select(p => new MediaRef(p.Image!.FileKey, p.Image.Format, p.Image.Width, p.Image.Height, p.Image.AltAr, p.Image.AltEn))
                    .ToList()))
            .ToListAsync(ct);

        return new SiteSnapshot(content, pages, items, nav, platforms, stats, news, events, partners, faqs, interests, benefits, socials,
            new ListCount(newsTotal, newsEnglish),
            new ListCount(events.Count, events.Count(e => EventQueries.HasEnglish(e.TitleEn, e.SummaryEn, e.DescriptionEn))),
            new ListCount(partners.Count, partners.Count(p => Has(p.NameEn))),
            partnerGroups,
            founderPhoto,
            ecosystem,
            images,
            sections,
            footer,
            editions,
            // English = ForumEditionQueries.HasEnglish (an English title AND an English text), on the loaded rows.
            new ListCount(editions.Count, editions.Count(e => !string.IsNullOrEmpty(e.TitleEn) && !string.IsNullOrEmpty(e.TextEn))));
    }

    /// <summary>
    /// The four ecosystem lists can grow: they are counted in the database (published rows, and those whose English page
    /// is complete — <see cref="EcosystemQueries"/>), never loaded. The packages are a short admin list, loaded.
    /// </summary>
    private static async Task<EcosystemData> LoadEcosystemAsync(AppDbContext db, CancellationToken ct)
    {
        async Task<ListCount> CountAsync<T>(IQueryable<T> published, System.Linq.Expressions.Expression<Func<T, bool>> english) =>
            new(await published.CountAsync(ct), await published.CountAsync(english, ct));

        var speakers = await CountAsync(db.Speakers.Where(p => p.IsActive), EcosystemQueries.PersonEnglish<Models.Speaker>());
        var guests = await CountAsync(db.TvGuests.Where(p => p.IsActive), EcosystemQueries.PersonEnglish<Models.TvGuest>());
        var episodes = await CountAsync(db.TvEpisodes.Where(e => e.IsActive), EcosystemQueries.EpisodeEnglish);
        var opportunities = await CountAsync(db.InvestmentOpportunities.Where(o => o.IsActive), EcosystemQueries.OpportunityEnglish);
        var packages = await db.SponsorshipPackages.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new PackageRow(p.Id, p.Unit, p.TitleAr, p.TitleEn, p.PriceTextAr, p.PriceTextEn, p.BenefitsAr, p.BenefitsEn))
            .ToListAsync(ct);
        // The interest form offers every open, published opportunity (the select is not paged like the list), so its
        // short id + title rows are cached here; an admin save on InvestmentOpportunities drops the snapshot.
        var open = await EcosystemQueries.OpportunitiesOrdered(db.InvestmentOpportunities.AsNoTracking().Where(o => o.IsActive && o.IsOpen))
            .Select(o => new Forms.FormChoice(o.Id, o.TitleAr, o.TitleEn))
            .ToListAsync(ct);
        return new EcosystemData(speakers, guests, episodes, opportunities, packages, open);
    }
}
