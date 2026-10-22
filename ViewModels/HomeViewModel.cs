using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;

namespace InnovestHolding.ViewModels;

// Display records of the public page, already resolved for the page language (fallback applied).
public sealed record NavLinkVm(string Href, LText Title, string? IconSvg, IReadOnlyList<NavLinkVm> SubItems);
public sealed record PlatformCardVm(LText Title, LText Description, string? LinkUrl, string IconSvg);
public sealed record FooterPlatformVm(LText Title);
public sealed record StatVm(int Value, string Suffix, LText Label, LText ShortLabel, string IconSvg);
public sealed record NewsCardVm(LText Title, LText Summary, LText Category, LText Date, string? LinkUrl);
public sealed record EventCardVm(int Day, LText Month, LText Title, LText Description, LText Location, LText Time);
public sealed record FaqVm(LText Question, LText Answer);
public sealed record InterestChoiceVm(int Id, LText Name);
public sealed record SocialVm(string Network, string Url);

/// <summary>Everything the &lt;head&gt; needs. Absolute URLs are null when no base URL is known (then omitted).</summary>
public sealed record SeoVm(
    string Lang, string Title, string Description, string SiteName,
    string? Canonical, string? AlternateAr, string? AlternateEn,
    string OgLocale, string OgLocaleAlternate, string? OgImage, string OgImageAlt, string? JsonLd);

/// <summary>SiteContents accessor for the page language (one fallback rule: <see cref="Bilingual"/>).</summary>
public sealed class ContentVm(IReadOnlyDictionary<string, TextPair> content, bool english)
{
    private TextPair? Row(string key) => content.GetValueOrDefault(key);

    /// <summary>English with fallback to Arabic (Arabic on the Arabic page).</summary>
    public LText Text(string key) => Row(key) is { } r ? Bilingual.Pick(r.Ar, r.En, english) : LText.Empty;

    /// <summary>No fallback (keys flagged Optional in the registry, e.g. hero_kicker).</summary>
    public LText Optional(string key) => Row(key) is { } r ? Bilingual.Optional(r.Ar, r.En, english) : LText.Empty;

    /// <summary>Language-neutral value (phone, email, URLs).</summary>
    public string Neutral(string key) => Row(key)?.Ar ?? "";
}

public sealed class HomeViewModel
{
    public required bool IsEnglish { get; init; }
    public string Lang => IsEnglish ? Cultures.EnglishName : Cultures.ArabicName;
    public required ContentVm Content { get; init; }

    public required IReadOnlyList<NavLinkVm> NavLinks { get; init; }
    public required IReadOnlyList<NavLinkVm> FooterLinks { get; init; }
    public required IReadOnlyList<PlatformCardVm> Platforms { get; init; }
    public required IReadOnlyList<FooterPlatformVm> FooterPlatforms { get; init; }
    public required IReadOnlyList<StatVm> Stats { get; init; }
    public required IReadOnlyList<NewsCardVm> News { get; init; }
    public required IReadOnlyList<EventCardVm> Events { get; init; }
    public required IReadOnlyList<LText> Partners { get; init; }
    public required IReadOnlyList<FaqVm> Faqs { get; init; }
    public required IReadOnlyList<InterestChoiceVm> Interests { get; init; }
    public required IReadOnlyList<LText> Benefits { get; init; }
    public required IReadOnlyList<SocialVm> Socials { get; init; }

    /// <summary>Section ids rendered on the page (a list section with no items is not rendered — nor its links).</summary>
    public required IReadOnlySet<string> VisibleSections { get; init; }

    public required SeoVm Seo { get; init; }

    public string RegisterAction => CultureUrls.FormAction("Register", IsEnglish);
    public string ContactAction => CultureUrls.FormAction("Contact", IsEnglish);

    /// <summary>The same page in the other language.</summary>
    public string OtherLanguageUrl => CultureUrls.Home(!IsEnglish);

    /// <summary>Reveal delay class: the literal page's pattern, clamped to the 6 classes the CSS defines.</summary>
    public static string Delay(int n) => n <= 0 ? "" : $" reveal-delay-{Math.Min(n, 6)}";

    public static HomeViewModel Create(HomeSnapshot s, bool english, SeoVm seo)
    {
        LText P(string ar, string? en) => Bilingual.Pick(ar, en, english);

        var platforms = s.Platforms.Select(p => new PlatformCardVm(P(p.TitleAr, p.TitleEn), P(p.DescriptionAr, p.DescriptionEn),
            string.IsNullOrWhiteSpace(p.LinkUrl) ? null : p.LinkUrl, p.IconSvg)).ToList();
        var stats = s.Stats.Select(x => new StatVm(x.Value, x.Suffix, P(x.LabelAr, x.LabelEn),
            string.IsNullOrWhiteSpace(x.ShortLabelAr) ? P(x.LabelAr, x.LabelEn) : P(x.ShortLabelAr, x.ShortLabelEn),
            x.IconSvg)).ToList();
        var news = s.News.Select(n => new NewsCardVm(P(n.TitleAr, n.TitleEn), P(n.SummaryAr, n.SummaryEn),
            P(n.CategoryAr, n.CategoryEn), P(n.NewsDateAr, n.NewsDateEn),
            string.IsNullOrWhiteSpace(n.LinkUrl) ? null : n.LinkUrl)).ToList();
        var events = s.Events.Select(e => new EventCardVm(e.EventDay, P(e.EventMonthAr, e.EventMonthEn), P(e.TitleAr, e.TitleEn),
            P(e.DescriptionAr, e.DescriptionEn), P(e.LocationAr, e.LocationEn), P(e.TimeAr, e.TimeEn))).ToList();
        var partners = s.Partners.Select(p => P(p.Ar, p.En)).ToList();
        var faqs = s.Faqs.Select(f => new FaqVm(P(f.QuestionAr, f.QuestionEn), P(f.AnswerAr, f.AnswerEn))).ToList();

        // Sections that are always present + list sections that have at least one item.
        var visible = new HashSet<string>(StringComparer.Ordinal) { "hero", "about", "register", "contact" };
        if (platforms.Count > 0) visible.Add("platforms");
        if (stats.Count > 0) visible.Add("stats");
        if (news.Count > 0) visible.Add("news");
        if (events.Count > 0) visible.Add("events");
        if (partners.Count > 0) visible.Add("partners");
        if (faqs.Count > 0) visible.Add("faq");
        string[] conditional = ["platforms", "stats", "news", "events", "partners", "faq"];
        bool Shown(string href) => !(href.StartsWith('#') && conditional.Contains(href[1..]) && !visible.Contains(href[1..]));

        NavLinkVm Sub(NavSubRow x) => new(x.Href, P(x.TitleAr, x.TitleEn), x.IconSvg, []);
        var nav = s.Nav.Where(n => Shown(n.Href))
            .Select(n => new NavLinkVm(n.Href, P(n.TitleAr, n.TitleEn), null,
                n.HasDropdown ? n.SubItems.Where(x => Shown(x.Href)).Select(Sub).ToList() : []))
            .ToList();
        var footerLinks = s.Nav.Where(n => n.ShowInFooter && Shown(n.Href))
            .Select(n => new NavLinkVm(n.Href, P(n.TitleAr, n.TitleEn), null, [])).ToList();
        var footerPlatforms = s.Platforms.Where(p => p.ShowInFooter)
            .Select(p => new FooterPlatformVm(string.IsNullOrWhiteSpace(p.ShortTitleAr)
                ? P(p.TitleAr, p.TitleEn)
                : P(p.ShortTitleAr, string.IsNullOrWhiteSpace(p.ShortTitleEn) ? p.TitleEn : p.ShortTitleEn)))
            .ToList();

        return new HomeViewModel
        {
            IsEnglish = english,
            Content = new ContentVm(s.Content, english),
            NavLinks = nav,
            FooterLinks = footerLinks,
            Platforms = platforms,
            FooterPlatforms = visible.Contains("platforms") ? footerPlatforms : [],
            Stats = stats,
            News = news,
            Events = events,
            Partners = partners,
            Faqs = faqs,
            Interests = s.Interests.Select(i => new InterestChoiceVm(i.Id, P(i.NameAr, i.NameEn))).ToList(),
            Benefits = s.Benefits.Select(b => P(b.Ar, b.En)).ToList(),
            Socials = s.Socials.Where(x => SocialIcons.IsKnown(x.Network)).Select(x => new SocialVm(x.Network, x.Url)).ToList(),
            VisibleSections = visible,
            Seo = seo,
        };
    }
}

/// <summary>The whitelisted networks: brand name (aria-label, language-neutral) and SVG body. Code, not data.</summary>
public static class SocialIcons
{
    public static readonly IReadOnlyDictionary<string, (string Name, string Svg)> All = new Dictionary<string, (string, string)>
    {
        ["linkedin"] = ("LinkedIn", "<path d=\"M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z\"/><rect x=\"2\" y=\"9\" width=\"4\" height=\"12\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>"),
        ["twitter"] = ("Twitter", "<path d=\"M23 3a10.9 10.9 0 0 1-3.14 1.53 4.48 4.48 0 0 0-7.86 3v1A10.66 10.66 0 0 1 3 4s-4 9 5 13a11.64 11.64 0 0 1-7 2c9 5 20 0 20-11.5a4.5 4.5 0 0 0-.08-.83A7.72 7.72 0 0 0 23 3z\"/>"),
        ["instagram"] = ("Instagram", "<rect x=\"2\" y=\"2\" width=\"20\" height=\"20\" rx=\"5\" ry=\"5\"/><path d=\"M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z\"/><line x1=\"17.5\" y1=\"6.5\" x2=\"17.51\" y2=\"6.5\"/>"),
        ["youtube"] = ("YouTube", "<path d=\"M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19.1c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.43z\"/><polygon points=\"9.75 15.02 15.5 11.75 9.75 8.48 9.75 15.02\"/>"),
    };

    public static bool IsKnown(string network) => All.ContainsKey(network);
}

/// <summary>Badge + highlighted title + subtitle of a list section (Views/Home/_SectionHeader.cshtml).</summary>
public sealed record SectionHeaderVm(LText Badge, LText Title, LText Subtitle, string BadgeIconSvg);
