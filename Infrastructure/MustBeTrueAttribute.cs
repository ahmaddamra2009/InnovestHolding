using InnovestHolding.Infrastructure.Localization;

namespace InnovestHolding.Infrastructure;

/// <summary>A checkbox that must be ticked (the consent box of the lead forms): an unticked box posts nothing → false.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MustBeTrueAttribute() : LocalizedValidationAttribute(MessageKeys.ConsentRequired)
{
    protected override bool IsValueValid(object? value) => value is true;
}
