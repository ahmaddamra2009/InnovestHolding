using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// Framework model-binding messages (a bad int, a missing value) come from the resx in the request's language —
/// otherwise "EventDay=abc" would answer with an English framework sentence on an Arabic page. The localizer is
/// resolved per call, so the CURRENT request culture decides.
/// </summary>
public sealed class ModelBindingMessagesSetup(IStringLocalizerFactory factory) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options)
    {
        var l = factory.Create(typeof(SharedResource));
        string F(string key, string field) => l[key, field];
        string N(string key) => l[key];

        var m = options.ModelBindingMessageProvider;
        m.SetAttemptedValueIsInvalidAccessor((_, field) => F(MessageKeys.InvalidValue, field));
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => N(MessageKeys.InvalidValueNoField));
        m.SetUnknownValueIsInvalidAccessor(field => F(MessageKeys.InvalidValue, field));
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => N(MessageKeys.InvalidValueNoField));
        m.SetValueIsInvalidAccessor(_ => N(MessageKeys.InvalidValueNoField));
        m.SetValueMustBeANumberAccessor(field => F(MessageKeys.InvalidValue, field));
        m.SetNonPropertyValueMustBeANumberAccessor(() => N(MessageKeys.InvalidValueNoField));
        m.SetValueMustNotBeNullAccessor(_ => N(MessageKeys.ValueRequiredNoField));
        m.SetMissingBindRequiredValueAccessor(field => F(MessageKeys.ValueRequired, field));
        m.SetMissingKeyOrValueAccessor(() => N(MessageKeys.ValueRequiredNoField));
        m.SetMissingRequestBodyRequiredValueAccessor(() => N(MessageKeys.ValueRequiredNoField));
    }
}
