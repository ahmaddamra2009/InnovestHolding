using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>
/// The texts of one page (Admin/Pages/Edit). The page key comes from the route, never from the form, and must be a
/// key of the route table; the lengths mirror the columns.
/// </summary>
public class PageEditInput : ImageFieldsInput
{
    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Summary)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryAr { get; set; }

    [Display(Name = F.SummaryEn)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryEn { get; set; }

    [Display(Name = F.Intro)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? IntroAr { get; set; }

    [Display(Name = F.IntroEn)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? IntroEn { get; set; }

    [Display(Name = F.Body)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyAr { get; set; }

    [Display(Name = F.BodyEn)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyEn { get; set; }

    [Display(Name = F.EmptyText)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? EmptyTextAr { get; set; }

    [Display(Name = F.EmptyTextEn)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? EmptyTextEn { get; set; }

    [Display(Name = F.MetaTitle)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? MetaTitleAr { get; set; }

    [Display(Name = F.MetaTitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? MetaTitleEn { get; set; }

    [Display(Name = F.MetaDescription)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? MetaDescriptionAr { get; set; }

    [Display(Name = F.MetaDescriptionEn)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? MetaDescriptionEn { get; set; }

    public bool ShowInMenu { get; set; }

    /// <summary>Ignored for the home page (always published).</summary>
    public bool IsPublished { get; set; }

    /// <summary>Listed in the automatic footer. Ignored for the home page.</summary>
    public bool ShowInFooter { get; set; }

    /// <summary>The empty state's button target: a key of the route table, or empty = the code default.</summary>
    [StringLength(60, ErrorMessage = K.MaxLength)]
    public string? EmptyActionPage { get; set; }
}
