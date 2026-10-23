namespace InnovestHolding.Infrastructure.Content;

/// <param name="Key">ContentItems.Group.</param>
/// <param name="Label">Arabic admin label.</param>
/// <param name="TitleKey">SiteContents key of the section heading on the public page.</param>
/// <param name="IntroKey">SiteContents key of the optional lead sentence (null: none).</param>
/// <param name="UsesDescription">The admin form shows the description fields.</param>
/// <param name="UsesIcon">The admin form shows the icon picker.</param>
public sealed record ContentItemGroup(string Key, string Label, string TitleKey, string? IntroKey, bool UsesDescription, bool UsesIcon);

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

    private static ContentItemGroup G(string key, string label, bool description = false, bool icon = true, string? titleKey = null) =>
        new(key, label, titleKey ?? $"section_{key}_title", $"section_{key}_intro", description, icon);

    public static readonly IReadOnlyList<ContentItemGroup> All =
    [
        G(Values, "القيم الأساسية (من نحن)", titleKey: "values_title"),
        G(WhyPoints, "لماذا إنوفست (من نحن)"),
        G(Sectors, "القطاعات التي نخدمها"),
        G(EcosystemPillars, "ماذا نقدم (المنظومة)"),
        G(Programs, "برامجنا"),
        G(ForumComponents, "مكونات المنتدى"),
        G(InvestorServices, "خدمات المستثمرين"),
        G(StartupServices, "خدمات الشركات الناشئة"),
        G(GovernmentServices, "خدمات الجهات الحكومية"),
        G(UniversityServices, "خدمات الجامعات"),
        G(PartnerTypes, "الجهات التي نعمل معها"),
        G(PartnersInSuccess, "شركاء النجاح"),
        G(NetworkSteps, "خطوات الانضمام إلى الشبكة", description: true),
    ];

    private static readonly Dictionary<string, ContentItemGroup> ByKey = All.ToDictionary(g => g.Key, StringComparer.Ordinal);

    public static ContentItemGroup? Find(string? key) => key is null ? null : ByKey.GetValueOrDefault(key);
}
