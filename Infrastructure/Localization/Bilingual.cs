namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// A text resolved for the page language. <see cref="IsFallback"/> is true on the English page when the English value
/// was empty and the Arabic one is shown instead — the view then marks the element <c>lang="ar" dir="rtl"</c>.
/// </summary>
public readonly record struct LText(string Value, bool IsFallback)
{
    public static readonly LText Empty = new("", false);

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    /// <summary>"ar" on a fallback, else null (Razor omits an attribute whose value is null).</summary>
    public string? Lang => IsFallback ? Cultures.ArabicName : null;

    public string? Dir => IsFallback ? "rtl" : null;

    public override string ToString() => Value;
}

/// <summary>THE fallback rule of the site (ANTIGRAVITY §9). Views never compare languages themselves.</summary>
public static class Bilingual
{
    /// <summary>
    /// English page: the English value when it has any non-whitespace character, else the Arabic value marked as a
    /// fallback. Arabic page: always the Arabic value (Arabic is never replaced by English).
    /// </summary>
    public static LText Pick(string? ar, string? en, bool english) =>
        english && !string.IsNullOrWhiteSpace(en)
            ? new LText(en.Trim(), false)
            : new LText(ar ?? "", english && !string.IsNullOrEmpty(ar));

    /// <summary>No fallback: each page shows its own language's value or nothing (e.g. the hero kicker).</summary>
    public static LText Optional(string? ar, string? en, bool english) =>
        new(english ? en?.Trim() ?? "" : ar ?? "", false);
}
