using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class NavItem
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TitleEn { get; set; }

    [Required, MaxLength(255)]
    public string Href { get; set; } = "#";

    public int SortOrder { get; set; }

    public bool HasDropdown { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Listed in the footer quick-links column.</summary>
    public bool ShowInFooter { get; set; }

    public ICollection<NavSubItem> SubItems { get; set; } = new List<NavSubItem>();
}
