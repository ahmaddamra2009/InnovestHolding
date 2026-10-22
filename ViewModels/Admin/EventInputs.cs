using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Admin;

public class EventInput
{
    [Display(Name = F.Title)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? Title { get; set; }

    [Display(Name = F.Description)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Description { get; set; }

    [Display(Name = F.EventDay)]
    [Required(ErrorMessage = UserMessages.Required)]
    [Range(1, 31, ErrorMessage = UserMessages.Range)]
    public int? EventDay { get; set; }

    [Display(Name = F.EventMonth)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(50, ErrorMessage = UserMessages.MaxLength)]
    public string? EventMonth { get; set; }

    [Display(Name = F.Location)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? Location { get; set; }

    [Display(Name = F.Time)]
    [StringLength(100, ErrorMessage = UserMessages.MaxLength)]
    public string? Time { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EventEditInput : EventInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = UserMessages.Range)]
    public int SortOrder { get; set; }
}
