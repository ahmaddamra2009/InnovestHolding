using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    public string DescriptionAr { get; set; } = string.Empty;

    public string? DescriptionEn { get; set; }

    public int EventDay { get; set; }

    [MaxLength(50)]
    public string EventMonthAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EventMonthEn { get; set; }

    [MaxLength(200)]
    public string LocationAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LocationEn { get; set; }

    [MaxLength(100)]
    public string TimeAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TimeEn { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
