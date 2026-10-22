using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>A choice of the registration form's interest list.</summary>
public class InterestOption
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
