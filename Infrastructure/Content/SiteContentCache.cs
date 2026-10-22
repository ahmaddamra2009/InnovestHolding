using InnovestHolding.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InnovestHolding.Infrastructure.Content;

// Immutable, culture-independent rows of everything the shell (header, footer, breadcrumbs) and the static pages show,
// in both languages. Never EF entities: the snapshot is shared across requests and threads.
public sealed record TextPair(string Ar, string? En);
public sealed record NavSubRow(string TitleAr, string? TitleEn, string Href, string? IconSvg);
public sealed record NavRow(string TitleAr, string? TitleEn, string Href, bool HasDropdown, bool ShowInFooter, IReadOnlyList<NavSubRow> SubItems);
/// <summary>An uploaded image as the pages need it (Infrastructure/Media).</summary>
public sealed record MediaRef(string FileKey, string Format, int Width, int Height, string AltAr, string? AltEn)
{
    public string Url => Media.MediaStore.Url(FileKey, Format);
}
public sealed record PageRow(string Key, string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn, string IntroAr, string? IntroEn,
    string BodyAr, string? BodyEn, string EmptyTextAr, string? EmptyTextEn, string MetaTitleAr, string? MetaTitleEn,
    string MetaDescriptionAr, string? MetaDescriptionEn, bool ShowInMenu, int SortOrder, DateTime UpdatedAt, MediaRef? Image = null);
public sealed record ItemRow(string TextAr, string? TextEn, string? DescriptionAr, string? DescriptionEn, string? IconKey);
public sealed record PlatformRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn,
    string? ShortTitleAr, string? ShortTitleEn, string? LinkUrl, bool ShowInFooter, string IconSvg);
public sealed record StatRow(string LabelAr, string? LabelEn, string? ShortLabelAr, string? ShortLabelEn, int Value, string Suffix, string IconSvg);
public sealed record NewsRow(string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn, string CategoryAr, string? CategoryEn,
    string NewsDateAr, string? NewsDateEn, string? LinkUrl, string Slug, MediaRef? Image);
public sealed record EventRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn, int EventDay,
    string EventMonthAr, string? EventMonthEn, string LocationAr, string? LocationEn, string TimeAr, string? TimeEn,
    string Slug, string SummaryAr, string? SummaryEn, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Models.EventStatus Status, MediaRef? Image);
public sealed record PartnerRow(string NameAr, string? NameEn, MediaRef? Logo, string? Url, int? GroupId, bool ShowOnHome);
public sealed record PartnerGroupRow(int Id, string NameAr, string? NameEn);
public sealed record FaqRow(string QuestionAr, string? QuestionEn, string AnswerAr, string? AnswerEn);
public sealed record InterestRow(int Id, string NameAr, string? NameEn);
public sealed record SocialRow(string Network, string Url);

/// <summary>How many active rows a list page has, overall and with the English text its cards need.</summary>
public sealed record ListCount(int Total, int English);

/// <summary>The published speakers, guests, episodes and opportunities (counted, never loaded) and the active packages.</summary>
public sealed record EcosystemData(ListCount Speakers, ListCount Guests, ListCount Episodes, ListCount Opportunities,
    IReadOnlyList<PackageRow> Packages)
{
    public static readonly EcosystemData None = new(new(0, 0), new(0, 0), new(0, 0), new(0, 0), []);

    public IReadOnlyList<PackageRow> PackagesOf(string unit) => Packages.Where(p => p.Unit == unit).ToList();
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
    IReadOnlyDictionary<string, MediaRef>? Images = null)
{
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
                p.Image == null ? null : new MediaRef(p.Image.FileKey, p.Image.Format, p.Image.Width, p.Image.Height, p.Image.AltAr, p.Image.AltEn)))
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
            .Select(n => new NavRow(n.TitleAr, n.TitleEn, n.Href, n.HasDropdown, n.ShowInFooter,
                n.SubItems.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                    .Select(s => new NavSubRow(s.TitleAr, s.TitleEn, s.Href, s.IconSvg)).ToList()))
            .ToListAsync(ct);

        var platforms = await db.Platforms.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new PlatformRow(p.TitleAr, p.TitleEn, p.DescriptionAr, p.DescriptionEn, p.ShortTitleAr,
                p.ShortTitleEn, p.LinkUrl, p.ShowInFooter, p.IconSvg))
            .ToListAsync(ct);

        var stats = await db.Stats.AsNoTracking()
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
            .Select(g => new PartnerGroupRow(g.Id, g.NameAr, g.NameEn))
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
            .Select(s => new SocialRow(s.Network, s.Url!))
            .ToListAsync(ct);

        var ecosystem = await LoadEcosystemAsync(db, ct);

        return new SiteSnapshot(content, pages, items, nav, platforms, stats, news, events, partners, faqs, interests, benefits, socials,
            new ListCount(newsTotal, newsEnglish),
            new ListCount(events.Count, events.Count(e => EventQueries.HasEnglish(e.TitleEn, e.SummaryEn, e.DescriptionEn))),
            new ListCount(partners.Count, partners.Count(p => Has(p.NameEn))),
            partnerGroups,
            founderPhoto,
            ecosystem,
            images);
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
            .Select(p => new PackageRow(p.Unit, p.TitleAr, p.TitleEn, p.PriceTextAr, p.PriceTextEn, p.BenefitsAr, p.BenefitsEn))
            .ToListAsync(ct);
        return new EcosystemData(speakers, guests, episodes, opportunities, packages);
    }
}
