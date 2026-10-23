using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.Infrastructure.Seo;

public enum RobotsDecision
{
    Index,
    /// <summary>Thin page (no content yet): kept out of the index, its links still followed.</summary>
    NoIndexFollow,
    NoIndexNoFollow,
}

/// <summary>
/// THE indexability rule of the public pages. The page's &lt;meta name="robots"&gt;, its X-Robots-Tag header
/// (<see cref="RobotsHeader"/>), its hreflang set and the sitemap all come from <see cref="Decide"/> on the same
/// snapshot, so they cannot disagree.
/// </summary>
public static class SeoPolicy
{
    /// <summary>HttpContext.Items key holding the page's <see cref="RobotsDecision"/>.</summary>
    public const string ItemKey = "Seo.Robots";

    private static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>The active list count of a list page.</summary>
    public static ListCount ListCountOf(string key, SiteSnapshot s) => key switch
    {
        PageKeys.ForumEvents => s.EventsCount,
        PageKeys.News => s.NewsCount,
        PageKeys.ForumPartners => s.PartnersCount,
        PageKeys.ForumSpeakers => s.Ecosystem.Speakers,
        PageKeys.TvGuests => s.Ecosystem.Guests,
        PageKeys.TvEpisodes => s.Ecosystem.Episodes,
        PageKeys.NetworkOpportunities => s.Ecosystem.Opportunities,
        // F10: the past editions (English = editions with an English title and text).
        PageKeys.ForumEditions => s.EditionsCount,
        _ => new ListCount(0, 0),
    };

    /// <summary>
    /// <list type="bullet">
    /// <item>A list page with no active item (English: no item with English text) → <see cref="RobotsDecision.NoIndexFollow"/>.</item>
    /// <item>Any other page whose intro and body are both empty → <see cref="RobotsDecision.NoIndexFollow"/> (the home page
    /// is exempt: its content is the site's).</item>
    /// <item>The English page whose English meta title or description is empty → <see cref="RobotsDecision.NoIndexFollow"/>
    /// (it would be a duplicate of the Arabic page).</item>
    /// <item>The English page of a page that is not a list (home exempt) whose English intro and body are both empty →
    /// <see cref="RobotsDecision.NoIndexFollow"/>: it would show the Arabic text under the English URL (SEO-C5 §1b).</item>
    /// <item>An unpublished page (its own row or an ancestor's IsPublished off) → <see cref="RobotsDecision.NoIndexNoFollow"/>:
    /// it is out of the sitemap (the page itself answers the 404 before reaching this rule).</item>
    /// <item>A page without its Pages row → <see cref="RobotsDecision.NoIndexFollow"/>.</item>
    /// </list>
    /// </summary>
    public static RobotsDecision Decide(SitePage page, bool english, SiteSnapshot s)
    {
        if (!s.IsPublished(page.Key)) return RobotsDecision.NoIndexNoFollow;
        if (s.Page(page.Key) is not { } row) return RobotsDecision.NoIndexFollow;

        if (page.Kind == PageKind.List)
        {
            var count = ListCountOf(page.Key, s);
            if ((english ? count.English : count.Total) == 0) return RobotsDecision.NoIndexFollow;
        }
        else if (page.Key != PageKeys.Home
                 && (english ? !Has(row.IntroEn) && !Has(row.BodyEn) : !Has(row.IntroAr) && !Has(row.BodyAr)))
        {
            return RobotsDecision.NoIndexFollow;
        }

        if (english && (!Has(row.MetaTitleEn) || !Has(row.MetaDescriptionEn))) return RobotsDecision.NoIndexFollow;
        return RobotsDecision.Index;
    }

    /// <summary>
    /// A published detail page (news item, event): the Arabic page is indexable; the English page only when the item
    /// has its English text (<see cref="EventQueries"/>). Otherwise the English URL still renders, with the Arabic
    /// fallback, as <see cref="RobotsDecision.NoIndexFollow"/> (CTO decision 2). Unpublished items are 404.
    /// </summary>
    public static RobotsDecision DecideDetail(bool english, bool englishAvailable) =>
        !english || englishAvailable ? RobotsDecision.Index : RobotsDecision.NoIndexFollow;

    /// <summary>hreflang pairs only when both languages are indexable.</summary>
    public static bool HasAlternates(SitePage page, SiteSnapshot s) =>
        Decide(page, false, s) == RobotsDecision.Index && Decide(page, true, s) == RobotsDecision.Index;
}
