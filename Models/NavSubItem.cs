using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovestHolding.Models;

public class NavSubItem
{
    public int Id { get; set; }

    [ForeignKey(nameof(NavItem))]
    public int NavItemId { get; set; }

    [Required, MaxLength(100)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TitleEn { get; set; }

    [Required, MaxLength(255)]
    public string Href { get; set; } = "#";

    /// <summary>SVG markup written by the seeder only (no admin field) — rendered as trusted markup.</summary>
    public string? IconSvg { get; set; }

    public int SortOrder { get; set; }

    public NavItem? NavItem { get; set; }
}
