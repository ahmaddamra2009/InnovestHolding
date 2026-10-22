using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace InnovestHolding.Infrastructure.Localization;

/// <summary>The two cultures of the site. Arabic is the default; the URL is the only source of the language.</summary>
public static class Cultures
{
    public const string ArabicName = "ar";
    public const string EnglishName = "en";

    /// <summary>
    /// Neutral "ar" with the Gregorian calendar set explicitly, so dates formatted under the Arabic culture never
    /// depend on the platform's default calendar for "ar" (ICU vs NLS).
    /// </summary>
    public static readonly CultureInfo Arabic = CreateArabic();

    public static readonly CultureInfo English = CultureInfo.ReadOnly(new CultureInfo(EnglishName));

    public static IList<CultureInfo> All => [Arabic, English];

    public static bool IsEnglish(CultureInfo culture) => culture.TwoLetterISOLanguageName == EnglishName;

    public static bool IsEnglishRequest => IsEnglish(CultureInfo.CurrentUICulture);

    /// <summary>"ar" or "en" for the current request (what Registration/ContactMessage.Culture stores).</summary>
    public static string CurrentName => IsEnglishRequest ? EnglishName : ArabicName;

    private static CultureInfo CreateArabic()
    {
        var culture = new CultureInfo(ArabicName);
        var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault() ?? new GregorianCalendar();
        culture.DateTimeFormat.Calendar = gregorian;
        // Numbers are read and written with ASCII signs and digits: the neutral "ar" NumberFormat uses a negative sign
        // with an Arabic letter mark, so a typed "-5" would fail model binding on every Arabic (admin) form.
        culture.NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        return CultureInfo.ReadOnly(culture);
    }

    /// <summary>Request localization: ar (default) + en, decided by <see cref="UrlCultureProvider"/> ONLY.</summary>
    public static void Configure(RequestLocalizationOptions options)
    {
        options.DefaultRequestCulture = new RequestCulture(Arabic, Arabic);
        options.SupportedCultures = All;
        options.SupportedUICultures = All;
        options.FallBackToParentCultures = false;
        options.FallBackToParentUICultures = false;
        // The provider list is REPLACED: no query-string, cookie or Accept-Language provider can change a page.
        options.RequestCultureProviders = [new UrlCultureProvider()];
    }
}

/// <summary>
/// "/en" and "/en/…" are English; every other path (including /Admin, /Account, static files) is Arabic. Read from
/// the path, so it works before routing and for attribute-routed actions alike.
/// </summary>
public sealed class UrlCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var name = CultureUrls.IsEnglishPath(httpContext.Request.Path) ? Cultures.EnglishName : Cultures.ArabicName;
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(name, name));
    }
}
