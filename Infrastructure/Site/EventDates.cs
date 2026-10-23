using System.Globalization;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// Event dates as the visitor reads them: in the event's OWN offset (where it takes place), Gregorian calendar,
/// Latin digits, month and day names in the page language, the offset written next to the time.
/// </summary>
public static class EventDates
{
    // ar-EG: Arabic month/day names on the Gregorian calendar (the neutral "ar" and ar-SA use the Hijri calendar).
    private static readonly CultureInfo Arabic = CultureInfo.GetCultureInfo("ar-EG");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    private static CultureInfo Culture(bool english) => english ? English : Arabic;

    public static string Month(DateTimeOffset at, bool english) => at.ToString("MMMM", Culture(english));

    /// <summary>F10: an event known to the month only (DateIsMonth): "يونيو 2026" / "June 2026".</summary>
    public static string MonthYear(DateTimeOffset at, bool english) => at.ToString("MMMM yyyy", Culture(english));

    /// <summary>A calendar date without a time: "15 يوليو 2026" / "15 July 2026".</summary>
    public static string Date(DateOnly date, bool english) => date.ToString("d MMMM yyyy", Culture(english));

    /// <summary>"الخميس 15 يوليو 2026" / "Thursday 15 July 2026"; a range when the end is on another day.</summary>
    public static string DateLine(DateTimeOffset start, DateTimeOffset? end, bool english)
    {
        var c = Culture(english);
        var first = start.ToString("dddd d MMMM yyyy", c);
        if (end is not { } e || e.Date == start.Date) return first;
        return first + " – " + e.ToString("dddd d MMMM yyyy", c);
    }

    /// <summary>"09:00 – 17:00 (UTC+02:00)".</summary>
    public static string TimeRange(DateTimeOffset start, DateTimeOffset? end, bool english)
    {
        var text = start.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (end is { } e && e.Date == start.Date) text += " – " + e.ToString("HH:mm", CultureInfo.InvariantCulture);
        return text + " (" + Offset(start.Offset) + ")";
    }

    public static string Offset(TimeSpan offset) =>
        "UTC" + (offset < TimeSpan.Zero ? "-" : "+") + offset.Duration().ToString("hh\\:mm", CultureInfo.InvariantCulture);

    /// <summary>ISO 8601 with the offset (schema.org startDate/endDate).</summary>
    public static string Iso(DateTimeOffset at) => at.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    /// <summary>Parses "+02:00" / "-05:30" (the admin's offset field and the default_utc_offset key).</summary>
    public static bool TryParseOffset(string? text, out TimeSpan offset)
    {
        offset = default;
        if (text is not { Length: 6 } || (text[0] != '+' && text[0] != '-') || text[3] != ':') return false;
        if (!int.TryParse(text.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var h)
            || !int.TryParse(text.AsSpan(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var m)
            || h > 14 || m > 59 || (h == 14 && m > 0))
            return false;
        offset = new TimeSpan(h, m, 0);
        if (text[0] == '-') offset = -offset;
        return true;
    }
}
