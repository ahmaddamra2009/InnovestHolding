using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>A bullet of the registration section.</summary>
public class RegisterBenefit
{
    public int Id { get; set; }

    [Required, MaxLength(330)]
    public string TextAr { get; set; } = string.Empty;

    [MaxLength(330)]
    public string? TextEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
