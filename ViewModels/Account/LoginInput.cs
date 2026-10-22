using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Account;

/// <summary>POST /Account/Login — the only fields the login form may bind.</summary>
public class LoginInput
{
    [Display(Name = F.Username)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? Username { get; set; }

    [Display(Name = F.Password)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(InputLimits.PasswordMax, ErrorMessage = K.MaxLength)]
    public string? Password { get; set; }
}
