using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

// Inputs of the inline forms on the list pages (PRG on error: the errors go to TempData["Error"]).

public class NavItemCreateInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.NameEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Href)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(255, ErrorMessage = K.MaxLength)]
    [SafeHref]
    public string? Href { get; set; }
}

public class NavItemUpdateInput : NavItemCreateInput
{
    public int Id { get; set; }
    public bool HasDropdown { get; set; }
    public bool IsActive { get; set; }
    public bool ShowInFooter { get; set; }
}

public class NavSubItemCreateInput : NavItemCreateInput
{
    public int NavItemId { get; set; }
}

public class NavSubItemUpdateInput : NavItemCreateInput
{
    public int Id { get; set; }
}

public class PartnerCreateInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? NameAr { get; set; }

    [Display(Name = F.NameEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? NameEn { get; set; }
}

public class PartnerUpdateInput : PartnerCreateInput
{
    public int Id { get; set; }
}

public class PlatformUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Description)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? DescriptionAr { get; set; }

    [Display(Name = F.DescriptionEn)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? DescriptionEn { get; set; }

    [Display(Name = F.ShortTitle)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? ShortTitleAr { get; set; }

    [Display(Name = F.ShortTitleEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? ShortTitleEn { get; set; }

    [Display(Name = F.LinkUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [SafeHref]
    public string? LinkUrl { get; set; }

    public bool ShowInFooter { get; set; }
}

public class StatUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Label)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? LabelAr { get; set; }

    [Display(Name = F.LabelEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? LabelEn { get; set; }

    [Display(Name = F.ShortLabel)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? ShortLabelAr { get; set; }

    [Display(Name = F.ShortLabelEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? ShortLabelEn { get; set; }

    [Display(Name = F.Value)]
    [Required(ErrorMessage = K.Required)]
    [Range(0, 1_000_000_000, ErrorMessage = K.Range)]
    public int? Value { get; set; }

    [Display(Name = F.Suffix)]
    [StringLength(10, ErrorMessage = K.MaxLength)]
    public string? Suffix { get; set; }
}

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

public class SocialLinkUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Url)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? Url { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}
