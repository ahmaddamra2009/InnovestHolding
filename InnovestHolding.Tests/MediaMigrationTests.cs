using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InnovestHolding.Tests;

/// <summary>
/// AddMediaAndDetailPages on a database in the state before it (the multi-page release, with real rows and admin
/// edits): every existing value survives, existing news/events get news-{id}/event-{id}, the new flags keep today's
/// behaviour, and Down/Up round-trips.
/// </summary>
[Collection("App")]
public class MediaMigrationTests
{
    private const string Before = "20261022200000_CorrectCoreValuesEnglish";
    private const string Media = "20261022202359_AddMediaAndDetailPages";

    private static async Task<Data.AppDbContext> AtRefineStateAsync()
    {
        var db = await MigrationTests.AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        await db.GetService<IMigrator>().MigrateAsync(Before);
        // Rows an admin may have added since: an Arabic-only event, an inactive news item, an edited partner.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Events (TitleAr, DescriptionAr, EventDay, EventMonthAr, LocationAr, TimeAr, IsActive, SortOrder, CreatedAt)
                VALUES (N'ملتقى أضافه المدير', N'وصف', 5, N'ديسمبر', N'القاهرة', N'10 ص', 1, 9, '2026-09-01T10:00:00');
            INSERT INTO News (TitleAr, SummaryAr, CategoryAr, NewsDateAr, LinkUrl, SortOrder, IsActive, CreatedAt)
                VALUES (N'خبر معطّل', N'ملخص', N'عام', N'1 يونيو', N'https://example.org/a', 0, 0, '2026-09-01T10:00:00');
            UPDATE Partners SET NameEn = N'Edited partner' WHERE Id = 2;
            UPDATE Partners SET IsActive = 0 WHERE Id = 3;
            """);
        return db;
    }

    /// <summary>Every row of every table, the columns of the state before the migration only, as JSON.</summary>
    private static async Task<Dictionary<string, string>> DumpAsync(Data.AppDbContext db, Dictionary<string, List<string>> columns)
    {
        var dump = new Dictionary<string, string>();
        foreach (var (table, cols) in columns)
        {
            var sql = "SELECT (SELECT " + string.Join(", ", cols.Select(c => "[" + c + "]")) + " FROM [" + table + "] ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";
            dump[table] = await db.Database.SqlQueryRaw<string>(sql).SingleAsync() ?? "[]";
        }
        return dump;
    }

    private static async Task<Dictionary<string, List<string>>> ColumnsAsync(Data.AppDbContext db) =>
        (await MigrationTests.Rows(db, "SELECT TABLE_NAME + '|' + COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME <> '__EFMigrationsHistory' ORDER BY TABLE_NAME, ORDINAL_POSITION"))
            .Select(r => r.Split('|')).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(p => p[1]).ToList());

    [Fact]
    public async Task Upgrade_keeps_every_value_and_backfills_slugs()
    {
        await using var db = await AtRefineStateAsync();
        var columns = await ColumnsAsync(db);
        Assert.True(columns.Count >= 17, $"{columns.Count} tables");
        var before = await DumpAsync(db, columns);

        await db.GetService<IMigrator>().MigrateAsync(Media);
        var after = await DumpAsync(db, columns);

        // Every table byte-identical in its existing columns, except SiteContents, which gains exactly one row.
        foreach (var table in columns.Keys.Where(t => t != "SiteContents"))
            Assert.True(before[table] == after[table], table);
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM SiteContents WHERE SectionKey = N'default_utc_offset' AND ContentAr = N'+02:00'").SingleAsync());
        Assert.StartsWith(before["SiteContents"].TrimEnd(']'), after["SiteContents"]);

        Assert.Equal(["event-1", "event-2", "event-3", "event-4"], await db.Events.OrderBy(e => e.Id).Select(e => e.Slug).ToListAsync());
        Assert.Equal(["news-1", "news-2", "news-3", "news-4"], await db.News.OrderBy(e => e.Id).Select(e => e.Slug).ToListAsync());
        Assert.All(await db.Events.Select(e => e.RegistrationOpen).ToListAsync(), Assert.True);
        Assert.All(await db.Partners.Select(p => p.ShowOnHome).ToListAsync(), Assert.True);
        Assert.Equal(0, await db.MediaFiles.CountAsync());
        Assert.Equal(0, await db.SlugRedirects.CountAsync());

        // A second run changes nothing (to this migration: later migrations have data steps of their own).
        await db.GetService<IMigrator>().MigrateAsync(Media);
        Assert.Equal(after, await DumpAsync(db, columns));

        // Down restores the previous schema and values; Up again reaches the same state.
        await db.GetService<IMigrator>().MigrateAsync(Before);
        Assert.Equal(columns, await ColumnsAsync(db));
        Assert.Equal(before, await DumpAsync(db, columns));
        await db.GetService<IMigrator>().MigrateAsync(Media);
        var again = await DumpAsync(db, columns);
        foreach (var table in columns.Keys.Where(t => t != "SiteContents"))
            Assert.True(after[table] == again[table], table);
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM SiteContents WHERE SectionKey = N'default_utc_offset'").SingleAsync());
    }
}
