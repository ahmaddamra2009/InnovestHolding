using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(300)]
    public string Subject { get; set; } = "";

    [Required]
    public string Message { get; set; } = "";

    /// <summary>Page language the visitor submitted from ("ar" / "en"); written by the server, never bound.</summary>
    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; } = false;

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
