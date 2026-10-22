using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Admin;

// Inputs of the inline forms on the Nav / Partners / Platforms / Stats list pages (PRG on error).

public class NavItemCreateInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(100, ErrorMessage = UserMessages.MaxLength)]
    public string? Title { get; set; }

    [Display(Name = F.Href)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(255, ErrorMessage = UserMessages.MaxLength)]
    [SafeHref]
    public string? Href { get; set; }
}

public class NavItemUpdateInput : NavItemCreateInput
{
    public int Id { get; set; }
    public bool HasDropdown { get; set; }
    public bool IsActive { get; set; }
}

public class NavSubItemCreateInput : NavItemCreateInput
{
    public int NavItemId { get; set; }
}

public class PartnerCreateInput
{
    [Display(Name = F.Name)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? Name { get; set; }
}

public class PlatformUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Title)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? Title { get; set; }

    [Display(Name = F.Description)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Description { get; set; }
}

public class StatUpdateInput
{
    public int Id { get; set; }

    [Display(Name = F.Label)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(100, ErrorMessage = UserMessages.MaxLength)]
    public string? Label { get; set; }

    [Display(Name = F.Value)]
    [Required(ErrorMessage = UserMessages.Required)]
    [Range(0, 1_000_000_000, ErrorMessage = UserMessages.Range)]
    public int? Value { get; set; }

    [Display(Name = F.Suffix)]
    [StringLength(10, ErrorMessage = UserMessages.MaxLength)]
    public string? Suffix { get; set; }
}
