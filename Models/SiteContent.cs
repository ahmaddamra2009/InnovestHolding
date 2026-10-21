using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class SiteContent
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string SectionKey { get; set; } = string.Empty;

    public string ContentAr { get; set; } = string.Empty;

    public string? ContentEn { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
