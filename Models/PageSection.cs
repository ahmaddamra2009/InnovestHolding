using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// The admin's visibility and order of one section (block) of a public page. <see cref="PageKey"/> is a key of the
/// route table (Infrastructure/Site/SiteRoutes), <see cref="SectionKey"/> a key of that page in
/// Infrastructure/Site/PageSectionRegistry. The rows are owned by the migrations (AddAdminControl inserts one per
/// registry entry, in today's order); a registry section without a row renders visible, at the end.
/// </summary>
public class PageSection : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string PageKey { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string SectionKey { get; set; } = string.Empty;

    /// <summary>The admin's switch; a visible section with no content still renders nothing (today's empty rule).</summary>
    public bool IsVisible { get; set; } = true;

    public int SortOrder { get; set; }

    /// <summary>UTC.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
