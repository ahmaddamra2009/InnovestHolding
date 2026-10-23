using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// An image uploaded from the admin. The bytes live on disk (Infrastructure/Media/MediaStore) under a random
/// <see cref="FileKey"/>; the row holds what the pages need to render it. Each file has ONE owner (a news item, an
/// event, a partner, a page, the founder block): the owner's FK points here and the app deletes the row and the
/// file together when the owner replaces or loses it.
/// </summary>
public class MediaFile
{
    public int Id { get; set; }

    /// <summary>32 lower-case hex characters (a GUID "N"), the only input of the file's path and URL.</summary>
    [Required, MaxLength(32)]
    public string FileKey { get; set; } = string.Empty;

    /// <summary>"jpeg", "png" or "webp" — detected from the file's signature, never from the client.</summary>
    [Required, MaxLength(4)]
    public string Format { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Size of the stored file.</summary>
    public int Bytes { get; set; }

    [MaxLength(200)]
    public string AltAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AltEn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
