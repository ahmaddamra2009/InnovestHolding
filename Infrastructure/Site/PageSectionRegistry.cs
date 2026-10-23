using InnovestHolding.Infrastructure.Content;

namespace InnovestHolding.Infrastructure.Site;

/// <param name="Key">PageSections.SectionKey (stable, never shown).</param>
/// <param name="Label">The admin's name of the section.</param>
/// <param name="CanHide">False: the page's main content (its list or form) — the switch is refused.</param>
/// <param name="Movable">False: the section stays in place (e.g. the hero figures inside the hero).</param>
public sealed record SectionDef(string Key, string Label, bool CanHide = true, bool Movable = true);

/// <summary>
/// The sections (blocks) of each public page, in today's code order. AddAdminControl inserted one PageSections row per
/// entry with these keys and SortOrder 10, 20, … — a test pins the two lists together (the migration's are frozen).
/// The home hero is not a section (the page's only &lt;h1&gt;): it is neither movable nor hideable.
/// CONTRACT (C5 Dev 1): the keys are the contract; Dev 2 owns this file (labels, Arrange, rendering).
/// </summary>
public static class PageSectionRegistry
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<SectionDef>> Pages =
        new Dictionary<string, IReadOnlyList<SectionDef>>(StringComparer.Ordinal)
        {
            [PageKeys.Home] =
            [
                new("hero_stats", "أرقام الواجهة", Movable: false),
                new("about", "من نحن"),
                new("about_values", "قيمنا (داخل «من نحن»)", Movable: false),
                new("ecosystem", "المنظومة"),
                new("platforms", "الخدمات"),
                new("stats", "أرقام الأثر"),
                new("events", "الفعاليات"),
                new("news", "الأخبار"),
                new("partners", "الشركاء"),
                new("faq", "الأسئلة الشائعة"),
                new("register", "التسجيل السريع"),
                new("contact", "تواصل معنا"),
                new("cta_band", "شريط الدعوة"),
            ],
            [PageKeys.About] =
            [
                new("vision", "الرؤية والرسالة"),
                new("values", "القيم"),
                new("why", "لماذا نحن"),
                new("stats", "أرقام الأثر"),
                new("sectors", "القطاعات"),
                new("partners_in_success", "شركاء النجاح"),
                new("founder", "المؤسس"),
                new("cta_band", "شريط الدعوة"),
            ],
            [PageKeys.Ecosystem] =
            [
                new("units", "الوحدات"),
                new("pillars", "الركائز"),
                new("services", "الخدمات"),
                new("programs", "البرامج"),
                new("government", "الجهات الحكومية"),
                new("universities", "الجامعات"),
                new("cta_band", "شريط الدعوة"),
            ],
            [PageKeys.Contact] = [new("contact", "نموذج التواصل", CanHide: false), new("faq", "الأسئلة الشائعة")],
            [PageKeys.Forum] = [new("units", "الوحدات"), new("components", "مكونات المنتدى"), new("events", "الفعاليات"), new("cta_band", "شريط الدعوة")],
            [PageKeys.Network] = [new("units", "الوحدات"), new("investor_services", "خدمات المستثمرين"), new("steps", "خطوات الانضمام"), new("cta_band", "شريط الدعوة")],
            [PageKeys.NetworkInvestors] = [new("investor_services", "خدمات المستثمرين"), new("join_cta", "دعوة الانضمام")],
            [PageKeys.NetworkStartups] = [new("startup_services", "خدمات الشركات الناشئة"), new("sectors", "القطاعات"), new("join_cta", "دعوة الانضمام")],
            [PageKeys.NetworkOpportunities] = [new("list", "قائمة الفرص", CanHide: false), new("interest_form", "نموذج إبداء الاهتمام")],
            [PageKeys.ForumPartners] = [new("partners", "الشركاء", CanHide: false), new("packages", "باقات الرعاية"), new("partner_types", "أنواع الشراكة")],
            [PageKeys.TvSponsorship] = [new("packages", "باقات الرعاية"), new("form", "نموذج الرعاية", CanHide: false)],
            [PageKeys.ForumEvents] = [new("list", "القائمة", CanHide: false)],
            [PageKeys.ForumSpeakers] = [new("list", "القائمة", CanHide: false)],
            [PageKeys.News] = [new("list", "القائمة", CanHide: false)],
            [PageKeys.TvEpisodes] = [new("list", "القائمة", CanHide: false)],
            [PageKeys.TvGuests] = [new("list", "القائمة", CanHide: false)],
            [PageKeys.ForumRegistration] = [new("form", "النموذج", CanHide: false)],
            [PageKeys.NetworkJoin] = [new("form", "النموذج", CanHide: false)],
            [PageKeys.Tv] = [new("units", "الوحدات")],
        };

    /// <summary>The sections of a page (empty for a page without sections, e.g. privacy).</summary>
    public static IReadOnlyList<SectionDef> Of(string pageKey) => Pages.GetValueOrDefault(pageKey) ?? [];

    public static SectionDef? Find(string pageKey, string? sectionKey) =>
        Of(pageKey).FirstOrDefault(d => d.Key == sectionKey);

    /// <summary>
    /// Every registry section of a page with the admin's switch, in render order: stored order (SortOrder, Id) first,
    /// then the registry sections that have no stored row (visible, registry order). Unknown stored keys are ignored.
    /// A section that cannot be hidden (<see cref="SectionDef.CanHide"/> false) is always visible, whatever is stored.
    /// </summary>
    public static IReadOnlyList<(SectionDef Def, bool Visible)> Layout(SiteSnapshot s, string pageKey)
    {
        var defs = Of(pageKey);
        var stored = s.SectionsOf(pageKey);
        var result = new List<(SectionDef, bool)>(defs.Count);
        foreach (var row in stored)
            if (defs.FirstOrDefault(d => d.Key == row.Key) is { } def && !result.Exists(x => x.Item1.Key == def.Key))
                result.Add((def, row.IsVisible || !def.CanHide));
        foreach (var def in defs)
            if (!result.Exists(x => x.Item1.Key == def.Key))
                result.Add((def, true));
        return result;
    }

    /// <summary>The admin's switch of one section (true when it has no row or cannot be hidden).</summary>
    public static bool IsVisible(SiteSnapshot s, string pageKey, string sectionKey) =>
        Layout(s, pageKey).Where(x => x.Def.Key == sectionKey).Select(x => x.Visible).FirstOrDefault();

    /// <summary>
    /// The items to render: registry sections only, admin-visible, non-null (today's empty rule), in the order of
    /// <see cref="Layout"/>.
    /// </summary>
    public static IReadOnlyList<T> Arrange<T>(SiteSnapshot s, string pageKey, IEnumerable<(string Key, T? Item)> items) where T : class
    {
        var layout = Layout(s, pageKey);
        var given = items.Where(x => x.Item is not null).ToList();
        var result = new List<T>(given.Count);
        foreach (var (def, visible) in layout)
            if (visible)
                result.AddRange(given.Where(x => x.Key == def.Key).Select(x => x.Item!));
        return result;
    }

    // ── Counts (Content keys of ContentKeys, group "sections"; edited inline on the sections screen) ──

    /// <param name="Key">A Count key of Infrastructure/Content/ContentKeys (its rule, max and default live there).</param>
    /// <param name="Page">The page whose sections screen edits it.</param>
    /// <param name="Section">The row of that page it is shown on.</param>
    public sealed record SectionLimit(string Key, string Page, string Section, string Label)
    {
        private ContentKey Def => ContentKeys.Find(Key)!;

        /// <summary>The largest accepted value (12 for the C5 counts, 50 for home_news_limit).</summary>
        public int Max => Def.CountMax;

        /// <summary>The value used while the stored one is missing or invalid (= the C4 number).</summary>
        public int Default => ContentKeys.ParseCount(Def.Default, ContentKeys.HomeNewsLimitFallback, Max);
    }

    /// <summary>The counts an admin sets from the sections screen.</summary>
    public static readonly IReadOnlyList<SectionLimit> Limits =
    [
        new(ContentKeys.HeroStatsLimit, PageKeys.Home, "hero_stats", "عدد الأرقام في الواجهة"),
        new(ContentKeys.HomeEventsLimit, PageKeys.Home, "events", "عدد الفعاليات على الصفحة الرئيسية"),
        new(ContentKeys.HomeNewsLimit, PageKeys.Home, "news", "عدد الأخبار على الصفحة الرئيسية"),
        new(ContentKeys.ForumEventsLimit, PageKeys.Forum, "events", "عدد الفعاليات في صفحة المنتدى"),
    ];

    public static SectionLimit? FindLimit(string? key) => Limits.FirstOrDefault(l => l.Key == key);

    /// <summary>A valid count of <paramref name="limit"/>'s rule (ASCII digits, 1..Max), else null.</summary>
    public static int? ParseLimit(SectionLimit limit, string? value) =>
        int.TryParse(value?.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n)
        && n >= 1 && n <= limit.Max ? n : null;
}