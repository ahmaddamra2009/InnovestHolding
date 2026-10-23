using System.Linq.Expressions;
using System.Text.RegularExpressions;
using InnovestHolding.Models;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>A person card or profile row (speaker, guest).</summary>
public sealed record PersonRow(string Slug, string NameAr, string? NameEn, string JobTitleAr, string? JobTitleEn,
    string OrganizationAr, string? OrganizationEn, MediaRef? Photo);

public sealed record EpisodeRow(string Slug, int? Season, int? Number, string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn,
    DateOnly? AiredOn, MediaRef? Thumbnail);

public sealed record OpportunityRow(string Slug, string TitleAr, string? TitleEn, string SummaryAr, string? SummaryEn,
    string? SectorAr, string? SectorEn, string StageTextAr, string? StageTextEn, string AskTextAr, string? AskTextEn, bool IsOpen, MediaRef? Image);

/// <summary>A sponsorship package as its page lists it (cached in the snapshot: a short list).</summary>
public sealed record PackageRow(string Unit, string TitleAr, string? TitleEn, string PriceTextAr, string? PriceTextEn, string BenefitsAr, string? BenefitsEn);

/// <summary>
/// The orderings, projections and "English page complete" rules of the ecosystem entities, shared by the snapshot
/// (list counts), the list and detail pages and the sitemap — so a list's indexability, its rows and the sitemap
/// cannot disagree. Each English rule exists twice: as a SQL-translatable expression (counts) and as a function
/// (a loaded row); <c>EcosystemRulesAgree</c> in the tests pins them together.
/// </summary>
public static class EcosystemQueries
{
    private static bool Has(string? s) => !string.IsNullOrWhiteSpace(s);

    // ── People (speakers, guests) ──

    /// <summary>
    /// A profile's English page is complete with an English name, and an English job title and bio wherever the
    /// Arabic ones exist (the card shows the job title, the page the bio).
    /// </summary>
    public static bool PersonHasEnglish(string? nameEn, string? jobTitleAr, string? jobTitleEn, string? bioAr, string? bioEn) =>
        Has(nameEn) && (!Has(jobTitleAr) || Has(jobTitleEn)) && (!Has(bioAr) || Has(bioEn));

    /// <summary>
    /// A profile page is indexed only with a description in that language (a bio or a meta description): a name alone
    /// is a thin page. Until then it renders as <c>noindex, follow</c> and stays out of the sitemap.
    /// </summary>
    public static bool PersonHasDescription(string? bio, string? metaDescription) => Has(bio) || Has(metaDescription);

    public static Expression<Func<T, bool>> PersonEnglish<T>() where T : PersonProfile =>
        p => p.NameEn != null && p.NameEn != ""
             && (p.JobTitleAr == "" || (p.JobTitleEn != null && p.JobTitleEn != ""))
             && (p.BioAr == "" || (p.BioEn != null && p.BioEn != ""));

    public static IOrderedQueryable<T> PeopleOrdered<T>(IQueryable<T> people) where T : PersonProfile =>
        people.OrderBy(p => p.SortOrder).ThenBy(p => p.Id);

    public static Expression<Func<T, PersonRow>> PersonCard<T>() where T : PersonProfile =>
        p => new PersonRow(p.Slug, p.NameAr, p.NameEn, p.JobTitleAr, p.JobTitleEn, p.OrganizationAr, p.OrganizationEn,
            p.Photo == null ? null : new MediaRef(p.Photo.FileKey, p.Photo.Format, p.Photo.Width, p.Photo.Height, p.Photo.AltAr, p.Photo.AltEn));

    // ── Episodes ──

    /// <summary>An episode's English page is complete with an English title and summary (as a news item).</summary>
    public static bool EpisodeHasEnglish(string? titleEn, string? summaryEn) => Has(titleEn) && Has(summaryEn);

    public static readonly Expression<Func<TvEpisode, bool>> EpisodeEnglish =
        e => e.TitleEn != null && e.TitleEn != "" && e.SummaryEn != null && e.SummaryEn != "";

    /// <summary>Most recently aired first (undated episodes after the dated ones), then the admin order.</summary>
    public static IOrderedQueryable<TvEpisode> EpisodesOrdered(IQueryable<TvEpisode> episodes) =>
        episodes.OrderBy(e => e.AiredOn == null ? 1 : 0).ThenByDescending(e => e.AiredOn).ThenBy(e => e.SortOrder).ThenBy(e => e.Id);

    public static readonly Expression<Func<TvEpisode, EpisodeRow>> EpisodeCard = e => new EpisodeRow(e.Slug, e.Season, e.Number,
        e.TitleAr, e.TitleEn, e.SummaryAr, e.SummaryEn, e.AiredOn,
        e.Thumbnail == null ? null : new MediaRef(e.Thumbnail.FileKey, e.Thumbnail.Format, e.Thumbnail.Width, e.Thumbnail.Height, e.Thumbnail.AltAr, e.Thumbnail.AltEn));

    // ── Opportunities ──

    /// <summary>An opportunity's English page is complete with an English title and summary.</summary>
    public static bool OpportunityHasEnglish(string? titleEn, string? summaryEn) => Has(titleEn) && Has(summaryEn);

    public static readonly Expression<Func<InvestmentOpportunity, bool>> OpportunityEnglish =
        o => o.TitleEn != null && o.TitleEn != "" && o.SummaryEn != null && o.SummaryEn != "";

    /// <summary>Open ones first, then the admin order.</summary>
    public static IOrderedQueryable<InvestmentOpportunity> OpportunitiesOrdered(IQueryable<InvestmentOpportunity> items) =>
        items.OrderByDescending(o => o.IsOpen).ThenBy(o => o.SortOrder).ThenBy(o => o.Id);

    public static readonly Expression<Func<InvestmentOpportunity, OpportunityRow>> OpportunityCard = o => new OpportunityRow(o.Slug,
        o.TitleAr, o.TitleEn, o.SummaryAr, o.SummaryEn,
        o.SectorItem == null ? null : o.SectorItem.TextAr, o.SectorItem == null ? null : o.SectorItem.TextEn,
        o.StageTextAr, o.StageTextEn, o.AskTextAr, o.AskTextEn, o.IsOpen,
        o.Image == null ? null : new MediaRef(o.Image.FileKey, o.Image.Format, o.Image.Width, o.Image.Height, o.Image.AltAr, o.Image.AltEn));
}

/// <summary>
/// The YouTube video id of a stored https video URL, for the click-to-load player. Only the YouTube hosts below and the
/// usual address shapes are recognised; anything else is not a YouTube video (the page then shows a plain link).
/// </summary>
public static partial class YouTube
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex IdPattern();

    private static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "www.youtube.com", "m.youtube.com", "youtube-nocookie.com", "www.youtube-nocookie.com", "youtu.be",
    };

    /// <summary>The privacy-enhanced embed address the player loads after the click.</summary>
    public static string EmbedUrl(string id) => "https://www.youtube-nocookie.com/embed/" + id;

    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    public static string? VideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps || !Hosts.Contains(uri.Host) || !uri.IsDefaultPort) return null;
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;
        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
            id = segments.Length == 1 ? segments[0] : null;
        else if (segments.Length == 1 && segments[0] == "watch")
            id = QueryValue(uri.Query, "v");
        else if (segments.Length == 2 && segments[0] is "embed" or "shorts" or "live")
            id = segments[1];
        return IsValidId(id) ? id : null;
    }

    private static string? QueryValue(string query, string name)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq] == name) return Uri.UnescapeDataString(part[(eq + 1)..]);
        }
        return null;
    }
}
