using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Partner
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
