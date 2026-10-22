namespace InnovestHolding.Infrastructure.Localization;

/// <summary>The only place that builds culture URLs: the two home pages, the form endpoints, the indexable list.</summary>
public static class CultureUrls
{
    public const string ArabicHome = "/";
    public const string EnglishHome = "/en";

    /// <summary>Paths that may be indexed (head tags, sitemap, and the X-Robots-Tag rule all read this list).</summary>
    public static readonly IReadOnlyList<string> Indexable = [ArabicHome, EnglishHome];

    public static string Home(bool english) => english ? EnglishHome : ArabicHome;

    /// <summary>Register / Contact endpoint for the page language: /Home/{action} or /en/{action}.</summary>
    public static string FormAction(string action, bool english) => english ? $"/en/{action}" : $"/Home/{action}";

    public static bool IsEnglishPath(PathString path)
    {
        var value = path.Value ?? "";
        return value.Equals(EnglishHome, StringComparison.OrdinalIgnoreCase)
               || value.StartsWith(EnglishHome + "/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIndexable(PathString path) =>
        Indexable.Any(p => string.Equals(path.Value ?? "", p, StringComparison.Ordinal));
}
