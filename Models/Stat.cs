using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Stat
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }

    [MaxLength(10)]
    public string Suffix { get; set; } = "+";

    public string IconSvg { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
