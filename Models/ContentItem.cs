using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// One entry of a bilingual list shown on a page (core values, sectors, services of a unit…). <see cref="Group"/> is a
/// key of Infrastructure/Content/ContentItemGroups; <see cref="IconKey"/> is a key of Infrastructure/Content/Icons (the
/// SVG comes from code, never from the database).
/// </summary>
public class ContentItem
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Group { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string TextAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? TextEn { get; set; }

    [MaxLength(3370)]
    public string? DescriptionAr { get; set; }

    [MaxLength(3370)]
    public string? DescriptionEn { get; set; }

    [MaxLength(40)]
    public string? IconKey { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
