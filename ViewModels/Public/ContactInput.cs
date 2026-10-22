using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Public;

/// <summary>POST /Home/Contact and /en/Contact. Lengths mirror the ContactMessages columns; Message is a product limit.</summary>
public class ContactInput
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

    [Display(Name = F.Subject)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? Subject { get; set; }

    [Display(Name = F.Message)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(InputLimits.MessageMax, ErrorMessage = K.MaxLength)]
    public string? Message { get; set; }

    /// <summary>Honeypot — hidden from people; must stay empty.</summary>
    public string? Website { get; set; }
}
