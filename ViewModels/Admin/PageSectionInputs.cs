using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>
/// One section on the sections screen. <see cref="Count"/>: items on the live site (null = not a list);
/// <see cref="Position"/>: 1-based among the movable sections (0 = fixed); <see cref="Limit"/>/<see cref="LimitValue"/>:
/// the count edited inline on this row, if any.
/// </summary>
public sealed record PageSectionRowVm(SectionDef Def, bool Visible, int? Count, int Position, int MovableCount,
    PageSectionRegistry.SectionLimit? Limit, int? LimitValue);

/// <summary>Views/Admin/PageSections/_Card: the sections of one page (the screen, and «كتل الصفحة» in Pages/Edit).</summary>
public sealed record PageSectionsCardVm(string PageKey, string PageTitle, string PageUrl, IReadOnlyList<PageSectionRowVm> Rows)
{
    public bool IsHome => PageKey == PageKeys.Home;
}
