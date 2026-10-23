using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Content;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>
/// The forum colours and fonts form (Admin/ForumIdentity/Colors). Colours are posted as Colors[{key}] for the keys of
/// <see cref="ForumIdentity.Colors"/> (any other posted key is ignored); each is #RRGGBB or empty (= the default). Fonts
/// are keys of <see cref="ForumIdentity.Fonts"/> or empty.
/// </summary>
public sealed class ForumColorsInput
{
    public Dictionary<string, string?> Colors { get; set; } = new(StringComparer.Ordinal);

    [StringLength(20, ErrorMessage = K.MaxLength)]
    public string? FontAr { get; set; }

    [StringLength(20, ErrorMessage = K.MaxLength)]
    public string? FontEn { get; set; }
}

/// <summary>A colour row of the screen (display only).</summary>
/// <param name="Value">What the field shows: the typed value when the form came back with an error, else the stored one.</param>
/// <param name="Effective">The colour the pages use (stored, else the default).</param>
public sealed record ForumColorRowVm(ForumColor Color, string? Value, string Effective, string? Error);

/// <summary>A contrast pair as the screen lists it.</summary>
public sealed record ForumContrastRowVm(ContrastPair Pair, string Foreground, string Background, double Ratio)
{
    public bool Passes => Ratio >= Pair.Min;
}

/// <summary>The «هوية المنتدى» screen (display only, never bound).</summary>
public sealed class ForumIdentityPageVm
{
    public IReadOnlyList<BrandSlotVm> Slots { get; init; } = [];
    public IReadOnlyList<ForumColorRowVm> Colors { get; init; } = [];
    public IReadOnlyList<ForumContrastRowVm> Contrast { get; init; } = [];
    public string? FontAr { get; init; }
    public string? FontEn { get; init; }
    public string? FontError { get; init; }

    /// <summary>Why the colours were refused (invalid values or contrast): shown above the form.</summary>
    public string? ColorsError { get; init; }

    /// <summary>The pairs that failed on the refused save (empty otherwise).</summary>
    public IReadOnlyList<ContrastFailure> Failures { get; init; } = [];
}
