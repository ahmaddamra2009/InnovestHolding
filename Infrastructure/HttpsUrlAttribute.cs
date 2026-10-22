using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Base for the app's custom validation attributes. MVC localizes only the built-in attributes (through their
/// adapters); a custom one would show its raw resource key, so it localizes its own message here ({0} = field name).
/// </summary>
public abstract class LocalizedValidationAttribute : ValidationAttribute
{
    private readonly string _messageKey;

    protected LocalizedValidationAttribute(string messageKey) : base(messageKey) => _messageKey = messageKey;

    protected abstract bool IsValueValid(object? value);

    public override bool IsValid(object? value) => IsValueValid(value);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (IsValueValid(value)) return ValidationResult.Success;
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var message = factory is null
            ? FormatErrorMessage(validationContext.DisplayName)
            : factory.Create(typeof(SharedResource))[_messageKey, validationContext.DisplayName].Value;
        return new ValidationResult(message, validationContext.MemberName is { } m ? [m] : null);
    }
}

/// <summary>An absolute https:// URL, or empty (empty is left to [Required]).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HttpsUrlAttribute() : LocalizedValidationAttribute(MessageKeys.HttpsUrl)
{
    protected override bool IsValueValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || SiteUrl.IsHttpsUrl(s);
}
