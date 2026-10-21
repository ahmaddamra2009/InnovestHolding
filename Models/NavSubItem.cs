using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovestHolding.Models;

public class NavSubItem
{
    public int Id { get; set; }

    [ForeignKey(nameof(NavItem))]
    public int NavItemId { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Href { get; set; } = "#";

    public string? IconSvg { get; set; }

    public int SortOrder { get; set; }

    public NavItem? NavItem { get; set; }
}
