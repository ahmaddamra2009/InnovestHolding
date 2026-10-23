using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// A customised footer column. While the table is empty the footer is built automatically (Infrastructure/Site/
/// FooterBuilder); the admin's «تخصيص الأعمدة» copies the automatic footer into rows, «إعادة إلى التلقائي» deletes them.
/// </summary>
public class FooterColumn : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(170)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(170)]
    public string? TitleEn { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FooterLink> Links { get; set; } = new List<FooterLink>();
}

/// <summary>
/// A link of a customised footer column: EITHER a public page (<see cref="PageKey"/>, a key of SiteRoutes) OR an extra
/// menu link (<see cref="NavItemId"/>) — a CHECK constraint enforces exactly one. Deleting the column or the extra link
/// removes the row (cascade).
/// </summary>
public class FooterLink : ISortable
{
    public int Id { get; set; }

    public int FooterColumnId { get; set; }
    public FooterColumn? FooterColumn { get; set; }

    [MaxLength(60)]
    public string? PageKey { get; set; }

    public int? NavItemId { get; set; }
    public NavItem? NavItem { get; set; }

    public int SortOrder { get; set; }
}
