using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// A past edition of the forum (or one of its side events), shown on /forum/editions with its photos (F10). Text is
/// plain: a blank line starts a new paragraph (the Page.Body rule). The migration AddForumContent inserts the editions of
/// the event profile; their photos are imported once at startup (Infrastructure/Content/ForumMediaImport).
/// </summary>
public class ForumEdition : ISortable
{
    public const short MinYear = 2000;
    public const short MaxYear = 2100;

    /// <summary>Most photos one edition may hold (enforced server-side by the admin).</summary>
    public const int MaxPhotos = 12;

    public int Id { get; set; }

    /// <summary>The year of the edition (validated <see cref="MinYear"/>–<see cref="MaxYear"/>).</summary>
    public short Year { get; set; }

    /// <summary>Free text, e.g. «النسخة الرابعة» / "Fourth Edition" (side events carry their parent's label).</summary>
    [MaxLength(60)]
    public string EditionLabelAr { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? EditionLabelEn { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    [MaxLength(200)]
    public string PlaceAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? PlaceEn { get; set; }

    [MaxLength(4000)]
    public string TextAr { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? TextEn { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Shown on the public site. No model default (EF would skip an explicit false on insert).</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC; the editions page's lastmod.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>In <see cref="ForumEditionPhoto.SortOrder"/> order the first one is the cover (no column).</summary>
    public List<ForumEditionPhoto> Photos { get; set; } = [];
}

/// <summary>
/// One photo of a <see cref="ForumEdition"/>. The image is an ordinary MediaFile with ONE owner (this row; the FK is
/// unique): its alt text lives in MediaFile.AltAr/AltEn. Deleting the edition cascades to these rows; the controller
/// removes the MediaFile rows and their files itself (the SaveWithImageAsync pattern).
/// </summary>
public class ForumEditionPhoto : ISortable
{
    public int Id { get; set; }

    public int ForumEditionId { get; set; }
    public ForumEdition? ForumEdition { get; set; }

    public int ImageId { get; set; }
    public MediaFile? Image { get; set; }

    public int SortOrder { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
