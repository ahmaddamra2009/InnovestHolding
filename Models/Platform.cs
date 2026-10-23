using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Platform : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    public string DescriptionAr { get; set; } = string.Empty;

    public string? DescriptionEn { get; set; }

    /// <summary>Optional shorter name for the footer; falls back to the title.</summary>
    [MaxLength(100)]
    public string? ShortTitleAr { get; set; }

    [MaxLength(100)]
    public string? ShortTitleEn { get; set; }

    /// <summary>"Learn more" target; the link is not rendered while empty.</summary>
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    public bool ShowInFooter { get; set; } = true;

    /// <summary>The admin's icon: a key of Infrastructure/Content/Icons (rendered from code). NULL = the legacy
    /// <see cref="IconSvg"/>, else the brand bullet.</summary>
    [MaxLength(40)]
    public string? IconKey { get; set; }

    /// <summary>SVG markup written by the seeder only (no admin field) — rendered as trusted markup.</summary>
    public string IconSvg { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>Shown on the public site (the «Our services» cards).</summary>
    public bool IsActive { get; set; } = true;
}
