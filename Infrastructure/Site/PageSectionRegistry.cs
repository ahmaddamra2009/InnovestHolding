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
    /// The items to render: registry sections only, admin-visible, non-null (today's empty rule), in stored order; a
    /// section without a stored row comes at the end, in registry order; unknown stored keys are ignored.
    /// </summary>
    public static IReadOnlyList<T> Arrange<T>(SiteSnapshot s, string pageKey, IEnumerable<(string Key, T? Item)> items) where T : class
    {
        var stored = s.SectionsOf(pageKey);
        var defs = Of(pageKey).Select(d => d.Key).ToList();
        int Rank(string key)
        {
            for (var i = 0; i < stored.Count; i++)
                if (stored[i].Key == key) return i;
            return stored.Count + defs.IndexOf(key);
        }
        bool Visible(string key) => stored.FirstOrDefault(r => r.Key == key)?.IsVisible ?? true;
        return items
            .Where(x => x.Item is not null && defs.Contains(x.Key) && Visible(x.Key))
            .OrderBy(x => Rank(x.Key))
            .Select(x => x.Item!)
            .ToList();
    }
}
