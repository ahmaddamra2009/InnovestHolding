using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

public class EventInput
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

    [Display(Name = F.EventDay)]
    [Required(ErrorMessage = K.Required)]
    [Range(1, 31, ErrorMessage = K.Range)]
    public int? EventDay { get; set; }

    [Display(Name = F.EventMonth)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? EventMonthAr { get; set; }

    [Display(Name = F.EventMonthEn)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? EventMonthEn { get; set; }

    [Display(Name = F.Location)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? LocationAr { get; set; }

    [Display(Name = F.LocationEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? LocationEn { get; set; }

    [Display(Name = F.Time)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TimeAr { get; set; }

    [Display(Name = F.TimeEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TimeEn { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EventEditInput : EventInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }
}
