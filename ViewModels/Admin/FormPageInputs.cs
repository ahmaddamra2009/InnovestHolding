using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.UserMessages.Fields;

namespace InnovestHolding.ViewModels.Admin;

public class ContentEditInput : IValidatableObject
{
    /// <summary>SectionKey → Arabic content. Only keys that already exist in SiteContents are updated.</summary>
    public Dictionary<string, string?> Content { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (key, value) in Content)
        {
            if ((value?.Length ?? 0) > InputLimits.LongTextMax)
                yield return new ValidationResult(
                    string.Format(UserMessages.MaxLength, F.Content, InputLimits.LongTextMax),
                    [$"Content[{key}]"]);
        }
    }
}

public class ChangePasswordInput
{
    [Display(Name = F.CurrentPassword)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(InputLimits.PasswordMax, ErrorMessage = UserMessages.MaxLength)]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Display(Name = F.NewPassword)]
    [Required(ErrorMessage = UserMessages.Required)]
    [StringLength(InputLimits.NewPasswordMax, MinimumLength = InputLimits.NewPasswordMin,
        ErrorMessage = UserMessages.LengthRange)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Display(Name = F.ConfirmPassword)]
    [Required(ErrorMessage = UserMessages.Required)]
    [Compare(nameof(NewPassword), ErrorMessage = UserMessages.PasswordsDoNotMatch)]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}
