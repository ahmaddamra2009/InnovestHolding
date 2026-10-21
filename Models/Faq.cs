using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Faq
{
    public int Id { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
