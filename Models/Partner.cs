using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Partner : ISortable
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Shown on the public site.</summary>
    public bool IsActive { get; set; } = true;

    public int? LogoId { get; set; }
    public MediaFile? Logo { get; set; }

    /// <summary>The partner's website (https only); the tile links to it when set.</summary>
    [MaxLength(500)]
    public string? Url { get; set; }

    /// <summary>Heading on the Forum Partners page; NULL lists the partner after the groups.</summary>
    public int? PartnerGroupId { get; set; }
    public PartnerGroup? PartnerGroup { get; set; }

    /// <summary>Listed in the home page's partner strip.</summary>
    public bool ShowOnHome { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
