using InnovestHolding.Infrastructure.Content;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>The posted overrides, keyed by resx key (only keys of <see cref="PublicTexts"/> are accepted).</summary>
public class SiteTextsInput
{
    public Dictionary<string, BilingualValueInput> Texts { get; set; } = new();
}

/// <summary>One field of the screen: the resx defaults (placeholders) and the stored or posted override.</summary>
public sealed record SiteTextField(PublicText Text, string DefaultAr, string DefaultEn, string? ValueAr, string? ValueEn, bool IsCustom)
{
    public IReadOnlyList<string> Placeholders => PublicTexts.Placeholders(DefaultAr);
}

public sealed record SiteTextsViewModel(string Tab, string? Query, IReadOnlyList<SiteTextField> Fields, IReadOnlyDictionary<string, int> CustomCounts);
