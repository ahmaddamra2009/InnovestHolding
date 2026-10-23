using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

// Inputs of the inline forms on the list pages (PRG on error: the errors go to TempData["Error"]).

/// <summary>InterestOptions and RegisterBenefits share this shape (Ar required, En optional, order, active).</summary>
public class SimpleListItemInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? NameAr { get; set; }

    [Display(Name = F.NameEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? NameEn { get; set; }
}

public class InterestOptionUpdateInput : SimpleListItemInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public class RegisterBenefitInput
{
    [Display(Name = F.Text)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TextAr { get; set; }

    [Display(Name = F.TextEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TextEn { get; set; }
}

public class RegisterBenefitUpdateInput : RegisterBenefitInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}
