using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// A footer social link. <see cref="Network"/> is one of <see cref="Networks"/>; the icon and the brand name come
/// from code (no SVG or label column, so no new trusted-markup path). An empty <see cref="Url"/> is not rendered.
/// </summary>
public class SocialLink
{
    public static readonly IReadOnlyList<string> Networks = ["linkedin", "twitter", "instagram", "youtube", "facebook", "tiktok", "linktree"];

    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Network { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Url { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
