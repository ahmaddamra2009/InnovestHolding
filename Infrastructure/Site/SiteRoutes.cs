namespace InnovestHolding.Infrastructure.Site;

/// <summary>What a page shows: drives its indexability rule (<see cref="Seo.SeoPolicy"/>).</summary>
public enum PageKind
{
    /// <summary>Text from the Pages row (and lists of ContentItems).</summary>
    Static,
    /// <summary>A list of entity rows (events, news…); thin while it has no item.</summary>
    List,
    /// <summary>A page that carries a form or a call to action to one.</summary>
    Form,
}

/// <param name="Key">Pages.Key — the stable name of the page (never shown).</param>
/// <param name="Path">Arabic path; the English one is "/en" + Path ("/en" for the home page). Lower-case, no trailing slash.</param>
/// <param name="Parent">Key of the parent page (breadcrumbs, menus); null for the home page.</param>
public sealed record SitePage(string Key, string Path, string? Parent, string Controller, string Action, PageKind Kind);

/// <summary>
/// A detail page: <see cref="Prefix"/> + slug (Arabic) and "/en" + prefix + slug (English). Kept apart from
/// <see cref="SiteRoutes.All"/> (which lists the fixed pages that menus, the sitemap's page part and the Pages admin
/// iterate); its parent is the list page it belongs to.
/// </summary>
/// <param name="EntityType">The <c>SlugRedirects.EntityType</c> of its items.</param>
public sealed record DetailRoute(string Key, string Parent, string Prefix, string Controller, string Action, string EntityType);

/// <summary>Keys of the detail routes.</summary>
public static class DetailKeys
{
    public const string NewsItem = "news.item";
    public const string ForumEvent = "forum.event";
    public const string ForumSpeaker = "forum.speaker";
    public const string NetworkOpportunity = "network.opportunity";
    public const string TvEpisode = "tv.episode";
    public const string TvGuest = "tv.guest";
}

/// <summary>Page keys (code constants: the structure of the site is code, its texts are data).</summary>
public static class PageKeys
{
    public const string Home = "home";
    public const string About = "about";
    public const string Ecosystem = "ecosystem";
    public const string Forum = "forum";
    public const string ForumEvents = "forum.events";
    public const string ForumSpeakers = "forum.speakers";
    public const string ForumPartners = "forum.partners";
    public const string ForumRegistration = "forum.registration";
    public const string Network = "network";
    public const string NetworkInvestors = "network.investors";
    public const string NetworkStartups = "network.startups";
    public const string NetworkOpportunities = "network.opportunities";
    public const string NetworkJoin = "network.join";
    public const string Tv = "tv";
    public const string TvEpisodes = "tv.episodes";
    public const string TvGuests = "tv.guests";
    public const string TvSponsorship = "tv.sponsorship";
    public const string News = "news";
    public const string Contact = "contact";
}

/// <summary>
/// THE route table of the public site (the structure image of the company). Program.cs maps every entry twice
/// (Arabic and /en), the menus, breadcrumbs, sitemap, normaliser and language switch all read it, so a path is
/// written once. Pages that a later phase adds (detail pages) are simply absent until they ship.
/// </summary>
public static class SiteRoutes
{
    public const string EnglishPrefix = "/en";

    public static readonly IReadOnlyList<SitePage> All =
    [
        new(PageKeys.Home, "/", null, "Home", "Index", PageKind.Static),
        new(PageKeys.About, "/about", PageKeys.Home, "SitePages", "About", PageKind.Static),
        new(PageKeys.Ecosystem, "/ecosystem", PageKeys.Home, "SitePages", "Ecosystem", PageKind.Static),
        new(PageKeys.Forum, "/forum", PageKeys.Ecosystem, "Forum", "Index", PageKind.Static),
        new(PageKeys.ForumEvents, "/forum/events", PageKeys.Forum, "Forum", "Events", PageKind.List),
        new(PageKeys.ForumSpeakers, "/forum/speakers", PageKeys.Forum, "Forum", "Speakers", PageKind.List),
        new(PageKeys.ForumPartners, "/forum/partners", PageKeys.Forum, "Forum", "Partners", PageKind.List),
        new(PageKeys.ForumRegistration, "/forum/registration", PageKeys.Forum, "Forum", "Registration", PageKind.Form),
        new(PageKeys.Network, "/angel-network", PageKeys.Ecosystem, "Network", "Index", PageKind.Static),
        new(PageKeys.NetworkInvestors, "/angel-network/investors", PageKeys.Network, "Network", "Investors", PageKind.Static),
        new(PageKeys.NetworkStartups, "/angel-network/startups", PageKeys.Network, "Network", "Startups", PageKind.Static),
        new(PageKeys.NetworkOpportunities, "/angel-network/opportunities", PageKeys.Network, "Network", "Opportunities", PageKind.List),
        new(PageKeys.NetworkJoin, "/angel-network/join", PageKeys.Network, "Network", "Join", PageKind.Form),
        new(PageKeys.Tv, "/tv", PageKeys.Ecosystem, "Tv", "Index", PageKind.Static),
        new(PageKeys.TvEpisodes, "/tv/episodes", PageKeys.Tv, "Tv", "Episodes", PageKind.List),
        new(PageKeys.TvGuests, "/tv/guests", PageKeys.Tv, "Tv", "Guests", PageKind.List),
        new(PageKeys.TvSponsorship, "/tv/sponsorship", PageKeys.Tv, "Tv", "Sponsorship", PageKind.Form),
        new(PageKeys.News, "/news", PageKeys.Home, "Newsroom", "Index", PageKind.List),
        new(PageKeys.Contact, "/contact", PageKeys.Home, "SitePages", "Contact", PageKind.Form),
    ];

    public static readonly IReadOnlyList<DetailRoute> Details =
    [
        new(DetailKeys.NewsItem, PageKeys.News, "/news/", "Newsroom", "Item", Models.SlugTypes.News),
        new(DetailKeys.ForumEvent, PageKeys.ForumEvents, "/forum/events/", "Forum", "Event", Models.SlugTypes.Event),
        new(DetailKeys.ForumSpeaker, PageKeys.ForumSpeakers, "/forum/speakers/", "Forum", "Speaker", Models.SlugTypes.Speaker),
        new(DetailKeys.NetworkOpportunity, PageKeys.NetworkOpportunities, "/angel-network/opportunities/", "Network", "Opportunity", Models.SlugTypes.Opportunity),
        new(DetailKeys.TvEpisode, PageKeys.TvEpisodes, "/tv/episodes/", "Tv", "Episode", Models.SlugTypes.Episode),
        new(DetailKeys.TvGuest, PageKeys.TvGuests, "/tv/guests/", "Tv", "Guest", Models.SlugTypes.Guest),
    ];

    public static DetailRoute Detail(string key) => Details.Single(d => d.Key == key);

    public static string DetailPath(DetailRoute route, string slug, bool english) =>
        (english ? EnglishPrefix : "") + route.Prefix + slug;

    /// <summary>True when <paramref name="path"/> is exactly a detail URL (either language) with a well-formed slug.</summary>
    public static bool MatchesDetail(string path) => ParseDetail(path) is not null;

    /// <summary>The detail route, slug and language of a canonical detail path, else null.</summary>
    public static (DetailRoute Route, string Slug, bool English)? ParseDetail(string path)
    {
        var english = path.StartsWith(EnglishPrefix + "/", StringComparison.Ordinal);
        if (english) path = path[EnglishPrefix.Length..];
        foreach (var d in Details)
        {
            if (path.StartsWith(d.Prefix, StringComparison.Ordinal) && Slugs.Pattern().IsMatch(path[d.Prefix.Length..]))
                return (d, path[d.Prefix.Length..], english);
        }
        return null;
    }

    private static readonly Dictionary<string, SitePage> ByKey = All.ToDictionary(p => p.Key, StringComparer.Ordinal);

    /// <summary>Canonical paths (both languages) → page.</summary>
    private static readonly Dictionary<string, SitePage> ByPath = All
        .SelectMany(p => new[] { (Path: p.Path, Page: p), (Path: PathFor(p, true), Page: p) })
        .ToDictionary(x => x.Path, x => x.Page, StringComparer.Ordinal);

    public static SitePage Get(string key) =>
        ByKey.TryGetValue(key, out var page) ? page : throw new KeyNotFoundException($"Unknown page key '{key}'.");

    public static SitePage? Find(string? key) => key is not null && ByKey.TryGetValue(key, out var page) ? page : null;

    public static bool IsKey(string? key) => key is not null && ByKey.ContainsKey(key);

    public static string PathFor(SitePage page, bool english) =>
        !english ? page.Path : page.Path == "/" ? EnglishPrefix : EnglishPrefix + page.Path;

    /// <summary>The page whose canonical path (either language) is exactly <paramref name="path"/>, else null.</summary>
    public static SitePage? MatchCanonical(string path) => ByPath.GetValueOrDefault(path);

    /// <summary>The page, then its parent, … up to (and including) the home page.</summary>
    public static IReadOnlyList<SitePage> Chain(string key)
    {
        var chain = new List<SitePage>();
        for (var page = Find(key); page is not null; page = Find(page.Parent))
            chain.Insert(0, page);
        return chain;
    }

    public static IEnumerable<SitePage> Children(string key) => All.Where(p => p.Parent == key);
}
