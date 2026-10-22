using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Public;

/// <summary>POST /Home/Contact. Lengths mirror the ContactMessages columns; Message is a product limit.</summary>
public class ContactInput
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

    [Display(Name = F.Subject)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(300, ErrorMessage = UserMessages.MaxLength)]
    public string? Subject { get; set; }

    [Display(Name = F.Message)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(InputLimits.MessageMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Message { get; set; }

    /// <summary>Honeypot — hidden from people; must stay empty.</summary>
    public string? Website { get; set; }
}
