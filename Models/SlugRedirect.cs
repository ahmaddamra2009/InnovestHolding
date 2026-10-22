using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

/// <summary>
/// A slug an item no longer uses: its old URL answers 301 to the item's current URL, and no other item of the same
/// type may take it. Written when an admin changes a slug; removed with the item.
/// </summary>
public class SlugRedirect
{
    public int Id { get; set; }

    /// <summary><see cref="SlugTypes"/>: the kind of item (news, event, speaker…).</summary>
    [Required, MaxLength(20)]
    public string EntityType { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OldSlug { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class SlugTypes
{
    public const string News = "news";
    public const string Event = "event";
    public const string Speaker = "speaker";
    public const string Guest = "guest";
    public const string Episode = "episode";
    public const string Opportunity = "opportunity";
}
