using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// The admin-managed texts of one public page. <see cref="Key"/> is a key of the route table
/// (Infrastructure/Site/SiteRoutes): the page's URL is code, every visible text is here. The rows are owned by the
/// migrations (one per non-detail route); the admin edits them but cannot add or remove a page.
/// </summary>
public class Page
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Menu label, &lt;h1&gt; and breadcrumb label.</summary>
    [Required, MaxLength(150)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? TitleEn { get; set; }

    /// <summary>Card text (hub cards, mega-menu) and the fallback meta description.</summary>
    [MaxLength(500)]
    public string SummaryAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SummaryEn { get; set; }

    /// <summary>Lead paragraph under the title.</summary>
    [MaxLength(2000)]
    public string IntroAr { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? IntroEn { get; set; }

    /// <summary>Main text: plain text, a blank line starts a new paragraph.</summary>
    public string BodyAr { get; set; } = string.Empty;

    public string? BodyEn { get; set; }

    /// <summary>Shown when the page has no content yet (a list with no items, a page with no text).</summary>
    [MaxLength(500)]
    public string EmptyTextAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? EmptyTextEn { get; set; }

    [MaxLength(200)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(300)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? MetaDescriptionEn { get; set; }

    /// <summary>Listed in the header, the drawer and the footer. A hidden page stays reachable by its URL.</summary>
    public bool ShowInMenu { get; set; } = true;

    /// <summary>Order among the pages of the same parent.</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC; the sitemap's lastmod.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The page's card image (hub cards) and share image (og:image).</summary>
    public int? ImageId { get; set; }
    public MediaFile? Image { get; set; }
}
