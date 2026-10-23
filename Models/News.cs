using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class News
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    public string SummaryAr { get; set; } = string.Empty;

    public string? SummaryEn { get; set; }

    [MaxLength(50)]
    public string CategoryAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CategoryEn { get; set; }

    /// <summary>Free text per language (no date parsing: the admin writes it as it should read).</summary>
    [MaxLength(50)]
    public string NewsDateAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NewsDateEn { get; set; }

    /// <summary>"Read more" target; the link is not rendered while empty.</summary>
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    /// <summary>Public order (ascending). A new item gets MIN − 1 so it shows first.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Address of the detail page (/news/{slug}): ASCII, lower-case, unique, shared by both languages.</summary>
    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Main text of the detail page: plain text, a blank line starts a new paragraph.</summary>
    public string BodyAr { get; set; } = string.Empty;

    public string? BodyEn { get; set; }

    public int? ImageId { get; set; }
    public MediaFile? Image { get; set; }

    /// <summary>Publication date (NewsArticle datePublished); the free-text date above stays what the cards show.</summary>
    public DateOnly? PublishedOn { get; set; }

    [MaxLength(200)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(300)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? MetaDescriptionEn { get; set; }

    /// <summary>UTC; the sitemap's lastmod and NewsArticle dateModified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
