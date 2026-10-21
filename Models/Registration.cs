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

    [MaxLength(100)]
    public string Interest { get; set; } = "";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
