using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

public class FaqInput
{
    [Display(Name = F.Question)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(1000, ErrorMessage = K.MaxLength)]
    public string? QuestionAr { get; set; }

    [Display(Name = F.QuestionEn)]
    [StringLength(1000, ErrorMessage = K.MaxLength)]
    public string? QuestionEn { get; set; }

    [Display(Name = F.Answer)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? AnswerAr { get; set; }

    [Display(Name = F.AnswerEn)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? AnswerEn { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FaqEditInput : FaqInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }
}

public class NewsInput
{
    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Summary)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? SummaryAr { get; set; }

    [Display(Name = F.SummaryEn)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? SummaryEn { get; set; }

    [Display(Name = F.Category)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? CategoryAr { get; set; }

    [Display(Name = F.CategoryEn)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? CategoryEn { get; set; }

    [Display(Name = F.NewsDate)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? NewsDateAr { get; set; }

    [Display(Name = F.NewsDateEn)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? NewsDateEn { get; set; }

    [Display(Name = F.LinkUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [SafeHref]
    public string? LinkUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

public class NewsEditInput : NewsInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(-100_000, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }
}
