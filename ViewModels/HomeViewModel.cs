using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using Microsoft.AspNetCore.Html;

namespace InnovestHolding.ViewModels;

/// <summary>
/// One movable section of the home page in the admin's order (PageSections, page "home"). <see cref="Render"/>: the
/// admin's switch AND it has content (today's empty rule). A section that does not render leaves its empty anchor
/// (<see cref="Anchor"/>) so old <c>/#news</c> links still land; the CTA band has none.
/// </summary>
public sealed record HomeSectionVm(string Key, bool Render, string? Anchor);

/// <summary>The home page: the shell plus teasers of the other pages. Section ids of the one-page site are kept.</summary>
public sealed class HomeViewModel
{
    public required bool IsEnglish { get; init; }
    public string Lang => IsEnglish ? Cultures.EnglishName : Cultures.ArabicName;
    public required ContentVm Content { get; init; }
    public required ShellVm Shell { get; init; }
    public required SeoVm Seo { get; init; }

    public required IReadOnlyList<CardVm> Units { get; init; }
    public required IReadOnlyList<ServiceVm> Services { get; init; }
    public required IReadOnlyList<StatVm> Stats { get; init; }
    public required IReadOnlyList<NewsCardVm> News { get; init; }
    public required IReadOnlyList<EventCardVm> Events { get; init; }
    public required IReadOnlyList<PartnerVm> Partners { get; init; }
    public required IReadOnlyList<FaqVm> Faqs { get; init; }
    public required RegisterFormVm Register { get; init; }
    public required ContactFormVm Contact { get; init; }
    public required CtaBlock CtaBand { get; init; }

    /// <summary>The movable sections after the hero, in the admin's order (rendered or anchor only).</summary>
    public required IReadOnlyList<HomeSectionVm> Sections { get; init; }

    /// <summary>The figures under the hero text (hide-only section "hero_stats", count hero_stats_limit); empty = none.</summary>
    public required IReadOnlyList<StatVm> HeroStats { get; init; }

    /// <summary>Vision / mission / values under About (hide-only section "about_values").</summary>
    public required bool ShowValues { get; init; }

    /// <summary>The hero's main button (empty fields = the header button) and second button; null = not rendered.</summary>
    public required LinkVm? HeroMain { get; init; }
    public required LinkVm? HeroSecondary { get; init; }

    /// <summary>About's "more" button; null while its page is unpublished.</summary>
    public required LinkVm? AboutMore { get; init; }

    /// <summary>The action of the event rows (events_action); null = none.</summary>
    public required LinkVm? EventsAction { get; init; }

    /// <summary>"View all" links of the teasers (section key → page link); a key is absent while its page is unpublished.</summary>
    public required IReadOnlyDictionary<string, LinkVm> ViewAll { get; init; }

    /// <summary>Badge and card icons (Content keys <c>*_icon</c>, default = the C4 icons).</summary>
    public required IReadOnlyDictionary<string, string> Icons { get; init; }

    public string Icon(string name) => Icons[name];

    /// <summary>Reveal delay class: the literal page's pattern, clamped to the 6 classes the CSS defines.</summary>
    public static string Delay(int n) => n <= 0 ? "" : $" reveal-delay-{Math.Min(n, 6)}";

    /// <summary>The arrow of the hero and About buttons (mirrored by the i-dir class in English).</summary>
    public const string Arrow = "<line x1=\"19\" y1=\"12\" x2=\"5\" y2=\"12\"/><polyline points=\"12 19 5 12 12 5\"/>";

    /// <summary>rel of a button: external links (https) only.</summary>
    public static string? Rel(LinkVm link) => link.Href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "noopener noreferrer" : null;

    /// <summary>"View all" link under a teaser section (the label is a UI string); nothing when the page is unpublished.</summary>
    public IHtmlContent ViewAllLink(string section, string label) =>
        ViewAll.TryGetValue(section, out var link)
            ? new HtmlString(
                $"<div class=\"section-more\"><a class=\"btn btn-outline\" href=\"{System.Net.WebUtility.HtmlEncode(link.Href)}\">{System.Net.WebUtility.HtmlEncode(label)}</a></div>")
            : HtmlString.Empty;

    /// <summary>The empty anchor of a section that does not render (id only — never linked from the page).</summary>
    private static readonly IReadOnlyDictionary<string, string?> Anchors = new Dictionary<string, string?>(StringComparer.Ordinal)
    {
        ["about"] = "about", ["ecosystem"] = "ecosystem", ["platforms"] = "platforms", ["stats"] = "stats", ["events"] = "events",
        ["news"] = "news", ["partners"] = "partners", ["faq"] = "faq", ["register"] = "register", ["contact"] = "contact",
        ["cta_band"] = null,
    };

    public static HomeViewModel Create(SiteViews v, ShellVm shell, SeoVm seo, DateTimeOffset now)
    {
        var s = v.Snapshot;
        var c = v.Content;
        var services = v.Services();
        var stats = v.Stats();
        var news = v.NewsCards(s.News);
        var events = v.EventCards(s.Events.Take(PageSectionRegistry.Limit(s, "home_events_limit")), now);
        var partners = v.Partners(homeOnly: true);
        var faqs = v.Faqs();
        var units = v.Cards(PageKeys.Ecosystem, true);

        // Content of each section (a list section with no items does not render — today's rule).
        var hasContent = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["ecosystem"] = units.Count > 0,
            ["platforms"] = services.Count > 0,
            ["stats"] = stats.Count > 0,
            ["events"] = events.Count > 0,
            ["news"] = news.Count > 0,
            ["partners"] = partners.Count > 0,
            ["faq"] = faqs.Count > 0,
        };
        var layout = PageSectionRegistry.Layout(s, PageKeys.Home);
        var sections = layout.Where(x => x.Def.Movable && Anchors.ContainsKey(x.Def.Key))
            .Select(x => new HomeSectionVm(x.Def.Key, x.Visible && hasContent.GetValueOrDefault(x.Def.Key, true), Anchors[x.Def.Key]))
            .ToList();
        bool Visible(string key) => layout.Any(x => x.Def.Key == key && x.Visible);

        // Hero buttons: the main one is the header button until the admin gives it its own label or target.
        var mainLabel = c.Optional("hero_cta_main_label");
        var heroMain = string.IsNullOrWhiteSpace(c.Neutral("hero_cta_main_link"))
            ? mainLabel.IsEmpty ? shell.Cta : shell.Cta with { Title = mainLabel }
            : SiteLinks.Resolve(v, "hero_cta_main_link", null, mainLabel);

        var viewAll = new Dictionary<string, LinkVm>(StringComparer.Ordinal);
        foreach (var (section, key) in new[] { ("events", PageKeys.ForumEvents), ("news", PageKeys.News), ("partners", PageKeys.ForumPartners) })
            if (SiteLinks.Page(v, key) is { } link) viewAll[section] = link;

        string Icon(string key, string fallback) => PageSectionRegistry.Icon(s, key, fallback);

        return new HomeViewModel
        {
            IsEnglish = v.English,
            Content = c,
            Shell = shell,
            Seo = seo,
            Units = units,
            Services = services,
            Stats = stats,
            News = news,
            Events = events,
            Partners = partners,
            Faqs = faqs,
            Register = v.RegisterForm("reg"),
            Contact = v.ContactForm("contact"),
            CtaBand = v.CtaBand(),
            Sections = sections,
            HeroStats = Visible("hero_stats") ? stats.Take(PageSectionRegistry.Limit(s, "hero_stats_limit")).ToList() : [],
            ShowValues = Visible("about_values"),
            HeroMain = heroMain,
            HeroSecondary = SiteLinks.Resolve(v, "hero_cta_secondary_link", PageKeys.Ecosystem, c.Text("hero_cta_primary")),
            AboutMore = SiteLinks.Resolve(v, "about_more_link", PageKeys.About, c.Optional("about_more_label")),
            EventsAction = SiteLinks.EventsAction(v),
            ViewAll = viewAll,
            Icons = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["vision"] = Icon("vision_icon", Infrastructure.Content.Icons.Svg("eye")!),
                ["mission"] = Icon("mission_icon", Infrastructure.Content.Icons.Svg("target")!),
                ["values"] = Icon("values_icon", Infrastructure.Content.Icons.Svg("handshake")!),
                ["ecosystem"] = Icon("ecosystem_icon", Infrastructure.Content.Icons.Svg("layers")!),
                ["platforms"] = Icon("platforms_icon", SiteViews.BadgeGrid),
                ["events"] = Icon("events_icon", Infrastructure.Content.Icons.Svg("calendar")!),
                ["news"] = Icon("news_icon", Infrastructure.Content.Icons.Svg("book")!),
                ["partners"] = Icon("partners_icon", Infrastructure.Content.Icons.Svg("handshake")!),
                ["faq"] = Icon("faq_icon", SiteViews.BadgeQuestion),
                ["contact"] = Icon("contact_icon", Infrastructure.Content.Icons.Svg("mic")!),
            },
        };
    }
}
