using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Platform
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IconSvg { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
