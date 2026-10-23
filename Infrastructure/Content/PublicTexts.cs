using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Localization;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using U = InnovestHolding.Infrastructure.Localization.MessageKeys.Ui;
using M = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>A resx key the public site shows, and the admin tab it is listed under.</summary>
public sealed record PublicText(string ResxKey, string Group)
{
    /// <summary>The SiteContents row that overrides it (ContentAr / ContentEn; empty = the resx text of that language).</summary>
    public string RowKey => PublicTexts.Prefix + ResxKey;
}

/// <summary>
/// The override layer of the public interface texts (Admin › رسائل وأزرار الموقع). Every resx key the PUBLIC pages and
/// JSON replies use is listed here; a SiteContents row <c>ui.&lt;resx key&gt;</c> overrides it per language. Nothing writes
/// such a row except the admin screen, so a database without rows renders exactly the resx texts (no visible change on
/// deploy, no migration). Applied by <see cref="PublicTextLocalizerFactory"/> to every IStringLocalizer read of a public
/// request (views, controllers, validation and model-binding messages, the rate limiter's 429) from the cached snapshot
/// that <see cref="PublicTextsMiddleware"/> puts on the request. Admin-only keys (Admin_*, the login messages, the admin
/// field names) are not listed: they always come from the resx.
/// </summary>
public static class PublicTexts
{
    public const string Prefix = "ui.";
    public const int MaxLength = 300;

    public static readonly IReadOnlyList<(string Id, string Title)> Groups =
    [
        ("forms", "النماذج (الحقول والأزرار)"),
        ("messages", "رسائل الشكر والأخطاء"),
        ("details", "تسميات صفحات التفاصيل"),
        ("badges", "الشارات والحالات"),
        ("navigation", "التنقل وإمكانية الوصول"),
        ("errors", "صفحة 404 وصفحة الخطأ"),
        ("other", "أخرى"),
    ];

    private static IEnumerable<PublicText> In(string group, params string[] keys) => keys.Select(k => new PublicText(k, group));

    public static readonly IReadOnlyList<PublicText> All =
    [
        .. In("forms",
            U.FormFullName, U.FormFullNamePlaceholderRegister, U.FormFullNamePlaceholderContact, U.FormEmail, U.FormEmailPlaceholder,
            U.FormPhone, U.FormPhonePlaceholder, U.FormInterest, U.FormInterestPlaceholder, U.FormSubject, U.FormSubjectPlaceholder,
            U.FormMessage, U.FormMessagePlaceholder, U.FormRequiredNote, U.FormChoose, U.FormOrganization, U.FormJobTitle,
            U.FormCountry, U.FormEvent, U.FormEventAny, U.FormKindInvestor, U.FormKindStartup, U.FormInvestorDetails,
            U.FormStartupDetails, U.FormInvestorType, U.FormTicketSize, U.FormSectors, U.FormStartupName, U.FormSector,
            U.FormStage, U.FormFunding, U.FormWebsite, U.FormLinkedIn, U.FormPitch, U.FormCompanyName, U.FormContactName,
            U.FormUnit, U.FormPackage, U.FormPackageAny, U.FormOpportunityAny,
            U.FormSubmitApplication, U.FormSubmitEnquiry, U.FormSubmitInterest, U.ContactSubmit,
            U.FormRegistering, U.FormSending,
            // Field names inside the validation messages of the public forms ({0} of "{0} is required").
            F.FullName, F.Email, F.Phone, F.Interest, F.Subject, F.Message, F.Consent, F.Country, F.Event, F.ApplicantType,
            F.StartupName, F.InvestorType, F.TicketSize, F.Sectors, F.Sector, F.FundingSought, F.WebsiteUrl, F.Pitch,
            F.CompanyName, F.Unit, F.Package, F.Opportunity, F.JobTitle, F.Organization, F.LinkedInUrl, F.Stage),
        .. In("messages",
            M.RegistrationSuccess, M.ContactSuccess, M.ForumRegistrationSuccess, M.JoinSuccess, M.SponsorshipSuccess, M.InterestSuccess,
            M.TooManyRequests, M.LeadFormFull, U.FormGenericError,
            M.Required, M.MaxLength, M.LengthRange, M.Range, M.Email, M.Phone, M.HttpsUrl, M.UnsafeHref, M.InvalidValue,
            M.ValueRequired, M.InvalidValueNoField, M.ValueRequiredNoField, M.InterestInvalid, M.OptionInvalid, M.ConsentRequired),
        .. In("details",
            U.EventDate, U.EventTime, U.EventVenue, U.EventOnline, U.EventJoinOnline, U.EventEndedNotice, U.PublishedOn,
            U.NewsSource, U.ArabicOnlyNotice, U.PlayVideo, U.VideoNotice, U.WatchVideo, U.WatchOnYouTube, U.ExternalLink,
            U.SeasonEpisode, U.EpisodeNumber, U.SeasonNumber, U.AiredOn, U.JobTitle, U.Organization, U.Sector, U.Stage, U.Ask,
            U.ContactAddress, U.ContactPhone, U.ContactEmail, U.ContactHours, U.ContactWhatsApp, U.ContactWebsite, U.ContactSocial),
        .. In("badges", U.EventCancelled, U.EventPostponed, U.EventPast, U.OpportunityOpen, U.OpportunityClosed),
        .. In("navigation",
            U.LangSwitchText, U.LangSwitchAria, U.LangDrawerText, U.NavAria, U.MenuAria, U.BreadcrumbAria, U.SubmenuToggle,
            U.PagerAria, U.PreviousPage, U.NextPage, U.PageOf, U.SkipToContent, U.BackToTop, U.ProfilesAria),
        .. In("errors", U.NotFoundTitle, U.NotFoundText, M.ErrorTitle, M.ErrorGeneric, M.ErrorReference, M.BackToHome),
        .. In("other", U.Explore, U.ViewAll, U.ViewDetails, U.ExpressInterest, U.SponsorshipPackages),
    ];

    private static readonly Dictionary<string, PublicText> ByKey = All.ToDictionary(t => t.ResxKey, StringComparer.Ordinal);

    public static PublicText? Find(string? resxKey) => resxKey is null ? null : ByKey.GetValueOrDefault(resxKey);

    private static readonly ResourceManager Resources = new("InnovestHolding.Resources.SharedResource", typeof(SharedResource).Assembly);

    /// <summary>The resx text of the language: what the site shows while no override is set (the admin's placeholder).</summary>
    public static string Default(PublicText text, bool english) =>
        Resources.GetString(text.ResxKey, english ? Cultures.English : Cultures.Arabic) ?? text.ResxKey;

    /// <summary>The override of the language from the snapshot's SiteContents, or null (no row, empty value).</summary>
    public static string? Override(IReadOnlyDictionary<string, TextPair> content, PublicText text, bool english)
    {
        var row = content.GetValueOrDefault(text.RowKey);
        var value = english ? row?.En : row?.Ar;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static readonly Regex Placeholder = new(@"\{(\d+)(?:[,:][^{}]*)?\}", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>The format placeholders ({0}, {1}…) of a text, sorted and distinct.</summary>
    public static IReadOnlyList<string> Placeholders(string? value) =>
        Placeholder.Matches(value ?? "").Select(m => "{" + m.Groups[1].Value + "}").Distinct().Order(StringComparer.Ordinal).ToList();

    /// <summary>
    /// An override must keep exactly the placeholders of the default ({0} = the field name, the e-mail…) and stay a valid
    /// format string when the default is one; a text without placeholders is never formatted, so its braces are free.
    /// </summary>
    public static bool PlaceholdersMatch(string value, string defaultText)
    {
        var expected = Placeholders(defaultText);
        if (!Placeholders(value).SequenceEqual(expected)) return false;
        if (expected.Count == 0) return true;
        try
        {
            _ = string.Format(CultureInfo.InvariantCulture, value, Enumerable.Range(0, 10).Select(i => (object)i).ToArray());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static readonly Regex FormatSpec = new(@"\{\s*\d+\s*[,:]", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>
    /// A placeholder with an alignment or format part ({0,999999}, {1:D9999999}): refused by the admin and ignored by the
    /// localizer, so an override can never make a reply grow (only bare {n} placeholders are allowed).
    /// </summary>
    public static bool HasFormatSpec(string? value) => !string.IsNullOrEmpty(value) && FormatSpec.IsMatch(value);

    private static readonly Regex Scheme = new(@"(?:java|vb)script\s*:|data\s*:", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Markup, a script/data URL scheme (javascript:, vbscript:, data:), or a link the default does not have: these are
    /// plain words (encoded wherever they are shown).
    /// </summary>
    public static bool HasMarkupOrLink(string value, string defaultText) =>
        value.Contains('<') || value.Contains('>') || Scheme.IsMatch(value)
        || (value.Contains("://", StringComparison.Ordinal) && !defaultText.Contains("://", StringComparison.Ordinal));
}
