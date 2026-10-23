using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>A list entry (Admin/ContentItems). The group is checked against the registry; the icon against the whitelist.</summary>
public class ContentItemInput
{
    [Required(ErrorMessage = K.Required)]
    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? Group { get; set; }

    [Display(Name = F.Text)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? TextAr { get; set; }

    [Display(Name = F.TextEn)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? TextEn { get; set; }

    [Display(Name = F.Description)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? DescriptionAr { get; set; }

    [Display(Name = F.DescriptionEn)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? DescriptionEn { get; set; }

    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? IconKey { get; set; }
}

public class ContentItemUpdateInput : ContentItemInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}
