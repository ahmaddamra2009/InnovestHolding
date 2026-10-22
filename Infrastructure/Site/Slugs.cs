using System.Text;
using System.Text.RegularExpressions;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// The address part of a detail page: ASCII, lower-case words joined by single hyphens, shared by both languages
/// (SEO decision: English slugs for both). Route constraints, the admin validation and the URL normaliser use the
/// same pattern.
/// </summary>
public static partial class Slugs
{
    public const int MaxLength = 100;

    /// <summary>The route-constraint form of <see cref="Pattern"/>.</summary>
    public const string RouteRegex = @"^[a-z0-9]+(-[a-z0-9]+)*\z";

    [GeneratedRegex(RouteRegex)]
    public static partial Regex Pattern();

    /// <summary>First path segments a slug must never equal (they are site areas).</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "en", "ar", "admin", "account", "home", "forms", "media", "assets", "uploads", "page", "index",
    };

    /// <summary>
    /// True for "{entityType}-{digits}" (e.g. "speaker-12"): the address the admin gives an item that has no English
    /// title, after its insert. Reserved for that use.
    /// </summary>
    public static bool IsAutomatic(string entityType, string slug) =>
        slug.Length > entityType.Length + 1
        && slug.StartsWith(entityType + "-", StringComparison.Ordinal)
        && slug.AsSpan(entityType.Length + 1).IndexOfAnyExceptInRange('0', '9') < 0;

    public static bool IsValid(string? slug) =>
        slug is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(slug) && !Reserved.Contains(slug);

    /// <summary>
    /// A slug made from a (normally English) title: letters and digits kept (accents removed), everything else becomes
    /// a hyphen; empty when nothing ASCII is left (an Arabic-only title).
    /// </summary>
    public static string FromTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        var normalized = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var hyphen = false;
        foreach (var ch in normalized)
        {
            var c = char.ToLowerInvariant(ch);
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
                hyphen = false;
            }
            else if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // accent of a decomposed letter: dropped
            }
            else if (!hyphen && sb.Length > 0)
            {
                sb.Append('-');
                hyphen = true;
            }
        }
        var slug = sb.ToString().Trim('-');
        if (slug.Length > MaxLength) slug = slug[..MaxLength].TrimEnd('-');
        return Reserved.Contains(slug) ? "" : slug;
    }
}
