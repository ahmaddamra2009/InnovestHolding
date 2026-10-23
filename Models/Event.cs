using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(330)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(330)]
    public string? TitleEn { get; set; }

    public string DescriptionAr { get; set; } = string.Empty;

    public string? DescriptionEn { get; set; }

    public int EventDay { get; set; }

    [MaxLength(80)]
    public string EventMonthAr { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EventMonthEn { get; set; }

    [MaxLength(330)]
    public string LocationAr { get; set; } = string.Empty;

    [MaxLength(330)]
    public string? LocationEn { get; set; }

    [MaxLength(170)]
    public string TimeAr { get; set; } = string.Empty;

    [MaxLength(170)]
    public string? TimeEn { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Address of the detail page (/forum/events/{slug}): ASCII, lower-case, unique, shared by both languages.</summary>
    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Card and meta-description text; the description above is the detail page's main text.</summary>
    [MaxLength(840)]
    public string SummaryAr { get; set; } = string.Empty;

    [MaxLength(840)]
    public string? SummaryEn { get; set; }

    /// <summary>Start with the event's own UTC offset. NULL: the free-text day/month/time above are shown instead.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>
    /// F10: the event is known to the month only. <see cref="StartsAt"/> is then the 1st of the month 00:00 and
    /// <see cref="EndsAt"/> the last day 23:59 (at the event's offset), so ordering and past/upcoming need no special
    /// case; the pages show month + year without a day, and the JSON-LD is a WebPage, never an Event.
    /// </summary>
    public bool DateIsMonth { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    public AttendanceMode AttendanceMode { get; set; } = AttendanceMode.Offline;

    [MaxLength(170)]
    public string CityAr { get; set; } = string.Empty;

    [MaxLength(170)]
    public string? CityEn { get; set; }

    [MaxLength(500)]
    public string AddressAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AddressEn { get; set; }

    /// <summary>https link of an online or mixed event.</summary>
    [MaxLength(500)]
    public string? OnlineUrl { get; set; }

    /// <summary>The detail page shows the registration call to action (never for a past or cancelled event).</summary>
    public bool RegistrationOpen { get; set; } = true;

    public int? ImageId { get; set; }
    public MediaFile? Image { get; set; }

    [MaxLength(330)]
    public string MetaTitleAr { get; set; } = string.Empty;

    [MaxLength(330)]
    public string? MetaTitleEn { get; set; }

    [MaxLength(500)]
    public string MetaDescriptionAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MetaDescriptionEn { get; set; }

    /// <summary>UTC; the sitemap's lastmod.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>schema.org eventStatus (stored as tinyint).</summary>
public enum EventStatus : byte
{
    Scheduled = 0,
    Postponed = 1,
    Cancelled = 2,
}

/// <summary>schema.org eventAttendanceMode (stored as tinyint).</summary>
public enum AttendanceMode : byte
{
    Offline = 0,
    Online = 1,
    Mixed = 2,
}
