using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using InnovestHolding.ViewModels.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Controllers;

public class HomeController(
    AppDbContext db,
    IHomeContentCache cache,
    IStringLocalizer<SharedResource> l,
    IWebHostEnvironment environment) : Controller
{
    /// <summary>The logo file (an asset path, not content).</summary>
    private const string LogoPath = "/assets/logo.png";

    /// <summary>GET / (Arabic) and GET /en (English). Data from the cached snapshot: 0 queries on a cache hit.</summary>
    public async Task<IActionResult> Index()
    {
        var english = Cultures.IsEnglishRequest;
        var snapshot = await cache.GetAsync(HttpContext.RequestAborted);
        var content = new ContentVm(snapshot.Content, english);
        var baseUrl = SiteUrl.Resolve(content.Neutral(SiteUrl.ContentKey), environment, Request);
        return View(HomeViewModel.Create(snapshot, english, BuildSeo(snapshot, content, english, baseUrl)));
    }

    // Pinned routes: /Home/Register/{id} and /Admin/Home/Register do not reach it. The English page
    // posts to /en/Register; both URLs share one rate-limit partition (it is keyed on the ACTION).
    [HttpPost("/Home/Register")]
    [HttpPost("/en/Register")]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Register(RegistrationInput input)
    {
        // Honeypot filled: answer exactly like a success and write nothing (do not tip off the bot).
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = l[MessageKeys.RegistrationSuccess].Value });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        // The interest must be one of the ACTIVE options; when none is active the field must be absent.
        var options = (await cache.GetAsync(HttpContext.RequestAborted)).Interests;
        InterestRow? option = null;
        if (options.Count > 0)
        {
            option = options.FirstOrDefault(o => o.Id == input.InterestOptionId);
            if (option is null)
                return BadRequest(new { success = false, message = l[MessageKeys.InterestInvalid].Value });
        }
        else if (input.InterestOptionId is not null)
        {
            return BadRequest(new { success = false, message = l[MessageKeys.InterestInvalid].Value });
        }

        db.Registrations.Add(new Registration
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = input.Phone?.Trim() ?? "",
            InterestOptionId = option?.Id,
            Interest = option?.NameAr ?? "",
            Culture = Cultures.CurrentName,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Json(new { success = true, message = l[MessageKeys.RegistrationSuccess].Value });
    }

    [HttpPost("/Home/Contact")]
    [HttpPost("/en/Contact")]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Contact(ContactInput input)
    {
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = l[MessageKeys.ContactSuccess].Value });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        db.ContactMessages.Add(new ContactMessage
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Subject = input.Subject!.Trim(),
            Message = input.Message!.Trim(),
            Culture = Cultures.CurrentName,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Json(new { success = true, message = l[MessageKeys.ContactSuccess].Value });
    }

    /// <summary>
    /// Target of UseExceptionHandler. The handler re-executes the ORIGINAL method, so a failed POST
    /// arrives here as a POST without a valid token — hence the only [IgnoreAntiforgeryToken] in the app.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private string FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? l[MessageKeys.InvalidValueNoField].Value;

    private static SeoVm BuildSeo(HomeSnapshot snapshot, ContentVm content, bool english, string? baseUrl)
    {
        var siteName = content.Text("site_name").Value;
        var title = content.Text("seo_title").Value;
        var description = content.Text("seo_description").Value;
        var fullTitle = string.IsNullOrEmpty(title) ? siteName : $"{title} | {siteName}";

        string? Abs(string path) => baseUrl is null ? null : baseUrl + path;
        var canonical = Abs(CultureUrls.Home(english));
        var ogImagePath = content.Neutral("seo_og_image");
        var ogImage = SiteUrl.IsSitePath(ogImagePath) ? Abs(ogImagePath) : null;

        return new SeoVm(
            Lang: english ? Cultures.EnglishName : Cultures.ArabicName,
            Title: fullTitle,
            Description: description,
            SiteName: siteName,
            Canonical: canonical,
            AlternateAr: Abs(CultureUrls.ArabicHome),
            AlternateEn: Abs(CultureUrls.EnglishHome),
            OgLocale: english ? "en_US" : "ar_AR",
            OgLocaleAlternate: english ? "ar_AR" : "en_US",
            OgImage: ogImage,
            OgImageAlt: content.Text("seo_og_image_alt").Value,
            JsonLd: BuildJsonLd(snapshot, content, english, canonical, Abs(LogoPath), description, siteName));
    }

    /// <summary>
    /// Organization JSON-LD built as an object and serialised by System.Text.Json (its JavaScriptEncoder always escapes
    /// &lt; and &gt;, so a value containing &lt;/script&gt; cannot close the tag). Empty values are omitted.
    /// </summary>
    public static string BuildJsonLd(HomeSnapshot snapshot, ContentVm content, bool english, string? url, string? logo,
        string description, string siteName)
    {
        static void Put(Dictionary<string, object> d, string key, object? value)
        {
            if (value is string s && string.IsNullOrWhiteSpace(s)) return;
            if (value is null) return;
            if (value is Dictionary<string, object> nested && nested.Count <= 1) return; // only "@type" left
            if (value is string[] array && array.Length == 0) return;
            d[key] = value;
        }

        var other = new ContentVm(snapshot.Content, !english).Text("site_name").Value;
        var email = content.Neutral("contact_email");
        var phone = content.Neutral("contact_phone");

        var address = new Dictionary<string, object> { ["@type"] = "PostalAddress" };
        Put(address, "streetAddress", content.Text("contact_address").Value);
        Put(address, "addressLocality", content.Neutral("org_address_locality"));
        Put(address, "addressCountry", content.Neutral("org_address_country"));

        var contactPoint = new Dictionary<string, object> { ["@type"] = "ContactPoint" };
        Put(contactPoint, "email", email);
        Put(contactPoint, "telephone", phone);
        if (contactPoint.Count > 1)
        {
            contactPoint["contactType"] = "customer service";
            contactPoint["availableLanguage"] = new[] { "ar", "en" };
        }

        var org = new Dictionary<string, object> { ["@context"] = "https://schema.org", ["@type"] = "Organization" };
        Put(org, "name", siteName);
        Put(org, "alternateName", other == siteName ? null : other);
        Put(org, "url", url);
        Put(org, "logo", logo);
        Put(org, "description", description);
        Put(org, "email", email);
        Put(org, "telephone", phone);
        Put(org, "address", address);
        Put(org, "contactPoint", contactPoint);
        Put(org, "sameAs", snapshot.Socials.Where(s => SiteUrl.IsHttpsUrl(s.Url)).Select(s => s.Url.Trim()).ToArray());

        return JsonSerializer.Serialize(org, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) });
    }
}
