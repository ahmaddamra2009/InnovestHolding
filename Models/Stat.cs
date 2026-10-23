using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Stat : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string LabelAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LabelEn { get; set; }

    /// <summary>Optional shorter label for the hero counters; falls back to the label.</summary>
    [MaxLength(100)]
    public string? ShortLabelAr { get; set; }

    [MaxLength(100)]
    public string? ShortLabelEn { get; set; }

    public int Value { get; set; }

    [MaxLength(10)]
    public string Suffix { get; set; } = "+";

    /// <summary>SVG markup written by the seeder only (no admin field) — rendered as trusted markup.</summary>
    public string IconSvg { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>Shown on the public site (hero figures and the impact section).</summary>
    public bool IsActive { get; set; } = true;
}
