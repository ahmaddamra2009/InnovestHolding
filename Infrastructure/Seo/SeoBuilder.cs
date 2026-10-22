using System.Text.Encodings.Web;
using System.Text.Json;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Seo;

/// <summary>Brand asset paths (technical constants, like a CSS file path — not content).</summary>
public static class BrandAssets
{
    public const string LogoOnDark = "/assets/brand/logo-on-dark.png";
    public const string LogoOnDarkWebp = "/assets/brand/logo-on-dark.webp";
    public const string LogoOnDark2x = "/assets/brand/logo-on-dark@2x.webp";
    public const string LogoOnLight = "/assets/brand/logo-on-light.png";
    public const string LogoOnLightWebp = "/assets/brand/logo-on-light.webp";
    public const string LogoOnLight2x = "/assets/brand/logo-on-light@2x.webp";

    /// <summary>The default share image (1200×630) the migration writes to seo_og_image.</summary>
    public const string OgDefault = "/assets/brand/og-default.jpg";
    public const int OgDefaultWidth = 1200;
    public const int OgDefaultHeight = 630;
}

/// <summary>
/// The &lt;head&gt; of a public page: title, description, canonical, hreflang (only when both languages are indexable),
/// Open Graph, robots and one JSON-LD @graph. Robots comes from <see cref="SeoPolicy.Decide"/> (pages) or
/// <see cref="SeoPolicy.DecideDetail"/> (news items, events), the same functions the sitemap uses.
/// </summary>
public static class SeoBuilder
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) };

    /// <param name="listedNames">The rows the page renders (one page of a paged list), with their detail URL when they
    /// have one; the JSON-LD ItemList describes exactly those.</param>
    public static SeoVm Build(SitePage page, SiteViews v, string? baseUrl, RobotsDecision? overrideRobots = null,
        IReadOnlyList<(string Name, string? Url)>? listedNames = null)
    {
        var s = v.Snapshot;
        var english = v.English;
        var c = v.Content;
        var row = s.Page(page.Key);
        var siteName = c.Text("site_name").Value;

        var metaTitle = row is null ? "" : v.P(row.MetaTitleAr, row.MetaTitleEn).Value;
        var title = string.IsNullOrWhiteSpace(metaTitle) ? v.Title(page.Key).Value : metaTitle;
        var fullTitle = string.IsNullOrWhiteSpace(title) ? siteName : $"{title} | {siteName}";

        var description = row is null ? "" : v.P(row.MetaDescriptionAr, row.MetaDescriptionEn).Value;
        if (string.IsNullOrWhiteSpace(description) && row is not null)
            description = Shorten(v.P(row.SummaryAr, row.SummaryEn).Value is { Length: > 0 } sum ? sum : v.P(row.IntroAr, row.IntroEn).Value);

        var robots = overrideRobots ?? SeoPolicy.Decide(page, english, s);
        var alternates = overrideRobots is null && SeoPolicy.HasAlternates(page, s);

        string? Abs(string path) => baseUrl is null ? null : baseUrl + path;
        var canonical = Abs(CultureUrls.Page(page.Key, english));
        var og = ShareImage(v, baseUrl, row?.Image);

        return new SeoVm(
            Lang: english ? Cultures.EnglishName : Cultures.ArabicName,
            Title: fullTitle,
            Description: description,
            SiteName: siteName,
            Canonical: canonical,
            AlternateAr: alternates ? Abs(CultureUrls.Page(page.Key, false)) : null,
            AlternateEn: alternates ? Abs(CultureUrls.Page(page.Key, true)) : null,
            OgLocale: english ? "en_US" : "ar_AR",
            OgLocaleAlternate: alternates ? (english ? "ar_AR" : "en_US") : null,
            OgImage: og.Url,
            OgImageAlt: og.Alt ?? c.Text("seo_og_image_alt").Value,
            JsonLd: BuildJsonLd(page, v, baseUrl, fullTitle, description, listedNames),
            Robots: robots,
            TwitterCard: og.Width >= 1200 ? "summary_large_image" : "summary",
            OgImageWidth: og.Width,
            OgImageHeight: og.Height);
    }

    private readonly record struct Share(string? Url, int? Width, int? Height, string? Alt);

    /// <summary>The share image: the first uploaded candidate (item, then page), else the site default (seo_og_image).</summary>
    private static Share ShareImage(SiteViews v, string? baseUrl, params MediaRef?[] candidates)
    {
        if (baseUrl is null) return default;
        foreach (var m in candidates)
        {
            if (m is null) continue;
            var alt = v.P(m.AltAr, m.AltEn).Value;
            return new Share(baseUrl + m.Url, m.Width, m.Height, string.IsNullOrWhiteSpace(alt) ? null : alt);
        }
        var path = v.Content.Neutral("seo_og_image");
        if (!SiteUrl.IsSitePath(path)) return default;
        return path == BrandAssets.OgDefault
            ? new Share(baseUrl + path, BrandAssets.OgDefaultWidth, BrandAssets.OgDefaultHeight, null)
            : new Share(baseUrl + path, null, null, null);
    }

    /// <summary>
    /// The head of a detail page (news item, event). The English page of an item without English text renders with
    /// the Arabic fallback but is <c>noindex,follow</c>, canonical to the Arabic URL and has no hreflang (CTO
    /// decision 2); the two language URLs are paired only when the English text exists. The share image is the
    /// item's, else its list page's, else the site default.
    /// </summary>
    /// <param name="node">The item's schema.org node (NewsArticle / Event) without @id, url, inLanguage and image.</param>
    public static SeoVm BuildDetail(DetailRoute route, string slug, SiteViews v, string? baseUrl, bool englishAvailable,
        LText title, string metaTitle, string description, MediaRef? image, string ogType, Dictionary<string, object> node)
    {
        var english = v.English;
        var siteName = v.Content.Text("site_name").Value;
        var name = string.IsNullOrWhiteSpace(metaTitle) ? title.Value : metaTitle.Trim();
        var fullTitle = $"{name} | {siteName}";
        var robots = SeoPolicy.DecideDetail(english, englishAvailable);

        string? Abs(string path) => baseUrl is null ? null : baseUrl + path;
        var own = CultureUrls.Detail(route.Key, slug, english);
        var canonical = Abs(robots == RobotsDecision.Index ? own : CultureUrls.Detail(route.Key, slug, false));
        var og = ShareImage(v, baseUrl, image, v.Snapshot.Page(route.Parent)?.Image);

        var graph = BaseGraph(v, baseUrl);
        Put(node, "@id", canonical is null ? null : canonical + "#" + node["@type"].ToString()!.ToLowerInvariant());
        Put(node, "url", canonical);
        Put(node, "inLanguage", english && englishAvailable ? "en" : "ar");
        if (image is not null && og.Url is not null) node["image"] = new[] { og.Url };
        graph.Add(node);

        var crumbs = SiteRoutes.Chain(route.Parent)
            .Select(p => (v.Title(p.Key).Value, Abs(CultureUrls.Page(p.Key, english))))
            .Append((title.Value, (string?)null)).ToList();
        graph.Add(Breadcrumbs(crumbs));

        return new SeoVm(
            Lang: english ? Cultures.EnglishName : Cultures.ArabicName,
            Title: fullTitle,
            Description: Shorten(description),
            SiteName: siteName,
            Canonical: canonical,
            AlternateAr: englishAvailable ? Abs(CultureUrls.Detail(route.Key, slug, false)) : null,
            AlternateEn: englishAvailable ? Abs(CultureUrls.Detail(route.Key, slug, true)) : null,
            OgLocale: english ? "en_US" : "ar_AR",
            OgLocaleAlternate: englishAvailable ? (english ? "ar_AR" : "en_US") : null,
            OgImage: og.Url,
            OgImageAlt: og.Alt ?? title.Value,
            JsonLd: JsonSerializer.Serialize(new Dictionary<string, object> { ["@context"] = "https://schema.org", ["@graph"] = graph }, Json),
            Robots: robots,
            OgType: ogType,
            TwitterCard: og.Width >= 1200 ? "summary_large_image" : "summary",
            OgImageWidth: og.Width,
            OgImageHeight: og.Height);
    }

    /// <summary>A node reference ({"@id": …}) to the site's Organization, or null without a base URL.</summary>
    public static Dictionary<string, object>? OrganizationRef(string? baseUrl) =>
        baseUrl is null ? null : new Dictionary<string, object> { ["@id"] = baseUrl + "/#organization" };

    /// <summary>A node reference to the TV programme (the TVSeries node of the TV hub page), or null without a base URL.</summary>
    public static Dictionary<string, object>? TvSeriesRef(string? baseUrl) =>
        baseUrl is null ? null : new Dictionary<string, object> { ["@id"] = baseUrl + CultureUrls.Page(PageKeys.Tv, false) + "#tvseries" };

    /// <summary>Sets <paramref name="key"/> unless the value is empty (the JSON-LD never carries empty values).</summary>
    public static void Set(Dictionary<string, object> d, string key, object? value) => Put(d, key, value);

    /// <summary>At most 155 characters, cut at a word boundary, no ellipsis.</summary>
    public static string Shorten(string text, int max = 155)
    {
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (text.Length <= max) return text;
        var cut = text.LastIndexOf(' ', max);
        return (cut > 40 ? text[..cut] : text[..max]).TrimEnd(' ', ',', '،', '.', ':', ';');
    }

    private static void Put(Dictionary<string, object> d, string key, object? value)
    {
        if (value is null) return;
        if (value is string str && string.IsNullOrWhiteSpace(str)) return;
        if (value is Dictionary<string, object> nested && (nested.Count == 0 || (nested.Count == 1 && nested.ContainsKey("@type")))) return; // only "@type" left
        if (value is Array array && array.Length == 0) return;
        if (value is System.Collections.ICollection { Count: 0 }) return;
        d[key] = value;
    }

    private static Dictionary<string, object> Breadcrumbs(IReadOnlyList<(string Name, string? Url)> crumbs) => new()
    {
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = crumbs.Select((x, i) =>
        {
            var li = new Dictionary<string, object> { ["@type"] = "ListItem", ["position"] = i + 1, ["name"] = x.Name };
            if (i < crumbs.Count - 1) Put(li, "item", x.Url);
            return li;
        }).ToList(),
    };

    /// <summary>
    /// One @graph per page, serialised by System.Text.Json (its encoder always escapes &lt; and &gt;, so a value containing
    /// &lt;/script&gt; cannot close the tag). Empty values are omitted; only what the page shows is described.
    /// </summary>
    public static string BuildJsonLd(SitePage page, SiteViews v, string? baseUrl, string fullTitle, string description,
        IReadOnlyList<(string Name, string? Url)>? listedNames = null)
    {
        var graph = BaseGraph(v, baseUrl);
        if (page.Key != PageKeys.Home) AddPageNodes(graph, page, v, baseUrl, fullTitle, description, listedNames);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["@context"] = "https://schema.org", ["@graph"] = graph }, Json);
    }

    /// <summary>The Organization and WebSite nodes every page carries (so each page's references resolve in its own graph).</summary>
    private static List<object> BaseGraph(SiteViews v, string? baseUrl)
    {
        var c = v.Content;
        var english = v.English;
        string? Abs(string path) => baseUrl is null ? null : baseUrl + path;
        var orgId = baseUrl is null ? null : baseUrl + "/#organization";
        var siteId = baseUrl is null ? null : baseUrl + "/#website";
        var graph = new List<object>();

        // Organization (the same node on every page: NAP from the same keys as the footer and the contact page).
        var siteName = c.Text("site_name").Value;
        var other = new ContentVm(v.Snapshot.Content, !english).Text("site_name").Value;
        var email = c.Neutral("contact_email");
        var phone = c.Neutral("contact_phone");
        var address = new Dictionary<string, object> { ["@type"] = "PostalAddress" };
        Put(address, "streetAddress", c.Text("contact_address").Value);
        Put(address, "addressLocality", c.Neutral("org_address_locality"));
        Put(address, "addressCountry", c.Neutral("org_address_country"));
        var contactPoint = new Dictionary<string, object> { ["@type"] = "ContactPoint" };
        Put(contactPoint, "email", email);
        Put(contactPoint, "telephone", phone);
        if (contactPoint.Count > 1)
        {
            contactPoint["contactType"] = "customer service";
            contactPoint["availableLanguage"] = new[] { "ar", "en" };
        }
        var org = new Dictionary<string, object> { ["@type"] = "Organization" };
        Put(org, "@id", orgId);
        Put(org, "name", siteName);
        Put(org, "alternateName", other == siteName ? null : other);
        Put(org, "url", Abs(CultureUrls.Home(english)));
        Put(org, "logo", Abs(BrandAssets.LogoOnLight));
        Put(org, "description", c.Text("brand_positioning").Value);
        Put(org, "email", email);
        Put(org, "telephone", phone);
        Put(org, "address", address);
        Put(org, "contactPoint", contactPoint);
        Put(org, "sameAs", v.Snapshot.Socials.Where(x => SiteUrl.IsHttpsUrl(x.Url)).Select(x => x.Url.Trim()).ToArray());
        graph.Add(org);

        // WebSite: on every page, so each page's isPartOf reference resolves inside its own graph.
        var site = new Dictionary<string, object> { ["@type"] = "WebSite" };
        Put(site, "@id", siteId);
        Put(site, "url", Abs(CultureUrls.Home(english)));
        Put(site, "name", siteName);
        Put(site, "inLanguage", english ? "en" : "ar");
        if (orgId is not null) site["publisher"] = new Dictionary<string, object> { ["@id"] = orgId };
        graph.Add(site);
        return graph;
    }

    private static void AddPageNodes(List<object> graph, SitePage page, SiteViews v, string? baseUrl, string fullTitle,
        string description, IReadOnlyList<(string Name, string? Url)>? listedNames)
    {
        var english = v.English;
        string? Abs(string path) => baseUrl is null ? null : baseUrl + path;
        var orgId = baseUrl is null ? null : baseUrl + "/#organization";
        var siteId = baseUrl is null ? null : baseUrl + "/#website";
        var pageUrl = Abs(CultureUrls.Page(page.Key, english));

        var type = page.Key switch
        {
            PageKeys.About => "AboutPage",
            PageKeys.Contact => "ContactPage",
            PageKeys.Ecosystem => "CollectionPage",
            _ when page.Kind == PageKind.List => "CollectionPage",
            _ => "WebPage",
        };
        var webPage = new Dictionary<string, object> { ["@type"] = type };
        Put(webPage, "@id", pageUrl is null ? null : pageUrl + "#webpage");
        Put(webPage, "url", pageUrl);
        Put(webPage, "name", fullTitle);
        Put(webPage, "description", description);
        Put(webPage, "inLanguage", english ? "en" : "ar");
        if (siteId is not null) webPage["isPartOf"] = new Dictionary<string, object> { ["@id"] = siteId };
        if (orgId is not null && type is "AboutPage" or "ContactPage") webPage["about"] = new Dictionary<string, object> { ["@id"] = orgId };

        // The visible list of the page (the ecosystem lists its units). A paged list passes the rows it renders, so
        // the ItemList never names a row of another page; rows with a detail page carry its URL.
        var listItems = listedNames is not null
            ? listedNames.Select(x => (x.Name, x.Url is null ? null : Abs(x.Url))).ToList()
            : page.Key switch
            {
                PageKeys.Ecosystem => v.Cards(PageKeys.Ecosystem, false).Select(x => (x.Title.Value, Abs(x.Href))).ToList(),
                PageKeys.ForumPartners => v.Partners().Select(p => (p.Name.Value, (string?)null)).ToList(),
                _ => [],
            };
        if (listItems.Count > 0)
        {
            webPage["mainEntity"] = new Dictionary<string, object>
            {
                ["@type"] = "ItemList",
                ["itemListElement"] = listItems.Select((x, i) =>
                {
                    var li = new Dictionary<string, object> { ["@type"] = "ListItem", ["position"] = i + 1, ["name"] = x.Item1 };
                    Put(li, "url", x.Item2);
                    return li;
                }).ToList(),
            };
        }
        graph.Add(webPage);

        // Forum and Network are brands of the company.
        if (page.Key is PageKeys.Forum or PageKeys.Network)
        {
            var unit = new Dictionary<string, object> { ["@type"] = "Organization" };
            Put(unit, "@id", pageUrl is null ? null : pageUrl + "#organization");
            Put(unit, "name", v.Title(page.Key).Value);
            Put(unit, "url", pageUrl);
            Put(unit, "description", v.Summary(page.Key).Value);
            if (orgId is not null) unit["parentOrganization"] = new Dictionary<string, object> { ["@id"] = orgId };
            graph.Add(unit);
        }

        // The TV programme: its hub page describes the series that every episode is part of (one @id in both languages).
        if (page.Key == PageKeys.Tv)
        {
            var series = new Dictionary<string, object> { ["@type"] = "TVSeries" };
            Put(series, "@id", TvSeriesRef(baseUrl)?["@id"]);
            Put(series, "name", v.Title(page.Key).Value);
            Put(series, "url", pageUrl);
            Put(series, "description", v.Summary(page.Key).Value);
            if (orgId is not null) series["producer"] = new Dictionary<string, object> { ["@id"] = orgId };
            graph.Add(series);
        }

        // BreadcrumbList = the visible breadcrumb (same route-table chain, same labels).
        graph.Add(Breadcrumbs(SiteRoutes.Chain(page.Key)
            .Select(p => (v.Title(p.Key).Value, Abs(CultureUrls.Page(p.Key, english)))).ToList()));
    }
}
