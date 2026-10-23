using System.Globalization;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// The app's IStringLocalizerFactory: the resx factory, with every localizer wrapped so a public request reads the
/// admin's overrides (<see cref="PublicTexts"/>). IStringLocalizer&lt;SharedResource&gt;, the view localizer, the
/// DataAnnotations and model-binding messages all come from this factory, so no view or controller has to change.
/// </summary>
public sealed class PublicTextLocalizerFactory(ResourceManagerStringLocalizerFactory inner, IHttpContextAccessor http) : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource) => new PublicTextLocalizer(inner.Create(resourceSource), http);

    public IStringLocalizer Create(string baseName, string location) => new PublicTextLocalizer(inner.Create(baseName, location), http);
}

/// <summary>
/// Reads the override of a listed key from the request's content snapshot (<see cref="PublicTextsMiddleware.ItemKey"/>,
/// present on public requests only), in the request's language; anything else — an admin request, an unlisted key, no
/// row, an empty value — is the resx text. An override that does not format (never saved: the admin screen refuses it)
/// falls back to the resx as well, so a page can never fail on a text.
/// </summary>
public sealed class PublicTextLocalizer(IStringLocalizer inner, IHttpContextAccessor http) : IStringLocalizer
{
    private string? Override(string name)
    {
        if (http.HttpContext?.Items[PublicTextsMiddleware.ItemKey] is not IReadOnlyDictionary<string, TextPair> content) return null;
        var value = PublicTexts.Find(name) is { } text ? PublicTexts.Override(content, text, Cultures.IsEnglishRequest) : null;
        // Defence in depth for a row written outside the admin screen: an alignment/format part ({0,999999}) is ignored.
        return PublicTexts.HasFormatSpec(value) ? null : value;
    }

    public LocalizedString this[string name] =>
        Override(name) is { } value ? new LocalizedString(name, value, resourceNotFound: false) : inner[name];

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (Override(name) is { } value)
            {
                try
                {
                    return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false);
                }
                catch (FormatException)
                {
                    // falls through to the resx text
                }
            }
            return inner[name, arguments];
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => inner.GetAllStrings(includeParentCultures);
}

/// <summary>
/// Puts the cached content snapshot's SiteContents on every public request (after request localization, before the rate
/// limiter so its 429 text is overridable too). Admin and login requests are skipped: their texts stay the resx.
/// A cache hit costs no query; if loading the snapshot throws, the request continues without overrides (resx texts)
/// instead of failing here (PublicTextsTests.Middleware_continues_with_resx_when_the_snapshot_cannot_be_loaded).
/// </summary>
public sealed class PublicTextsMiddleware(RequestDelegate next, ILogger<PublicTextsMiddleware> logger)
{
    public const string ItemKey = "InnovestHolding.PublicTexts";

    public async Task InvokeAsync(HttpContext context, ISiteContentCache cache)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                context.Items[ItemKey] = (await cache.GetAsync(context.RequestAborted)).Content;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("Public text overrides unavailable for this request ({Type}); the resx texts are used.", ex.GetType().Name);
            }
        }
        await next(context);
    }
}
