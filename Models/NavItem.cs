using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class NavItem
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Href { get; set; } = "#";

    public int SortOrder { get; set; }

    public bool HasDropdown { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<NavSubItem> SubItems { get; set; } = new List<NavSubItem>();
}
