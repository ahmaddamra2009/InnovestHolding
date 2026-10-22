using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Account;

/// <summary>POST /Account/Login — the only fields the login form may bind.</summary>
public class LoginInput
{
    [Display(Name = F.Username)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(100, ErrorMessage = UserMessages.MaxLength)]
    public string? Username { get; set; }

    [Display(Name = F.Password)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(InputLimits.PasswordMax, ErrorMessage = UserMessages.MaxLength)]
    public string? Password { get; set; }
}
