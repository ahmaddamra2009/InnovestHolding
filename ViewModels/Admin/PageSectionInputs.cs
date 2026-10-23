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
/// <param name="Embedded">Rendered inside Pages/Edit: every form posts <c>from=pages</c> so the action returns there (QA C5-4).</param>
public sealed record PageSectionsCardVm(string PageKey, string PageTitle, string PageUrl, IReadOnlyList<PageSectionRowVm> Rows,
    bool Embedded = false)
{
    public bool IsHome => PageKey == PageKeys.Home;

    /// <summary>
    /// The row offers a show/hide switch only when the switch can succeed: the section can be hidden
    /// (<see cref="SectionDef.CanHide"/>) and it is not the page's last visible section (Toggle refuses that). Same rule
    /// as PageSectionsController.Toggle, so the card never offers a button that is always refused (Hassan C5 F5).
    /// </summary>
    public bool CanToggle(PageSectionRowVm row) => row.Def.CanHide && !(row.Visible && Rows.Count(r => r.Visible) <= 1);
}
