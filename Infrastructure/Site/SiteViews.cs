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

    /// <summary>Shown in menus: the page's own switch and every ancestor's (a hidden hub hides its sub-pages too).</summary>
    public bool InMenu(string key) =>
        SiteRoutes.Chain(key).All(p => p.Key == PageKeys.Home || (s.Page(p.Key)?.ShowInMenu ?? true));

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

        var ctaKey = Content.Neutral("header_cta_page");
        var cta = new LinkVm(Url(SiteRoutes.IsKey(ctaKey) ? ctaKey : PageKeys.NetworkJoin), Content.Text("header_cta_label"));

        var crumbs = currentKey == PageKeys.Home
            ? []
            : SiteRoutes.Chain(currentKey).Select(p => new BreadcrumbVm(Title(p.Key), p.Key == currentKey && leaf is null ? null : Url(p.Key),
                baseUrl is null ? null : baseUrl + Url(p.Key))).ToList();
        if (leaf is not null) crumbs.Add(leaf);

        return new ShellVm
        {
            IsEnglish = english,
            Content = Content,
            CurrentKey = currentKey,
            HomeUrl = Url(PageKeys.Home),
            OtherLanguageUrl = otherLanguageUrl ?? CultureUrls.Alternate(currentKey, english),
            Menu = menu,
            ExtraLinks = extra,
            Cta = cta,
            Breadcrumbs = crumbs,
            Socials = Socials(),
        };
    }

    public IReadOnlyList<SocialVm> Socials() =>
        s.Socials.Where(x => SocialIcons.IsKnown(x.Network)).Select(x => new SocialVm(x.Network, x.Url)).ToList();

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

    public IReadOnlyList<ServiceVm> Services() =>
        s.Platforms.Select(p => new ServiceVm(P(p.TitleAr, p.TitleEn), Lines(P(p.DescriptionAr, p.DescriptionEn)), p.IconSvg,
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

    public SectionHeaderVm Header(string prefix, string iconSvg) =>
        new(Content.Text(prefix + "_badge"), Content.Text(prefix + "_title"), Content.Text(prefix + "_subtitle"), iconSvg);

    // ── Forms ──

    public RegisterFormVm RegisterForm(string idPrefix) => new(
        CultureUrls.Form("register", english),
        s.Interests.Select(i => new InterestChoiceVm(i.Id, P(i.NameAr, i.NameEn))).ToList(),
        s.Benefits.Select(b => P(b.Ar, b.En)).ToList(),
        Content.Text("register_badge"), Content.Text("register_title"), Content.Text("register_text"),
        Content.Text("cta_register"), idPrefix);

    public ContactFormVm ContactForm(string idPrefix) => new(
        CultureUrls.Form("contact", english),
        Content.Text("contact_intro"), Content.Text("contact_address"), Content.Text("contact_hours"),
        Content.Neutral("contact_phone"), Content.Neutral("contact_email"), Content.Neutral("contact_whatsapp"),
        Content.Neutral("contact_website"), Socials(), idPrefix);

    public CtaBlock CtaBand() => new(Content.Text("cta_band_title"), Content.Text("cta_band_text"),
        [new LinkVm(Url(PageKeys.NetworkJoin), Title(PageKeys.NetworkJoin)), new LinkVm(Url(PageKeys.ForumRegistration), Title(PageKeys.ForumRegistration))]);

    public const string BadgeGrid = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\"/>";
    public const string BadgeQuestion = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/>";
}
