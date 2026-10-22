using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Partner
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }
}
