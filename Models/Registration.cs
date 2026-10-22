using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Registration
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string Phone { get; set; } = "";

    /// <summary>Arabic name of the chosen option when the visitor registered (snapshot: survives option edits/deletes).</summary>
    [MaxLength(100)]
    public string Interest { get; set; } = "";

    public int? InterestOptionId { get; set; }

    public InterestOption? InterestOption { get; set; }

    /// <summary>Page language the visitor submitted from ("ar" / "en"); written by the server, never bound.</summary>
    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; } = false;

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
