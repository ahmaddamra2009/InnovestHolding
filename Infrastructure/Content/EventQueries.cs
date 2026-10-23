using System.Linq.Expressions;
using InnovestHolding.Models;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>The display row of an uploaded image, from its entity.</summary>
public static class MediaRefs
{
    public static MediaRef? Of(MediaFile? m) => m is null ? null : new MediaRef(m.FileKey, m.Format, m.Width, m.Height, m.AltAr, m.AltEn);
}

/// <summary>The one ordering and projection of events, shared by the snapshot, the events list and the sitemap.</summary>
public static class EventQueries
{
    /// <summary>
    /// Upcoming first — an event with no date (the free-text day/month of the older rows) or one that has not ended
    /// yet — by start date (undated first) then admin order; then past events, most recent first.
    /// An event is past when it has ended (its end, or its start when it has no end, is before now).
    /// </summary>
    public static IOrderedQueryable<Event> Ordered(IQueryable<Event> events, DateTimeOffset now) =>
        events
            .OrderBy(e => (e.EndsAt ?? e.StartsAt) < now ? 1 : 0)
            .ThenBy(e => (e.EndsAt ?? e.StartsAt) < now ? null : e.StartsAt)
            .ThenByDescending(e => (e.EndsAt ?? e.StartsAt) < now ? e.StartsAt : null)
            .ThenBy(e => e.SortOrder)
            .ThenBy(e => e.Id);

    public static bool IsPast(DateTimeOffset? startsAt, DateTimeOffset? endsAt, DateTimeOffset now) =>
        (endsAt ?? startsAt) is { } end && end < now;

    public static readonly Expression<Func<Event, EventRow>> Row = e => new EventRow(e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.EventDay, e.EventMonthAr, e.EventMonthEn, e.LocationAr, e.LocationEn, e.TimeAr, e.TimeEn,
        e.Slug, e.SummaryAr, e.SummaryEn, e.StartsAt, e.EndsAt, e.Status,
        e.Image == null ? null : new MediaRef(e.Image.FileKey, e.Image.Format, e.Image.Width, e.Image.Height, e.Image.AltAr, e.Image.AltEn),
        e.DateIsMonth);

    /// <summary>
    /// F10: an event the visitor can still register for: not past (<see cref="IsPast"/>; an undated event never is).
    /// Written out for SQL: a NULL end/start is "not past", never dropped by a NULL comparison.
    /// </summary>
    public static IQueryable<Event> NotPast(IQueryable<Event> events, DateTimeOffset now) =>
        events.Where(e => (e.EndsAt ?? e.StartsAt) == null || (e.EndsAt ?? e.StartsAt) >= now);

    /// <summary>
    /// F10: an event page has text of its own (an Arabic summary or description). Without it the page is thin:
    /// noindex,follow and out of the sitemap (the 2026 programme rows carry a title, a month and a place only).
    /// </summary>
    public static bool HasText(string? summaryAr, string? descriptionAr) =>
        !string.IsNullOrWhiteSpace(summaryAr) || !string.IsNullOrWhiteSpace(descriptionAr);

    /// <summary>
    /// An event's English page is complete — indexable, in the sitemap and hreflang — when it has an English title and
    /// an English summary (the description stands in for the summary on events created before the summary existed).
    /// </summary>
    public static bool HasEnglish(string? titleEn, string? summaryEn, string? descriptionEn) =>
        !string.IsNullOrWhiteSpace(titleEn) && (!string.IsNullOrWhiteSpace(summaryEn) || !string.IsNullOrWhiteSpace(descriptionEn));

    /// <summary>A news item's English page is complete with an English title and summary.</summary>
    public static bool NewsHasEnglish(string? titleEn, string? summaryEn) =>
        !string.IsNullOrWhiteSpace(titleEn) && !string.IsNullOrWhiteSpace(summaryEn);
}
