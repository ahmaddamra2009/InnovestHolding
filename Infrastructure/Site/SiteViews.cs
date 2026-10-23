using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// Maps the cached snapshot to the display records of the public site, for one language. The ONE place where the
/// menus, breadcrumbs, cards and forms are assembled, so the home page and every inner page agree.
/// </summary>
public sealed class SiteViews(SiteSnapshot s, bool english)
{
    public SiteSnapshot Snapshot => s;
    public bool English => english;
    public ContentVm Content { get; } = new(s.Content, english);

    public LText P(string? ar, string? en) => Bilingual.Pick(ar, en, english);

    public string Url(string key) => CultureUrls.Page(key, english);

    /// <summary>The page's menu/breadcrumb label (falls back to the key if the row is missing).</summary>
    public LText Title(string key) => s.Page(key) is { } p ? P(p.TitleAr, p.TitleEn) : new LText(key, false);

    public LText Summary(string key) => s.Page(key) is { } p ? P(p.SummaryAr, p.SummaryEn) : LText.Empty;

    /// <summary>Shown in menus: the page's own switch and every ancestor's (a hidden hub hides its sub-pages too), and
    /// published (C5: an unpublished page is out of the menus, the drawer, the hub cards and the unit tabs).</summary>
    public bool InMenu(string key) =>
        SiteRoutes.Chain(key).All(p => p.Key == PageKeys.Home || (s.Page(p.Key)?.ShowInMenu ?? true)) && s.IsPublished(key);

    private IEnumerable<SitePage> MenuChildren(string key) =>
        SiteRoutes.Children(key).Where(p => InMenu(p.Key)).OrderBy(p => s.Page(p.Key)?.SortOrder ?? 0);

    // ── Shell ──

    /// <param name="leaf">A detail page: <paramref name="currentKey"/> is its list page (linked in the breadcrumb and
    /// active in the menu) and the leaf is the item's own last breadcrumb step.</param>
    public ShellVm Shell(string currentKey, string? baseUrl, string? otherLanguageUrl = null, BreadcrumbVm? leaf = null)
    {
        var chain = SiteRoutes.Chain(currentKey).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);

        MenuItemVm Item(SitePage p, int depth) => new(p.Key, Url(p.Key), Title(p.Key), Summary(p.Key), chain.Contains(p.Key),
            depth < 2 ? MenuChildren(p.Key).Select(c => Item(c, depth + 1)).ToList() : []);

        var menu = new List<MenuItemVm> { new(PageKeys.Home, Url(PageKeys.Home), Title(PageKeys.Home), LText.Empty, currentKey == PageKeys.Home, []) };
        menu.AddRange(MenuChildren(PageKeys.Home).Select(p => Item(p, 0)));

        var extra = s.Nav.Select(n => new LinkVm(n.Href, P(n.TitleAr, n.TitleEn))).ToList();
        var headerLinks = s.Nav.Where(n => n.ShowInHeader).Select(n => new LinkVm(n.Href, P(n.TitleAr, n.TitleEn))).ToList();

        var headerCta = HeaderCta();

        var crumbs = currentKey == PageKeys.Home
            ? []
            : SiteRoutes.Chain(currentKey).Select(p => new BreadcrumbVm(Title(p.Key), p.Key == currentKey && leaf is null ? null : Url(p.Key),
                baseUrl is null ? null : baseUrl + Url(p.Key))).ToList();
        if (leaf is not null) crumbs.Add(leaf);

        var logo = s.Image(SiteImages.LogoOnDark);
        return new ShellVm
        {
            IsEnglish = english,
            Content = Content,
            CurrentKey = currentKey,
            HomeUrl = Url(PageKeys.Home),
            OtherLanguageUrl = otherLanguageUrl ?? CultureUrls.Alternate(currentKey, english),
            Menu = menu,
            ExtraLinks = extra,
            HeaderLinks = headerLinks,
            HeaderCta = headerCta,
            Breadcrumbs = crumbs,
            Socials = Socials(),
            Brand = new BrandImagesVm(logo, s.Image(SiteImages.HeroImage), LogoAlt(logo)),
            Footer = FooterBuilder.Build(this),
        };
    }

    /// <summary>The logo's alt: the uploaded logo's alt text in the page language, else the site name (as before C5).</summary>
    public LText LogoAlt(MediaRef? logo)
    {
        var alt = logo is null ? LText.Empty : Bilingual.Optional(logo.AltAr, logo.AltEn, english);
        return alt.IsEmpty ? Content.Text("site_name") : alt;
    }

    /// <summary>The known networks (brand icons) and the admin's "custom" ones that have a label and a known icon.</summary>
    public IReadOnlyList<SocialVm> Socials() =>
        s.Socials.Where(x => SocialIcons.IsKnown(x.Network)
                             || (x.Network == Models.SocialLink.Custom && !string.IsNullOrWhiteSpace(x.Label) && Icons.IsKnown(x.IconKey)))
            .Select(x => SocialIcons.IsKnown(x.Network) ? new SocialVm(x.Network, x.Url) : new SocialVm(x.Network, x.Url, x.Label!.Trim(), x.IconKey))
            .ToList();

    // ── Links (C5: buttons the admin points to a page or to an https address) ──

    /// <summary>A page of the route table as a link titled with the page's title, or null when it is not published.</summary>
    public LinkVm? PageLink(string key) =>
        SiteRoutes.IsKey(key) && s.IsPublished(key) && Title(key) is { IsEmpty: false } title ? new LinkVm(Url(key), title) : null;

    /// <summary>The stored (trimmed) Arabic/neutral value of a key; null when the key has no row.</summary>
    private string? Stored(string key) => s.Content.TryGetValue(key, out var row) ? (row.Ar ?? "").Trim() : null;

    /// <summary>
    /// A button text override in the page language only: on the English page an Arabic-only override is NOT used (the
    /// target page's English title is, Dana Q10). Empty = no override.
    /// </summary>
    private LText Override(string? labelKey)
    {
        if (labelKey is null || !s.Content.TryGetValue(labelKey, out var row)) return LText.Empty;
        var value = english ? row.En : row.Ar;
        return string.IsNullOrWhiteSpace(value) ? LText.Empty : new LText(value.Trim(), false);
    }

    /// <summary>
    /// A target value → link: a page key → that page's URL in the page language (null when unpublished), titled by
    /// <paramref name="label"/> else the page title; an external https address → <paramref name="externalLabel"/>
    /// (required: no text → not rendered), with rel="noopener noreferrer". Anything else → null.
    /// </summary>
    private LinkVm? Resolve(string target, LText label, LText externalLabel)
    {
        if (SiteRoutes.IsKey(target))
        {
            if (!s.IsPublished(target)) return null;
            var title = label.IsEmpty ? Title(target) : label;
            return title.IsEmpty ? null : new LinkVm(Url(target), title);
        }
        if (Infrastructure.Content.ContentKeys.IsExternalLink(target) && !externalLabel.IsEmpty)
            return new LinkVm(target.Trim(), externalLabel, External: true);
        return null;
    }

    /// <summary>
    /// The button <paramref name="name"/>: target <c>{name}_link</c> (a page key or https; missing or invalid → the
    /// registry default), text <c>{name}_label</c> (empty → the target page's title in the page language). An empty
    /// target means the default, or no button for a key the registry allows to be empty without a default meaning
    /// (cta_band_button2). Null = render nothing (unpublished target, external link without text).
    /// </summary>
    public LinkVm? Link(string name)
    {
        var linkKey = name + "_link";
        var def = ContentKeys.Find(linkKey) ?? throw new ArgumentException($"Unknown link {name}.", nameof(name));
        var stored = Stored(linkKey);
        var noneWhenEmpty = def.AllowEmpty && def.Default is not null; // "empty = no button"
        string? target = stored switch
        {
            null => def.Default,
            "" => noneWhenEmpty ? null : def.Default,
            _ => ContentKeys.IsLink(stored) ? stored : def.Default,
        };
        if (target is null) return null;
        var labelKey = def.LabelKey;
        var external = labelKey is null || !s.Content.TryGetValue(labelKey, out var row) ? LText.Empty : P(row.Ar, row.En);
        return Resolve(target, Override(labelKey), external);
    }

    /// <summary>The header/drawer button: header_cta_page (page or https; invalid → Join the Network) titled
    /// header_cta_label (English falls back to Arabic, as before C5). Null when the page is unpublished.</summary>
    public LinkVm? HeaderCta()
    {
        var stored = Stored("header_cta_page");
        var target = ContentKeys.IsLink(stored) ? stored! : PageKeys.NetworkJoin;
        var label = Content.Text("header_cta_label");
        return Resolve(target, label, label);
    }

    /// <summary>The hero's main button: hero_cta_main_link/label; while the link is empty it is the header button
    /// (with the hero text when one is set in the page language).</summary>
    public LinkVm? HeroMainCta()
    {
        var stored = Stored("hero_cta_main_link");
        var label = Override("hero_cta_main_label");
        if (!ContentKeys.IsLink(stored))
        {
            var header = HeaderCta();
            return header is null || label.IsEmpty ? header : header with { Title = label };
        }
        return Link("hero_cta_main");
    }

    /// <summary>The hero's second button: text hero_cta_primary (as before C5), target hero_cta_secondary_link
    /// (default the Ecosystem page).</summary>
    public LinkVm? HeroSecondaryCta()
    {
        var stored = Stored("hero_cta_secondary_link");
        var target = ContentKeys.IsLink(stored) ? stored! : PageKeys.Ecosystem;
        var label = Content.Text("hero_cta_primary");
        return Resolve(target, label, label);
    }

    /// <summary>The «more» button of the home About section (default: the About page, titled with its title).</summary>
    public LinkVm? AboutMore() => Link("about_more");

    /// <summary>The register action of the event rows and pages (default: Forum registration).</summary>
    public LinkVm? EventsAction() => Link("events_action");

    /// <summary>The action of the TV lists' empty states and of episode/guest pages (default: TV sponsorship).</summary>
    public LinkVm? TvAction() => Link("tv_action");

    /// <summary>
    /// The empty-state button of a page: the page's own EmptyActionPage when set to a page key, else
    /// <paramref name="fallback"/> (the code default). Null when the chosen page is unpublished.
    /// </summary>
    public LinkVm? EmptyAction(string pageKey, LinkVm? fallback) =>
        s.Page(pageKey)?.EmptyActionPage is { } key && SiteRoutes.IsKey(key.Trim()) ? PageLink(key.Trim()) : fallback;

    /// <summary>The 404 page's suggestions: notfound_pages (published pages only, in order, at most 6); none valid → the
    /// default three (Home, Ecosystem, Contact).</summary>
    public IReadOnlyList<LinkVm> NotFoundLinks()
    {
        IReadOnlyList<LinkVm> From(IEnumerable<string> keys) =>
            keys.Distinct(StringComparer.Ordinal).Take(ContentKeys.MaxPageList).Select(PageLink).OfType<LinkVm>().ToList();
        var links = From(ContentKeys.PageList(Stored(ContentKeys.NotFoundPages)));
        return links.Count > 0 ? links : From(ContentKeys.PageList(ContentKeys.Find(ContentKeys.NotFoundPages)!.Default));
    }

    /// <summary>The privacy policy link (footer bottom bar, consent boxes): only while the page is published and its
    /// Arabic body is not empty (Rami Q6).</summary>
    public LinkVm? PrivacyLink() =>
        s.Page(PageKeys.Privacy) is { } p && !string.IsNullOrWhiteSpace(p.BodyAr) ? PageLink(PageKeys.Privacy) : null;

    // ── Counts and icons (C5 content keys; missing or invalid → the registry default = the value before C5) ──

    /// <summary>A count key of the registry (hero_stats_limit, home_events_limit, forum_events_limit, home_news_limit…).</summary>
    public int Count(string key) => ContentKeys.Count(s.Content, key);

    /// <summary>The SVG body of an icon key (vision_icon, events_icon…): the admin's choice when it is a known icon,
    /// else the registry default.</summary>
    public string Icon(string key) =>
        Icons.Svg(Stored(key)) ?? Icons.Svg(ContentKeys.Find(key)?.Default) ?? throw new ArgumentException($"No icon for {key}.", nameof(key));

    // ── Lists ──

    public IReadOnlyList<ItemVm> Items(string group) =>
        s.ItemsOf(group).Select(i => new ItemVm(P(i.TextAr, i.TextEn), P(i.DescriptionAr, i.DescriptionEn), Icons.Svg(i.IconKey))).ToList();

    public ItemsBlock? ItemsSection(string group, bool compact = false, bool dark = false)
    {
        var g = ContentItemGroups.Find(group)!;
        var items = Items(group);
        return items.Count == 0 ? null : new ItemsBlock(group, Content.Text(g.TitleKey), g.IntroKey is null ? LText.Empty : Content.Text(g.IntroKey), items, compact, dark);
    }

    /// <summary>The three units (or a hub's sub-pages) as cards; each unit lists its visible sub-pages as chips.</summary>
    public IReadOnlyList<CardVm> Cards(string parentKey, bool withChips) =>
        MenuChildren(parentKey).Select((p, i) => new CardVm(i + 1, Title(p.Key), Summary(p.Key), Url(p.Key),
            withChips ? MenuChildren(p.Key).Select(c => new LinkVm(Url(c.Key), Title(c.Key))).ToList() : [],
            Image(s.Page(p.Key)?.Image, Title(p.Key)))).ToList();

    /// <summary>The service cards: the icon of the admin's IconKey (whitelist), else the migration-owned markup of the
    /// row (C4 rows → unchanged), else none (the view draws the brand bullet).</summary>
    public IReadOnlyList<ServiceVm> Services() =>
        s.Platforms.Select(p => new ServiceVm(P(p.TitleAr, p.TitleEn), Lines(P(p.DescriptionAr, p.DescriptionEn)),
            Icons.Svg(p.IconKey) ?? (string.IsNullOrWhiteSpace(p.IconSvg) ? null : p.IconSvg),
            string.IsNullOrWhiteSpace(p.LinkUrl) ? null : p.LinkUrl)).ToList();

    /// <summary>One bullet per non-empty line (each keeps the fallback marker of its text).</summary>
    public static IReadOnlyList<LText> Lines(LText text) =>
        text.Value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0)
            .Select(l => new LText(l, text.IsFallback)).ToList();

    /// <summary>Paragraphs of a plain-text body: a blank line starts a new paragraph.</summary>
    public static IReadOnlyList<LText> Paragraphs(LText text) =>
        text.Value.Replace("\r\n", "\n").Split("\n\n").Select(l => l.Trim()).Where(l => l.Length > 0)
            .Select(l => new LText(l, text.IsFallback)).ToList();

    public IReadOnlyList<StatVm> Stats() =>
        s.Stats.Select(x => new StatVm(x.Value, x.Suffix, P(x.LabelAr, x.LabelEn),
            ShortLabel(x.LabelAr, x.LabelEn, x.ShortLabelAr, x.ShortLabelEn), x.IconSvg)).ToList();

    /// <summary>
    /// The short label of a figure: the short label in the page's language, else the full label in that language, and
    /// only then the other language (an English page with an English label never shows the Arabic short label).
    /// </summary>
    private LText ShortLabel(string labelAr, string? labelEn, string? shortAr, string? shortEn)
    {
        if (string.IsNullOrWhiteSpace(shortAr)) return P(labelAr, labelEn);
        if (english && string.IsNullOrWhiteSpace(shortEn) && !string.IsNullOrWhiteSpace(labelEn)) return P(labelAr, labelEn);
        return P(shortAr, shortEn);
    }

    /// <summary>An uploaded image for the page language; the alt text falls back to <paramref name="fallbackAlt"/>
    /// (the item's title) when the admin left it empty.</summary>
    public ImageVm? Image(MediaRef? media, LText fallbackAlt)
    {
        if (media is null) return null;
        var alt = P(media.AltAr, media.AltEn);
        return new ImageVm(media.Url, media.Width, media.Height, alt.IsEmpty ? fallbackAlt : alt);
    }

    public IReadOnlyList<NewsCardVm> NewsCards(IEnumerable<NewsRow> rows) =>
        rows.Select(n =>
        {
            var title = P(n.TitleAr, n.TitleEn);
            return new NewsCardVm(title, P(n.SummaryAr, n.SummaryEn), P(n.CategoryAr, n.CategoryEn),
                P(n.NewsDateAr, n.NewsDateEn), CultureUrls.Detail(DetailKeys.NewsItem, n.Slug, english), Image(n.Image, title));
        }).ToList();

    public IReadOnlyList<EventCardVm> EventCards(IEnumerable<EventRow> rows, DateTimeOffset now) =>
        rows.Select(e =>
        {
            var title = P(e.TitleAr, e.TitleEn);
            var summary = P(e.SummaryAr, e.SummaryEn);
            var (day, month) = e.StartsAt is { } start
                ? (start.Day, new LText(EventDates.Month(start, english), false))
                : (e.EventDay, P(e.EventMonthAr, e.EventMonthEn));
            var time = P(e.TimeAr, e.TimeEn);
            if (time.IsEmpty && e.StartsAt is { } s0) time = new LText(EventDates.TimeRange(s0, e.EndsAt, english), false);
            return new EventCardVm(day, month, title, summary.IsEmpty ? P(e.DescriptionAr, e.DescriptionEn) : summary,
                P(e.LocationAr, e.LocationEn), time, CultureUrls.Detail(DetailKeys.ForumEvent, e.Slug, english), Image(e.Image, title),
                EventQueries.IsPast(e.StartsAt, e.EndsAt, now), e.Status);
        }).ToList();

    public IReadOnlyList<PartnerVm> Partners(bool homeOnly = false) =>
        s.Partners.Where(p => !homeOnly || p.ShowOnHome).Select(p =>
        {
            var name = P(p.NameAr, p.NameEn);
            return new PartnerVm(name, Image(p.Logo, name), SiteUrlOrNull(p.Url), p.GroupId);
        }).ToList();

    /// <summary>The Forum Partners page: one section per active group (in order), then the partners with no group.</summary>
    public IReadOnlyList<PartnerGroupVm> PartnerGroups()
    {
        var all = Partners();
        var groups = s.PartnerGroups.Select(g => new PartnerGroupVm(P(g.NameAr, g.NameEn), all.Where(p => p.GroupId == g.Id).ToList()))
            .Where(g => g.Partners.Count > 0).ToList();
        var known = s.PartnerGroups.Select(g => g.Id).ToHashSet();
        var rest = all.Where(p => p.GroupId is null || !known.Contains(p.GroupId.Value)).ToList();
        if (rest.Count > 0) groups.Add(new PartnerGroupVm(LText.Empty, rest));
        return groups;
    }

    private static string? SiteUrlOrNull(string? url) => Seo.SiteUrl.IsHttpsUrl(url) ? url!.Trim() : null;

    public IReadOnlyList<FaqVm> Faqs() => s.Faqs.Select(f => new FaqVm(P(f.QuestionAr, f.QuestionEn), P(f.AnswerAr, f.AnswerEn))).ToList();

    /// <summary>Badge + title + subtitle of a section; the badge icon is <c>{prefix}_icon</c> when the registry has that
    /// key (C5), else <paramref name="iconSvg"/>.</summary>
    public SectionHeaderVm Header(string prefix, string iconSvg) =>
        new(Content.Text(prefix + "_badge"), Content.Text(prefix + "_title"), Content.Text(prefix + "_subtitle"),
            ContentKeys.Find(prefix + "_icon") is null ? iconSvg : Icon(prefix + "_icon"));

    /// <summary>A home section header whose badge icon is the content key <c>{prefix}_icon</c>.</summary>
    public SectionHeaderVm Header(string prefix) => Header(prefix, Icon(prefix + "_icon"));

    // ── Forms ──

    public RegisterFormVm RegisterForm(string idPrefix) => new(
        CultureUrls.Form("register", english),
        s.Interests.Select(i => new InterestChoiceVm(i.Id, P(i.NameAr, i.NameEn))).ToList(),
        s.Benefits.Select(b => P(b.Ar, b.En)).ToList(),
        Content.Text("register_badge"), Content.Text("register_title"), Content.Text("register_text"),
        Content.Text("cta_register"), idPrefix) { BadgeIconSvg = Icon("register_icon") };

    public ContactFormVm ContactForm(string idPrefix) => new(
        CultureUrls.Form("contact", english),
        Content.Text("contact_intro"), Content.Text("contact_address"), Content.Text("contact_hours"),
        Content.Neutral("contact_phone"), Content.Neutral("contact_email"), Content.Neutral("contact_whatsapp"),
        Content.Neutral("contact_website"), Socials(), idPrefix);

    /// <summary>The call-to-action band: two buttons (cta_band_button1/2: default Join the Network and Forum
    /// registration, titled with the page titles); the second one is left out when its target is emptied.</summary>
    public CtaBlock CtaBand() => new(Content.Text("cta_band_title"), Content.Text("cta_band_text"),
        new[] { Link("cta_band_button1"), Link("cta_band_button2") }.OfType<LinkVm>().ToList());

    /// <summary>The services-grid and FAQ badge glyphs (C5: entries of <see cref="Icons"/>, keys grid / question).</summary>
    public static readonly string BadgeGrid = Icons.Svg("grid")!;
    public static readonly string BadgeQuestion = Icons.Svg("question")!;
}
