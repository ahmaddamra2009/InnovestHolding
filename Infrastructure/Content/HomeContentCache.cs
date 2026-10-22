using InnovestHolding.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InnovestHolding.Infrastructure.Content;

// Immutable, culture-independent rows of everything the home page shows (both languages). Never EF entities: the
// snapshot is shared across requests and threads.
public sealed record TextPair(string Ar, string? En);
public sealed record NavSubRow(string TitleAr, string? TitleEn, string Href, string? IconSvg);
public sealed record NavRow(string TitleAr, string? TitleEn, string Href, bool HasDropdown, bool ShowInFooter, IReadOnlyList<NavSubRow> SubItems);
public sealed record PlatformRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn,
    string? ShortTitleAr, string? ShortTitleEn, string? LinkUrl, bool ShowInFooter, string IconSvg);
public sealed record StatRow(string LabelAr, string? LabelEn, string? ShortLabelAr, string? ShortLabelEn, int Value, string Suffix, string IconSvg);
public sealed record NewsRow(string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn, string CategoryAr, string? CategoryEn,
    string NewsDateAr, string? NewsDateEn, string? LinkUrl);
public sealed record EventRow(string TitleAr, string? TitleEn, string DescriptionAr, string? DescriptionEn, int EventDay,
    string EventMonthAr, string? EventMonthEn, string LocationAr, string? LocationEn, string TimeAr, string? TimeEn);
public sealed record FaqRow(string QuestionAr, string? QuestionEn, string AnswerAr, string? AnswerEn);
public sealed record InterestRow(int Id, string NameAr, string? NameEn);
public sealed record SocialRow(string Network, string Url);

public sealed record HomeSnapshot(
    IReadOnlyDictionary<string, TextPair> Content,
    IReadOnlyList<NavRow> Nav,
    IReadOnlyList<PlatformRow> Platforms,
    IReadOnlyList<StatRow> Stats,
    IReadOnlyList<NewsRow> News,
    IReadOnlyList<EventRow> Events,
    IReadOnlyList<TextPair> Partners,
    IReadOnlyList<FaqRow> Faqs,
    IReadOnlyList<InterestRow> Interests,
    IReadOnlyList<TextPair> Benefits,
    IReadOnlyList<SocialRow> Socials);

public interface IHomeContentCache
{
    Task<HomeSnapshot> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the snapshot; the next request reloads it from the database.</summary>
    void Invalidate();
}

/// <summary>
/// One cached snapshot of the home page data (singleton over IMemoryCache). A cache hit costs 0 queries; a miss
/// runs 11 AsNoTracking projections on one context, behind a semaphore so an expiry never causes a stampede.
/// Invalidated on every admin save by <see cref="ContentCacheInvalidationInterceptor"/>; the absolute expiry is only
/// a safety net for edits made outside the app (e.g. SQL run on the server).
/// </summary>
public sealed class HomeContentCache(IMemoryCache cache, IServiceScopeFactory scopes) : IHomeContentCache
{
    private const string CacheKey = "home-snapshot";
    public static readonly TimeSpan SafetyExpiry = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _generation;

    public async Task<HomeSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out HomeSnapshot? hit) && hit is not null) return hit;

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

    private async Task<HomeSnapshot> LoadAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var content = await db.SiteContents.AsNoTracking()
            .Select(c => new { c.SectionKey, c.ContentAr, c.ContentEn })
            .ToDictionaryAsync(c => c.SectionKey, c => new TextPair(c.ContentAr, c.ContentEn), ct);

        var nav = await db.NavItems.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
            .Select(n => new NavRow(n.TitleAr, n.TitleEn, n.Href, n.HasDropdown, n.ShowInFooter,
                n.SubItems.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                    .Select(s => new NavSubRow(s.TitleAr, s.TitleEn, s.Href, s.IconSvg)).ToList()))
            .ToListAsync(ct);

        var platforms = await db.Platforms.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new PlatformRow(p.TitleAr, p.TitleEn, p.DescriptionAr, p.DescriptionEn, p.ShortTitleAr,
                p.ShortTitleEn, p.LinkUrl, p.ShowInFooter, p.IconSvg))
            .ToListAsync(ct);

        var stats = await db.Stats.AsNoTracking()
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new StatRow(s.LabelAr, s.LabelEn, s.ShortLabelAr, s.ShortLabelEn, s.Value, s.Suffix, s.IconSvg))
            .ToListAsync(ct);

        // Bounded: the admin-set number of cards (one small read of a value already loaded above).
        var newsLimit = ContentKeys.ParseCount(content.GetValueOrDefault(ContentKeys.HomeNewsLimit)?.Ar, ContentKeys.HomeNewsLimitFallback);
        var news = await db.News.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
            .Take(newsLimit)
            .Select(n => new NewsRow(n.TitleAr, n.TitleEn, n.SummaryAr, n.SummaryEn, n.CategoryAr, n.CategoryEn,
                n.NewsDateAr, n.NewsDateEn, n.LinkUrl))
            .ToListAsync(ct);

        var events = await db.Events.AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Select(e => new EventRow(e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn, e.EventDay,
                e.EventMonthAr, e.EventMonthEn, e.LocationAr, e.LocationEn, e.TimeAr, e.TimeEn))
            .ToListAsync(ct);

        var partners = await db.Partners.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new TextPair(p.NameAr, p.NameEn))
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

        return new HomeSnapshot(content, nav, platforms, stats, news, events, partners, faqs, interests, benefits, socials);
    }
}
