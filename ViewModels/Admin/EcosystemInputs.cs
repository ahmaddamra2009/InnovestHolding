using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

// Admin input models of the ecosystem entities: lengths = the columns, URLs https only, ids of linked rows checked
// by the controller. Display-only data is [BindNever].

/// <summary>A checkbox list of linked rows (an event's speakers, an episode's guests): display only.</summary>
public sealed record PickVm(int Id, string Label, bool Active);

/// <summary>Meta title/description shared by the detail-page entities.</summary>
public abstract class DetailMetaInput : ImageFieldsInput
{
    [Display(Name = F.Slug)]
    [StringLength(Infrastructure.Site.Slugs.MaxLength, ErrorMessage = K.MaxLength)]
    public string? Slug { get; set; }

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

    public bool IsActive { get; set; } = true;

    /// <summary>Set on edit (0 on create).</summary>
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }
}

/// <summary>A speaker or a TV guest.</summary>
public sealed class PersonInput : DetailMetaInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? NameAr { get; set; }

    [Display(Name = F.NameEn)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? NameEn { get; set; }

    [Display(Name = F.JobTitle)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? JobTitleAr { get; set; }

    [Display(Name = F.JobTitleEn)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? JobTitleEn { get; set; }

    [Display(Name = F.Organization)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? OrganizationAr { get; set; }

    [Display(Name = F.OrganizationEn)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? OrganizationEn { get; set; }

    [Display(Name = F.Body)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BioAr { get; set; }

    [Display(Name = F.BodyEn)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BioEn { get; set; }

    [Display(Name = F.LinkedInUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? LinkedInUrl { get; set; }

    [Display(Name = F.XUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? XUrl { get; set; }
}

public sealed class EpisodeInput : DetailMetaInput
{
    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Summary)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryAr { get; set; }

    [Display(Name = F.SummaryEn)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryEn { get; set; }

    [Display(Name = F.Body)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyAr { get; set; }

    [Display(Name = F.BodyEn)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyEn { get; set; }

    [Display(Name = F.Season)]
    [Range(1, 999, ErrorMessage = K.Range)]
    public int? Season { get; set; }

    [Display(Name = F.EpisodeNumber)]
    [Range(1, 9999, ErrorMessage = K.Range)]
    public int? Number { get; set; }

    [Display(Name = F.AiredOn)]
    public DateOnly? AiredOn { get; set; }

    [Display(Name = F.VideoUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? VideoUrl { get; set; }

    /// <summary>Linked guests, in the order of the list.</summary>
    public List<int> GuestIds { get; set; } = [];

    [BindNever]
    public IReadOnlyList<PickVm> GuestChoices { get; set; } = [];
}

public sealed class OpportunityInput : DetailMetaInput
{
    public OpportunityInput() => IsActive = false; // a new opportunity is a draft

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Summary)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryAr { get; set; }

    [Display(Name = F.SummaryEn)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryEn { get; set; }

    [Display(Name = F.Body)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyAr { get; set; }

    [Display(Name = F.BodyEn)]
    [StringLength(InputLimits.BodyTextMax, ErrorMessage = K.MaxLength)]
    public string? BodyEn { get; set; }

    /// <summary>A row of the "sectors" list; checked by the controller.</summary>
    public int? SectorItemId { get; set; }

    [Display(Name = F.Stage)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? StageTextAr { get; set; }

    [Display(Name = F.StageEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? StageTextEn { get; set; }

    [Display(Name = F.Ask)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? AskTextAr { get; set; }

    [Display(Name = F.AskEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? AskTextEn { get; set; }

    public bool IsOpen { get; set; } = true;

    [BindNever]
    public IReadOnlyList<PickVm> SectorChoices { get; set; } = [];
}

public sealed class PackageInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = K.Required)]
    [StringLength(10, ErrorMessage = K.MaxLength)]
    public string? Unit { get; set; } = Models.SponsorshipUnits.Tv;

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Price)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? PriceTextAr { get; set; }

    [Display(Name = F.PriceEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? PriceTextEn { get; set; }

    [Display(Name = F.Benefits)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? BenefitsAr { get; set; }

    [Display(Name = F.BenefitsEn)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? BenefitsEn { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
