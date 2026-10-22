using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InnovestHolding.ViewModels;

/// <summary>Display model of the login page. The POST binds <see cref="Account.LoginInput"/>.</summary>
public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    [BindNever]
    public string? ErrorMessage { get; set; }
}
