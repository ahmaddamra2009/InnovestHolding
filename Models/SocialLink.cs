using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// A footer social link. <see cref="Network"/> is one of <see cref="Networks"/> (the icon and the brand name come from
/// code) or <see cref="Custom"/> (a plain-text label and an icon KEY of the code whitelist) — never admin SVG, so no
/// new trusted-markup path. An empty <see cref="Url"/> is not rendered.
/// </summary>
public class SocialLink : ISortable
{
    public static readonly IReadOnlyList<string> Networks = ["linkedin", "twitter", "instagram", "youtube", "facebook", "tiktok", "linktree"];

    /// <summary>A network outside <see cref="Networks"/>: it carries its own <see cref="Label"/> and <see cref="IconKey"/>,
    /// and may repeat (the known networks are unique — a filtered unique index).</summary>
    public const string Custom = "custom";

    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Network { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Url { get; set; }

    /// <summary>Custom rows only: the network's name (aria-label / title), one language-neutral value.</summary>
    [MaxLength(60)]
    public string? Label { get; set; }

    /// <summary>Custom rows only: a key of Infrastructure/Content/Icons.</summary>
    [MaxLength(40)]
    public string? IconKey { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
