using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// One-time conversion of Registrations.CreatedAt and ContactMessages.CreatedAt from the web server's local time
/// (what earlier versions of the app wrote with DateTime.Now) to UTC (what it writes now).
///
/// It runs at startup, inside the app, on the server that wrote the rows — NOT in the EF migration, which may be
/// scripted and run from another machine whose time zone is not the one that wrote the values.
/// Every row is converted with <c>TimeZoneInfo.ConvertTimeToUtc(value, zone)</c>, so each row gets the offset that was
/// in force on its own date (DST included). All rows and the marker key are written in ONE transaction; the marker
/// <see cref="ContentKeys.TimestampsUtcMarker"/> makes every later start a no-op.
/// </summary>
public static class TimestampUtcConversion
{
    public static async Task<int> RunAsync(AppDbContext db, TimeZoneInfo zone, ILogger logger, CancellationToken ct = default)
    {
        // Guard polarity: the marker's presence makes this do LESS (nothing). A missing marker converts — which is
        // only correct because no current code path writes a local time.
        if (await db.SiteContents.AnyAsync(c => c.SectionKey == ContentKeys.TimestampsUtcMarker, ct))
            return 0;

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var converted = 0;

            foreach (var r in await db.Registrations.ToListAsync(ct))
            {
                r.CreatedAt = ToUtc(r.CreatedAt, zone);
                converted++;
            }
            foreach (var m in await db.ContactMessages.ToListAsync(ct))
            {
                m.CreatedAt = ToUtc(m.CreatedAt, zone);
                converted++;
            }

            db.SiteContents.Add(new SiteContent
            {
                SectionKey = ContentKeys.TimestampsUtcMarker,
                ContentAr = $"converted {converted} rows from {zone.Id}",
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            db.ChangeTracker.Clear();

            logger.LogInformation("Timestamps converted to UTC: {Count} row(s), from time zone {Zone}.", converted, zone.Id);
            return converted;
        });
    }

    /// <summary>
    /// Local wall-clock time in <paramref name="zone"/> → UTC. A time inside a spring-forward gap does not exist and
    /// ConvertTimeToUtc refuses it: such a value is shifted by the zone's base offset instead (an hour's error at most,
    /// on a value that could only have come from a clock change).
    /// </summary>
    public static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(unspecified))
            return DateTime.SpecifyKind(unspecified - zone.BaseUtcOffset, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }
}
