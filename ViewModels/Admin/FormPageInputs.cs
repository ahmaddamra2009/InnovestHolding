using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>One SiteContents key as posted by the content editor: content[key].Ar / content[key].En.</summary>
public class BilingualValueInput
{
    public string? Ar { get; set; }
    public string? En { get; set; }
}

/// <summary>
/// The content editor. Only keys of the registry (Infrastructure/Content/ContentKeys) are accepted; the controller
/// validates each posted key against its registry entry (length, required Arabic, highlight syntax, URL rules).
/// Keys that are not posted are left unchanged.
/// </summary>
public class ContentEditInput
{
    public Dictionary<string, BilingualValueInput> Content { get; set; } = new();
}

public class ChangePasswordInput
{
    [Display(Name = F.CurrentPassword)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(InputLimits.PasswordMax, ErrorMessage = K.MaxLength)]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Display(Name = F.NewPassword)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(InputLimits.NewPasswordMax, MinimumLength = InputLimits.NewPasswordMin,
        ErrorMessage = K.LengthRange)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Display(Name = F.ConfirmPassword)]
    [Required(ErrorMessage = K.Required)]
    [Compare(nameof(NewPassword), ErrorMessage = K.PasswordsDoNotMatch)]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}
