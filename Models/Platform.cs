using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Platform
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

    /// <summary>SVG markup written by the seeder only (no admin field) — rendered as trusted markup.</summary>
    public string IconSvg { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
