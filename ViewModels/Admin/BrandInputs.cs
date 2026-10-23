using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Content;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>One brand asset's form (Admin/Brand): which slot, and the image fields (file, remove, alt texts).</summary>
public sealed class BrandImageInput : ImageFieldsInput
{
    [StringLength(40, ErrorMessage = K.MaxLength)]
    public string? Key { get; set; }
}

/// <summary>The theme colour form: #RRGGBB, or empty for the default.</summary>
public sealed class BrandThemeInput
{
    [StringLength(7, ErrorMessage = K.MaxLength)]
    public string? ThemeColor { get; set; }
}

/// <summary>A slot as the Brand screen shows it (display only, never bound).</summary>
public sealed record BrandSlotVm(BrandImage Slot, BrandImageInput Input, int? Bytes, string? Advice, string? Error);

/// <summary>The Brand screen (display only, never bound: the POSTs bind <see cref="BrandImageInput"/> and
/// <see cref="BrandThemeInput"/>).</summary>
public sealed class BrandPageVm
{
    public IReadOnlyList<BrandSlotVm> Slots { get; init; } = [];

    public string ThemeColor { get; init; } = BrandImages.DefaultThemeColor;

    /// <summary>The value the admin typed when it was refused (shown again with the error).</summary>
    public string? ThemeColorTyped { get; init; }

    public string? ThemeColorError { get; init; }
}
