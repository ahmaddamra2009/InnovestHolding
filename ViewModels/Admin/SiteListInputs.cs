using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

// Inputs of the C5 list screens owned by Dev 4: extra menu links, footer columns, services, figures, partners, social
// links (PRG on error: the errors go to TempData["Error"]). Order and visibility are separate POSTs (Move / Toggle),
// never fields of these forms, so an edit form cannot flip them by omission. Icon keys are checked against
// Infrastructure/Content/Icons by the controller; no SVG is ever bound.

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
}

/// <summary>A customised footer column's title (Arabic required, English optional = Arabic).</summary>
public class FooterColumnInput
{
    public int Id { get; set; }

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }
}

/// <summary>A link added to a footer column: <c>page:&lt;key&gt;</c> or <c>nav:&lt;id&gt;</c> (one select).</summary>
public class FooterLinkInput
{
    public int ColumnId { get; set; }

    [Required(ErrorMessage = K.Required)]
    [StringLength(80, ErrorMessage = K.MaxLength)]
    public string? Target { get; set; }
}

public class PlatformInput
{
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

    [Display(Name = F.LinkUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [SafeHref]
    public string? LinkUrl { get; set; }

    /// <summary>A key of Infrastructure/Content/Icons; empty = the legacy icon of a migrated row, else the brand bullet.</summary>
    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? IconKey { get; set; }
}

public class PlatformUpdateInput : PlatformInput
{
    public int Id { get; set; }
}

public class StatInput
{
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

public class StatUpdateInput : StatInput
{
    public int Id { get; set; }
}

public class PartnerCreateInput : ImageFieldsInput
{
    [Display(Name = F.Url)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? Url { get; set; }

    public int? PartnerGroupId { get; set; }

    public bool ShowOnHome { get; set; } = true;

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

/// <summary>A social link's URL, and for a custom network its label and icon key (ignored on a known network).</summary>
public class SocialLinkUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Url)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? Url { get; set; }

    [Display(Name = F.Name)]
    [StringLength(60, ErrorMessage = K.MaxLength)]
    public string? Label { get; set; }

    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? IconKey { get; set; }
}

/// <summary>«إضافة شبكة»: a known network without a row, or <c>custom</c> (label and icon required).</summary>
public class SocialLinkCreateInput
{
    [Required(ErrorMessage = K.Required)]
    [StringLength(20, ErrorMessage = K.MaxLength)]
    public string? Network { get; set; }

    [Display(Name = F.Url)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? Url { get; set; }

    [Display(Name = F.Name)]
    [StringLength(60, ErrorMessage = K.MaxLength)]
    public string? Label { get; set; }

    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? IconKey { get; set; }
}
