using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Admin;

public class FaqInput
{
    [Display(Name = F.Question)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(1000, ErrorMessage = UserMessages.MaxLength)]
    public string? Question { get; set; }

    [Display(Name = F.Answer)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Answer { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FaqEditInput : FaqInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = UserMessages.Range)]
    public int SortOrder { get; set; }
}

public class NewsInput
{
    [Display(Name = F.Title)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? Title { get; set; }

    [Display(Name = F.Summary)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Summary { get; set; }

    [Display(Name = F.Category)]
    [StringLength(50, ErrorMessage = UserMessages.MaxLength)]
    public string? Category { get; set; }

    [Display(Name = F.NewsDate)]
    [StringLength(50, ErrorMessage = UserMessages.MaxLength)]
    public string? NewsDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class NewsEditInput : NewsInput
{
    public int Id { get; set; }
}
