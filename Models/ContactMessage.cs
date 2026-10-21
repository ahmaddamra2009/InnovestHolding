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

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
