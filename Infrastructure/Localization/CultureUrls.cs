using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// The only place that builds public URLs: pages (from the route table <see cref="SiteRoutes"/>) and form endpoints.
/// Views, menus, breadcrumbs, the sitemap, JSON-LD and the language switch all call it.
/// </summary>
public static class CultureUrls
{
    public const string ArabicHome = "/";
    public const string EnglishHome = SiteRoutes.EnglishPrefix;

    /// <summary>The form endpoints (POST only): /forms/{name} and /en/forms/{name}.</summary>
    public static readonly IReadOnlyList<string> FormNames = ["register", "contact"];

    public static string Home(bool english) => english ? EnglishHome : ArabicHome;

    /// <summary>The URL of a page of the route table in the requested language.</summary>
    public static string Page(string key, bool english) => SiteRoutes.PathFor(SiteRoutes.Get(key), english);

    /// <summary>The same page in the other language.</summary>
    public static string Alternate(string key, bool english) => Page(key, !english);

    /// <summary>A form endpoint for the page language: /forms/{name} or /en/forms/{name}.</summary>
    public static string Form(string name, bool english) => (english ? EnglishHome : "") + "/forms/" + name;

    public static bool IsEnglishPath(PathString path)
    {
        var value = path.Value ?? "";
        return value.Equals(EnglishHome, StringComparison.OrdinalIgnoreCase)
               || value.StartsWith(EnglishHome + "/", StringComparison.OrdinalIgnoreCase);
    }
}
