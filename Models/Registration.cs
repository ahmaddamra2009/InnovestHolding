using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Registration : ILead
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string Phone { get; set; } = "";

    /// <summary>Arabic name of the chosen option when the visitor registered (snapshot: survives option edits/deletes).</summary>
    [MaxLength(100)]
    public string Interest { get; set; } = "";

    public int? InterestOptionId { get; set; }

    public InterestOption? InterestOption { get; set; }

    public const string Quick = "quick";
    public const string Forum = "forum";

    /// <summary>
    /// Which form wrote the row: <see cref="Quick"/> (the home page's quick registration; every row older than the lead
    /// forms) or <see cref="Forum"/> (the Forum registration page). Written by the server, never bound.
    /// </summary>
    [Required, MaxLength(20)]
    public string Kind { get; set; } = Quick;

    /// <summary>The event chosen on the Forum form (null: any event, or the event was deleted later).</summary>
    public int? EventId { get; set; }

    public Event? Event { get; set; }

    /// <summary>Arabic title of the chosen event at registration time (snapshot: survives event edits/deletes).</summary>
    [MaxLength(200)]
    public string? EventTitle { get; set; }

    [MaxLength(200)]
    public string? Organization { get; set; }

    [MaxLength(150)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>UTC time the consent box was ticked (Forum form; null for quick registrations, which have no box).</summary>
    public DateTime? ConsentAt { get; set; }

    /// <summary>Page language the visitor submitted from ("ar" / "en"); written by the server, never bound.</summary>
    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; } = false;

    /// <summary>Where the admin is with this lead (existing rows: New).</summary>
    public LeadStatus Status { get; set; }

    /// <summary>The admin's internal note (never shown on the public site).</summary>
    [MaxLength(LeadLimits.NoteMax)]
    public string? AdminNote { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
