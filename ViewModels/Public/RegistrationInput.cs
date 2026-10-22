using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Localization;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Public;

/// <summary>POST /Home/Register and /en/Register. Lengths mirror the Registrations columns; messages come from the resx.</summary>
public class RegistrationInput
{
    [Display(Name = F.FullName)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? FullName { get; set; }

    [Display(Name = F.Email)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    [EmailAddress(ErrorMessage = K.Email)]
    public string? Email { get; set; }

    [Display(Name = F.Phone)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    [RegularExpression(@"^[0-9+\-\s().]*$", ErrorMessage = K.Phone)]
    public string? Phone { get; set; }

    /// <summary>
    /// Id of an ACTIVE InterestOption — checked against the list by the controller (required while the list has
    /// active options, must be absent when it has none). The stored text is the option's Arabic name.
    /// </summary>
    [Display(Name = F.Interest)]
    public int? InterestOptionId { get; set; }

    /// <summary>Honeypot — hidden from people; must stay empty.</summary>
    public string? Website { get; set; }
}
