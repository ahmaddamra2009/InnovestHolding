using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;
/// <summary>An item with its own detail page, as the shared admin CRUD (EcosystemAdminControllers) handles it.</summary>
public interface IDetailEntity
{
    int Id { get; }
    string Slug { get; set; }
    int SortOrder { get; set; }
    DateTime UpdatedAt { get; set; }
}

// The content of the three units that starts empty and is filled by the admin: Forum speakers, TV guests and
// episodes, Network investment opportunities and sponsorship packages. Every text is an Ar/En pair (English falls
// back to Arabic), bodies are plain text (a blank line starts a paragraph), URLs are https only.

/// <summary>
/// A person with a public profile page (a Forum speaker or a TV guest). Not an EF hierarchy: each subclass is its own
/// table with the same columns.
/// </summary>
public abstract class PersonProfile : IDetailEntity
{
    public int Id { get; set; }

    /// <summary>Address of the profile page: ASCII, lower-case, unique per table, shared by both languages.</summary>
    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? NameEn { get; set; }

    [MaxLength(150)]
    public string JobTitleAr { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? JobTitleEn { get; set; }

    [MaxLength(150)]
    public string OrganizationAr { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? OrganizationEn { get; set; }

    public string BioAr { get; set; } = string.Empty;

    public string? BioEn { get; set; }

    public int? PhotoId { get; set; }
    public MediaFile? Photo { get; set; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(500)]
    public string? XUrl { get; set; }

    [MaxLength(200)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(300)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? MetaDescriptionEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC; the sitemap's lastmod.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A speaker of the Innovest Arab Forum (/forum/speakers/{slug}).</summary>
public class Speaker : PersonProfile;

/// <summary>A guest of the TV programme (/tv/guests/{slug}).</summary>
public class TvGuest : PersonProfile;

/// <summary>A speaker of an event; removed with either side.</summary>
public class EventSpeaker
{
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int SpeakerId { get; set; }
    public Speaker? Speaker { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>An episode of the TV programme (/tv/episodes/{slug}).</summary>
public class TvEpisode : IDetailEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    public int? Season { get; set; }

    public int? Number { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    [MaxLength(500)]
    public string SummaryAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SummaryEn { get; set; }

    public string BodyAr { get; set; } = string.Empty;

    public string? BodyEn { get; set; }

    public DateOnly? AiredOn { get; set; }

    /// <summary>https only. A YouTube address plays in a click-to-load player; any other address is a link.</summary>
    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    public int? ThumbnailId { get; set; }
    public MediaFile? Thumbnail { get; set; }

    [MaxLength(200)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(300)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? MetaDescriptionEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A guest of an episode; removed with either side.</summary>
public class TvEpisodeGuest
{
    public int TvEpisodeId { get; set; }
    public TvEpisode? TvEpisode { get; set; }

    public int TvGuestId { get; set; }
    public TvGuest? TvGuest { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// An investment opportunity of the Angel Investors Network (/angel-network/opportunities/{slug}). A draft
/// (IsActive = false) until the admin publishes it. Stage and ask are free text ("on request" is allowed): the site
/// never shows a figure the admin did not write.
/// </summary>
public class InvestmentOpportunity : IDetailEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    [MaxLength(500)]
    public string SummaryAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SummaryEn { get; set; }

    public string BodyAr { get; set; } = string.Empty;

    public string? BodyEn { get; set; }

    /// <summary>A row of the "sectors" list (ContentItems); deleting a sector in use is refused.</summary>
    public int? SectorItemId { get; set; }
    public ContentItem? SectorItem { get; set; }

    [MaxLength(100)]
    public string StageTextAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? StageTextEn { get; set; }

    [MaxLength(100)]
    public string AskTextAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AskTextEn { get; set; }

    public int? ImageId { get; set; }
    public MediaFile? Image { get; set; }

    /// <summary>Still accepting interest (shown as Open / Closed).</summary>
    public bool IsOpen { get; set; } = true;

    [MaxLength(200)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(300)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? MetaDescriptionEn { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Published. A new opportunity starts as a draft.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A sponsorship package of the Forum or the TV programme (listed on its page; no detail page).</summary>
public class SponsorshipPackage
{
    public int Id { get; set; }

    /// <summary><see cref="SponsorshipUnits"/>.</summary>
    [Required, MaxLength(10)]
    public string Unit { get; set; } = SponsorshipUnits.Tv;

    [Required, MaxLength(150)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? TitleEn { get; set; }

    /// <summary>Free text: a price or "on request".</summary>
    [MaxLength(100)]
    public string PriceTextAr { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PriceTextEn { get; set; }

    /// <summary>One benefit per line.</summary>
    [MaxLength(2000)]
    public string BenefitsAr { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? BenefitsEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>The unit a sponsorship package belongs to (code values; the labels are page titles).</summary>
public static class SponsorshipUnits
{
    public const string Forum = "forum";
    public const string Tv = "tv";

    public static readonly IReadOnlyList<string> All = [Forum, Tv];

    public static bool IsKnown(string? unit) => unit is Forum or Tv;
}
