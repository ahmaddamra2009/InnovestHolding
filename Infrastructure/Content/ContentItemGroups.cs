namespace InnovestHolding.Infrastructure.Content;

/// <param name="Key">ContentItems.Group.</param>
/// <param name="Label">Arabic admin label.</param>
/// <param name="TitleKey">SiteContents key of the section heading on the public page.</param>
/// <param name="IntroKey">SiteContents key of the optional lead sentence (null: none).</param>
/// <param name="UsesDescription">The admin form shows the description fields.</param>
/// <param name="UsesIcon">The admin form shows the icon picker.</param>
/// <param name="FormOptions">The choices of a public form's select (not a section of a page: no heading keys).</param>
/// <param name="Kind">How the rows read (F10): a plain list, figures (Text = the figure as displayed, Description = its
/// label) or bullet cards (Text = the card title, Description = one bullet per line).</param>
/// <param name="Numbered">The public block numbers the rows 01, 02, … (F10: goals, sectors).</param>
/// <param name="NoteKey">SiteContents key of an optional closing paragraph under the list (F10), else null.</param>
public sealed record ContentItemGroup(string Key, string Label, string TitleKey, string? IntroKey, bool UsesDescription, bool UsesIcon,
    bool FormOptions = false, ContentItemKind Kind = ContentItemKind.List, bool Numbered = false, string? NoteKey = null)
{
    /// <summary>The admin Content card that holds the group's heading, intro and note keys (forum lists: «صفحات المنتدى»).</summary>
    public string ContentGroup => Key.StartsWith("forum_", StringComparison.Ordinal) ? ContentKeys.ForumGroup : "pages";
}

/// <summary>What the rows of a <see cref="ContentItemGroup"/> hold (the admin form relabels its fields per kind).</summary>
public enum ContentItemKind
{
    /// <summary>Title (+ optional description and icon).</summary>
    List,
    /// <summary>Text = the figure as displayed in each language («+2000» / "2000+"), Description = its label.</summary>
    Figures,
    /// <summary>Text = the card title, Description = the bullets, one per line.</summary>
    Bullets,
}

/// <summary>
/// The lists an admin can edit in «القوائم» (ContentItems). Only these groups are accepted by the admin actions (no
/// over-posting); the migration AddSiteStructure inserts their rows from the company profile.
/// </summary>
public static class ContentItemGroups
{
    public const string Values = "values";
    public const string WhyPoints = "why_points";
    public const string Sectors = "sectors";
    public const string EcosystemPillars = "ecosystem_pillars";
    public const string Programs = "programs";
    public const string ForumComponents = "forum_components";
    public const string InvestorServices = "investor_services";
    public const string StartupServices = "startup_services";
    public const string GovernmentServices = "government_services";
    public const string UniversityServices = "university_services";
    public const string PartnerTypes = "partner_types";
    public const string PartnersInSuccess = "partners_in_success";
    public const string NetworkSteps = "network_steps";

    // F10 — the forum page's lists (the migration AddForumContent inserts them from the event profile).
    public const string ForumAudiences = "forum_audiences";
    public const string ForumFigures = "forum_figures";
    public const string ForumPillars = "forum_pillars";
    public const string ForumGoals = "forum_goals";
    public const string ForumSectors = "forum_sectors";

    // Choices of the «Join the Network» form (the migration AddLeadForms inserts them). Sectors reuse Sectors above.
    public const string InvestorTypes = "investor_types";
    public const string TicketSizes = "ticket_sizes";
    public const string StartupStages = "startup_stages";
    public const string FundingRanges = "funding_ranges";

    private static ContentItemGroup Options(string key, string label) => new(key, label, "", null, false, false, FormOptions: true);

    private static ContentItemGroup G(string key, string label, bool description = false, bool icon = true, string? titleKey = null,
        ContentItemKind kind = ContentItemKind.List, bool numbered = false, bool note = false) =>
        new(key, label, titleKey ?? $"section_{key}_title", $"section_{key}_intro", description, icon, false, kind, numbered,
            note ? $"section_{key}_note" : null);

    public static readonly IReadOnlyList<ContentItemGroup> All =
    [
        G(Values, "القيم الأساسية (من نحن)", titleKey: "values_title"),
        G(WhyPoints, "لماذا إنوفست (من نحن)"),
        G(Sectors, "القطاعات التي نخدمها"),
        G(EcosystemPillars, "ماذا نقدم (المنظومة)"),
        G(Programs, "برامجنا"),
        G(ForumComponents, "مكونات المنتدى", description: true, note: true),
        G(InvestorServices, "خدمات المستثمرين"),
        G(StartupServices, "خدمات الشركات الناشئة"),
        G(GovernmentServices, "خدمات الجهات الحكومية"),
        G(UniversityServices, "خدمات الجامعات"),
        G(PartnerTypes, "الجهات التي نعمل معها"),
        G(PartnersInSuccess, "شركاء النجاح"),
        G(NetworkSteps, "خطوات الانضمام إلى الشبكة", description: true),
        G(ForumFigures, "المنتدى — الأرقام", description: true, icon: false, kind: ContentItemKind.Figures),
        G(ForumAudiences, "المنتدى — التأثير على الأطراف الرئيسية", description: true, kind: ContentItemKind.Bullets),
        G(ForumPillars, "المنتدى — القطاعات الرئيسية (الركائز)", description: true, kind: ContentItemKind.Bullets),
        G(ForumGoals, "المنتدى — أهداف النسخة", icon: false, numbered: true, note: true),
        G(ForumSectors, "المنتدى — المحاور والقطاعات", icon: false, numbered: true, note: true),
        Options(InvestorTypes, "نموذج الانضمام — أنواع المستثمرين"),
        Options(TicketSizes, "نموذج الانضمام — حجم الاستثمار"),
        Options(StartupStages, "نموذج الانضمام — مراحل الشركة الناشئة"),
        Options(FundingRanges, "نموذج الانضمام — التمويل المطلوب"),
    ];

    private static readonly Dictionary<string, ContentItemGroup> ByKey = All.ToDictionary(g => g.Key, StringComparer.Ordinal);

    public static ContentItemGroup? Find(string? key) => key is null ? null : ByKey.GetValueOrDefault(key);
}
