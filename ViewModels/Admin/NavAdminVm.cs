using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>«القائمة والفوتر» (Views/Admin/Nav/Index): the extra links tab and the footer columns tab.</summary>
public sealed class NavAdminVm
{
    public const string MenuTab = "menu";
    public const string FooterTab = "footer";

    public string Tab { get; init; } = MenuTab;

    public required List<NavItem> Items { get; init; }

    /// <summary>The customised columns with their links in order; empty = the footer is automatic.</summary>
    public required List<FooterColumn> Columns { get; init; }

    /// <summary>Page key → (Arabic title, published) of the stored pages (labels of footer links and of the picker).</summary>
    public required Dictionary<string, (string TitleAr, bool IsPublished)> Pages { get; init; }

    /// <summary>While automatic: what the footer shows now (what «تخصيص الأعمدة» starts from).</summary>
    public IReadOnlyList<FooterPlanColumn>? AutomaticPlan { get; set; }

    public string QuickLinksTitle { get; set; } = "";

    public bool IsCustomised => Columns.Count > 0;

    public string PageTitle(string key) => Pages.TryGetValue(key, out var p) ? p.TitleAr : key;

    public string NavTitle(int id) => Items.FirstOrDefault(n => n.Id == id)?.TitleAr ?? "#" + id;

    /// <summary>The label of one footer link in the admin (page title or extra link name, with its state).</summary>
    public (string Label, string Kind, bool Shown) Describe(string? pageKey, int? navId)
    {
        if (pageKey is not null)
            return (PageTitle(pageKey), "صفحة", SiteRoutes.IsKey(pageKey) && (!Pages.TryGetValue(pageKey, out var p) || p.IsPublished));
        var nav = Items.FirstOrDefault(n => n.Id == navId);
        return (nav?.TitleAr ?? "—", "رابط إضافي", nav?.IsActive == true);
    }
}
