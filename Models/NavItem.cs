using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class NavItem : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(170)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(170)]
    public string? TitleEn { get; set; }

    [Required, MaxLength(255)]
    public string Href { get; set; } = "#";

    public int SortOrder { get; set; }

    public bool HasDropdown { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Kept, unused since C5: the automatic footer lists every active extra link; placement in the footer is
    /// done in the customised footer columns (FooterLinks).</summary>
    public bool ShowInFooter { get; set; }

    /// <summary>Listed in the header and the drawer (with <see cref="IsActive"/>).</summary>
    public bool ShowInHeader { get; set; } = true;

    public ICollection<NavSubItem> SubItems { get; set; } = new List<NavSubItem>();
}
