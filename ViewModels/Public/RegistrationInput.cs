using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Public;

/// <summary>POST /Home/Register. Lengths mirror the Registrations columns.</summary>
public class RegistrationInput
{
    [Display(Name = F.FullName)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    public string? FullName { get; set; }

    [Display(Name = F.Email)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(200, ErrorMessage = UserMessages.MaxLength)]
    [EmailAddress(ErrorMessage = UserMessages.Email)]
    public string? Email { get; set; }

    [Display(Name = F.Phone)]
    [StringLength(50, ErrorMessage = UserMessages.MaxLength)]
    [RegularExpression(@"^[0-9+\-\s().]*$", ErrorMessage = UserMessages.Phone)]
    public string? Phone { get; set; }

    [Display(Name = F.Interest)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(100, ErrorMessage = UserMessages.MaxLength)]
    public string? Interest { get; set; }

    /// <summary>Honeypot — hidden from people; must stay empty.</summary>
    public string? Website { get; set; }
}
