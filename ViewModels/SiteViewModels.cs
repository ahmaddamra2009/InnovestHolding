using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;

namespace InnovestHolding.ViewModels;

// Display records of the public site, already resolved for the page language (fallback applied).

public sealed record LinkVm(string Href, LText Title, bool Active = false);

/// <summary>A menu entry (the header, the drawer and the footer read the same tree).</summary>
public sealed record MenuItemVm(string Key, string Href, LText Title, LText Summary, bool Active, IReadOnlyList<MenuItemVm> Children);

/// <summary>A breadcrumb step; <see cref="Href"/> is null for the current page.</summary>
public sealed record BreadcrumbVm(LText Title, string? Href, string? AbsoluteUrl);

/// <summary>Everything the header, the drawer, the breadcrumb and the footer need (Views/Shared/_Header, _Footer).</summary>
public sealed class ShellVm
{
    public required bool IsEnglish { get; init; }
    public required ContentVm Content { get; init; }
    public required string CurrentKey { get; init; }
    public required string HomeUrl { get; init; }
    public required string OtherLanguageUrl { get; init; }
    public required IReadOnlyList<MenuItemVm> Menu { get; init; }
    public required IReadOnlyList<LinkVm> ExtraLinks { get; init; }
    public required LinkVm Cta { get; init; }
    public required IReadOnlyList<BreadcrumbVm> Breadcrumbs { get; init; }
    public required IReadOnlyList<SocialVm> Socials { get; init; }

    /// <summary>The uploaded brand images (null = the bundled files / the drawn triangle).</summary>
    public BrandImagesVm Brand { get; init; } = new(null, null);

    /// <summary>The header starts transparent over a dark hero (home) instead of solid.</summary>
    public bool OverHero => CurrentKey == Infrastructure.Site.PageKeys.Home;

    public string OtherLang => IsEnglish ? Cultures.ArabicName : Cultures.EnglishName;
}

/// <summary>Everything the &lt;head&gt; needs. Absolute URLs are null when no base URL is known (then omitted).</summary>
public sealed record SeoVm(
    string Lang, string Title, string Description, string SiteName,
    string? Canonical, string? AlternateAr, string? AlternateEn,
    string OgLocale, string? OgLocaleAlternate, string? OgImage, string OgImageAlt, string? JsonLd,
    RobotsDecision Robots = RobotsDecision.Index, string OgType = "website", string TwitterCard = "summary",
    int? OgImageWidth = null, int? OgImageHeight = null)
{
    /// <summary>The robots meta content for a noindex page, null when indexable.</summary>
    public string? RobotsMeta => Robots switch
    {
        RobotsDecision.Index => null,
        RobotsDecision.NoIndexFollow => RobotsHeader.FollowValue,
        _ => RobotsHeader.Value,
    };
}

/// <summary>SiteContents accessor for the page language (one fallback rule: <see cref="Bilingual"/>).</summary>
public sealed class ContentVm(IReadOnlyDictionary<string, TextPair> content, bool english)
{
    private TextPair? Row(string key) => content.GetValueOrDefault(key);

    public bool IsEnglish => english;

    /// <summary>English with fallback to Arabic (Arabic on the Arabic page).</summary>
    public LText Text(string key) => Row(key) is { } r ? Bilingual.Pick(r.Ar, r.En, english) : LText.Empty;

    /// <summary>No fallback (keys flagged Optional in the registry, e.g. hero_kicker).</summary>
    public LText Optional(string key) => Row(key) is { } r ? Bilingual.Optional(r.Ar, r.En, english) : LText.Empty;

    /// <summary>Language-neutral value (phone, email, URLs).</summary>
    public string Neutral(string key) => Row(key)?.Ar ?? "";
}

/// <summary>The whitelisted networks: brand name (aria-label, language-neutral) and SVG body. Code, not data.</summary>
public static class SocialIcons
{
    public static readonly IReadOnlyDictionary<string, (string Name, string Svg)> All = new Dictionary<string, (string, string)>
    {
        ["linkedin"] = ("LinkedIn", "<path d=\"M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z\"/><rect x=\"2\" y=\"9\" width=\"4\" height=\"12\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>"),
        ["twitter"] = ("X", "<path d=\"M4 4l16 16\"/><path d=\"M20 4 4 20\"/>"),
        ["instagram"] = ("Instagram", "<rect x=\"2\" y=\"2\" width=\"20\" height=\"20\" rx=\"5\" ry=\"5\"/><path d=\"M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z\"/><line x1=\"17.5\" y1=\"6.5\" x2=\"17.51\" y2=\"6.5\"/>"),
        ["youtube"] = ("YouTube", "<path d=\"M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19.1c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.43z\"/><polygon points=\"9.75 15.02 15.5 11.75 9.75 8.48 9.75 15.02\"/>"),
        ["facebook"] = ("Facebook", "<path d=\"M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z\"/>"),
        ["tiktok"] = ("TikTok", "<path d=\"M9 12a4 4 0 1 0 4 4V2a5 5 0 0 0 5 5\"/>"),
        ["linktree"] = ("Linktree", "<path d=\"M12 22v-8\"/><path d=\"m5 9 7 5 7-5\"/><path d=\"m7 4 5 5 5-5\"/><path d=\"M4 14h16\"/>"),
    };

    public static bool IsKnown(string network) => All.ContainsKey(network);
}

public sealed record SocialVm(string Network, string Url);

/// <summary>The site logo (on a dark background) and the home hero image when the admin uploaded them.</summary>
public sealed record BrandImagesVm(Infrastructure.Content.MediaRef? LogoOnDark, Infrastructure.Content.MediaRef? Hero);

// ── Page blocks: a page is its header (title, intro, body) + an ordered list of blocks (Views/Site/_Blocks) ──

public abstract record BlockVm;

/// <summary>A titled section of ContentItems (cards with an icon, or a check list).</summary>
public sealed record ItemsBlock(string Group, LText Title, LText Intro, IReadOnlyList<ItemVm> Items, bool Compact, bool Dark = false) : BlockVm;
public sealed record ItemVm(LText Text, LText Description, string? IconSvg);

/// <summary>Links to pages: the three units (with their sub-pages as chips) or a hub's sub-pages.</summary>
public sealed record CardsBlock(LText Title, LText Intro, IReadOnlyList<CardVm> Cards, bool Dark) : BlockVm;
public sealed record CardVm(int Number, LText Title, LText Summary, string Href, IReadOnlyList<LinkVm> Chips, ImageVm? Image = null);

public sealed record VisionBlock(IReadOnlyList<(LText Title, LText Text, string IconSvg)> Cards) : BlockVm;

public sealed record ServicesBlock(SectionHeaderVm Header, IReadOnlyList<ServiceVm> Services, LText ReadMore) : BlockVm;
public sealed record ServiceVm(LText Title, IReadOnlyList<LText> Lines, string IconSvg, string? LinkUrl);

public sealed record StatsBlock(LText Title, LText Intro, IReadOnlyList<StatVm> Stats) : BlockVm;

public sealed record EventsBlock(IReadOnlyList<EventCardVm> Events, PagerVm? Pager, LinkVm? Action) : BlockVm;
public sealed record NewsBlock(IReadOnlyList<NewsCardVm> News, PagerVm? Pager, LText ReadMore) : BlockVm;
public sealed record PartnersBlock(LText Title, IReadOnlyList<PartnerGroupVm> Groups) : BlockVm;
public sealed record PartnerVm(LText Name, ImageVm? Logo, string? Url, int? GroupId);
/// <summary>A heading (empty for the partners with no group) and its partners.</summary>
public sealed record PartnerGroupVm(LText Name, IReadOnlyList<PartnerVm> Partners);

/// <summary>The admin-managed "coming soon" state of a page with no content yet, with one useful next action.</summary>
public sealed record EmptyBlock(LText Text, LinkVm? Action) : BlockVm;

public sealed record RegisterBlock(RegisterFormVm Form) : BlockVm;
public sealed record ContactBlock(ContactFormVm Form) : BlockVm;
public sealed record FaqBlock(SectionHeaderVm Header, IReadOnlyList<FaqVm> Faqs) : BlockVm;
public sealed record FounderBlock(LText Heading, LText Name, LText Title, LText Message, ImageVm? Photo = null) : BlockVm;
public sealed record CtaBlock(LText Title, LText Text, IReadOnlyList<LinkVm> Buttons) : BlockVm;

public sealed record PagerVm(int Page, int PageCount, string BaseUrl)
{
    public string UrlFor(int page) => page <= 1 ? BaseUrl : $"{BaseUrl}?page={page}";
}

/// <summary>The quick-registration form (home and the Forum registration page).</summary>
public sealed record RegisterFormVm(string Action, IReadOnlyList<InterestChoiceVm> Interests, IReadOnlyList<LText> Benefits,
    LText Badge, LText Title, LText Text, LText SubmitLabel, string IdPrefix);

/// <summary>The contact block: details from the content keys and the form.</summary>
public sealed record ContactFormVm(string Action, LText Intro, LText Address, LText Hours, string Phone, string Email,
    string WhatsApp, string Website, IReadOnlyList<SocialVm> Socials, string IdPrefix);

/// <summary>A public page other than the home page (Views/Site/Page.cshtml).</summary>
public sealed class SitePageVm
{
    public required ShellVm Shell { get; init; }
    public required SeoVm Seo { get; init; }
    public required string Key { get; init; }
    public required LText Title { get; init; }
    public required LText Intro { get; init; }
    public required IReadOnlyList<LText> Body { get; init; }

    /// <summary>The unit's sub-pages (tab bar) when the page belongs to the Forum, the Network or the TV Program.</summary>
    public required IReadOnlyList<LinkVm> UnitTabs { get; init; }
    public LText UnitTitle { get; init; }

    public required IReadOnlyList<BlockVm> Blocks { get; init; }
}

/// <summary>The branded 404 page.</summary>
public sealed record NotFoundVm(ShellVm Shell, SeoVm Seo, IReadOnlyList<LinkVm> Links);

// Records shared with the home page.
public sealed record StatVm(int Value, string Suffix, LText Label, LText ShortLabel, string IconSvg);
public sealed record NewsCardVm(LText Title, LText Summary, LText Category, LText Date, string Href, ImageVm? Image);
public sealed record EventCardVm(int Day, LText Month, LText Title, LText Description, LText Location, LText Time,
    string Href, ImageVm? Image, bool Past, Models.EventStatus Status);

/// <summary>An uploaded image, ready for an &lt;img&gt; (src, intrinsic size, alt in the page language).</summary>
public sealed record ImageVm(string Src, int Width, int Height, LText Alt);

/// <summary>One line of a detail page's facts row (date, time, place…): an icon key of Infrastructure/Content/Icons.</summary>
public sealed record FactVm(string Icon, LText Label, LText Text, string? Href = null);

/// <summary>A news item or an event page (Views/Site/Detail.cshtml).</summary>
public sealed class DetailPageVm
{
    /// <summary>Set by the controller's Detail() helper (breadcrumb with the item as its last step).</summary>
    public ShellVm Shell { get; set; } = default!;
    public required SeoVm Seo { get; init; }
    public required string Kind { get; init; }
    public required LText Title { get; init; }
    public required LText Lead { get; init; }
    public required IReadOnlyList<LText> Body { get; init; }
    public ImageVm? Image { get; init; }
    /// <summary>Category of a news item, status of an event (past, cancelled, postponed).</summary>
    public LText Badge { get; init; }
    public IReadOnlyList<FactVm> Facts { get; init; } = [];
    public IReadOnlyList<LinkVm> Actions { get; init; } = [];
    /// <summary>A line above the text (e.g. "This event has ended").</summary>
    public LText Notice { get; init; }
    /// <summary>The English page shows the Arabic text (no English yet).</summary>
    public bool ArabicOnly { get; init; }
    public required LinkVm Back { get; init; }

    /// <summary>A person's page: the image is a portrait (round crop beside the text) instead of a wide figure.</summary>
    public bool Portrait { get; init; }

    /// <summary>The episode's video (click-to-load player or a plain link).</summary>
    public VideoVm? Video { get; init; }

    /// <summary>Linked items under the text: an event's speakers, a speaker's events, an episode's guests…</summary>
    public IReadOnlyList<RelatedVm> Related { get; init; } = [];

    /// <summary>The person's profiles elsewhere (LinkedIn, X), language-neutral names.</summary>
    public IReadOnlyList<SocialVm> Profiles { get; init; } = [];
}

/// <summary>
/// An episode's video. With a YouTube id the page shows a poster and a play button, and only the click loads the
/// privacy-enhanced player (no request to YouTube before it). <see cref="LinkUrl"/> is the stored https address, shown as a
/// plain link (beside the player, or alone when the address is not a YouTube video).
/// </summary>
public sealed record VideoVm(string? YouTubeId, string? LinkUrl, ImageVm? Poster, LText Title);

/// <summary>A titled group of linked cards on a detail page.</summary>
public sealed record RelatedVm(LText Title, IReadOnlyList<EntityCardVm> Cards, bool People);

/// <summary>
/// A card of the ecosystem lists (speaker, guest, episode, opportunity): image, a small kicker above the title, the
/// title (the link), a subtitle and up to a few chips.
/// </summary>
public sealed record EntityCardVm(LText Title, string Href, ImageVm? Image, LText Kicker, LText Subtitle, LText Summary,
    IReadOnlyList<LText> Chips, LText Badge = default);

/// <summary>A paged grid of cards; <see cref="People"/> renders round portraits instead of wide images.</summary>
public sealed record EntityGridBlock(string Kind, IReadOnlyList<EntityCardVm> Cards, PagerVm? Pager, bool People) : BlockVm;

/// <summary>The sponsorship packages of a unit.</summary>
public sealed record PackagesBlock(LText Title, IReadOnlyList<PackageVm> Packages) : BlockVm;
public sealed record PackageVm(LText Title, LText Price, IReadOnlyList<LText> Benefits);
public sealed record FaqVm(LText Question, LText Answer);
public sealed record InterestChoiceVm(int Id, LText Name);

/// <summary>Badge + highlighted title + subtitle of a section (Views/Shared/_SectionHeader.cshtml).</summary>
public sealed record SectionHeaderVm(LText Badge, LText Title, LText Subtitle, string BadgeIconSvg);
