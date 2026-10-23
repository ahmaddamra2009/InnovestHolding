using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InnovestHolding.Tests;

/// <summary>
/// AddEcosystemEntities on a database in the state before it (with rows and admin edits): it only adds the seven
/// new tables — every existing table keeps every column and value — a second run changes nothing, and Down/Up
/// round-trips.
/// </summary>
[Collection("App")]
public class EcosystemMigrationTests
{
    private const string Before = "20261022202359_AddMediaAndDetailPages";
    private const string Ecosystem = "20261022213309_AddEcosystemEntities";

    private static readonly string[] NewTables =
        ["EventSpeakers", "InvestmentOpportunities", "Speakers", "SponsorshipPackages", "TvEpisodeGuests", "TvEpisodes", "TvGuests"];

    private static async Task<Dictionary<string, string>> DumpAsync(Data.AppDbContext db, Dictionary<string, List<string>> columns)
    {
        var dump = new Dictionary<string, string>();
        foreach (var (table, cols) in columns)
        {
            var order = cols.Contains("Id") ? "[Id]" : "1";
            var sql = "SELECT (SELECT " + string.Join(", ", cols.Select(c => "[" + c + "]")) + " FROM [" + table + "] ORDER BY " + order + " FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";
            dump[table] = await db.Database.SqlQueryRaw<string>(sql).SingleAsync() ?? "[]";
        }
        return dump;
    }

    private static async Task<Dictionary<string, List<string>>> ColumnsAsync(Data.AppDbContext db) =>
        (await MigrationTests.Rows(db, "SELECT TABLE_NAME + '|' + COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME <> '__EFMigrationsHistory' ORDER BY TABLE_NAME, ORDINAL_POSITION"))
            .Select(r => r.Split('|')).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(p => p[1]).ToList());

    [Fact]
    public async Task Upgrade_only_adds_tables_and_round_trips()
    {
        await using var db = await MigrationTests.AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        await db.GetService<IMigrator>().MigrateAsync(Before);
        // Rows an admin may have made since: an edited event slug (with its redirect) and a sector.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Events SET Slug = 'renamed-event' WHERE Id = 1;
            INSERT INTO SlugRedirects (EntityType, OldSlug, EntityId) VALUES ('event', 'event-1', 1);
            UPDATE News SET TitleEn = N'Edited by the admin' WHERE Id = 2;
            """);

        var columns = await ColumnsAsync(db);
        Assert.True(columns.Count >= 20, $"{columns.Count} tables");
        Assert.Empty(columns.Keys.Intersect(NewTables));
        var before = await DumpAsync(db, columns);

        await db.GetService<IMigrator>().MigrateAsync(Ecosystem);
        var afterColumns = await ColumnsAsync(db);
        Assert.Equal(NewTables, afterColumns.Keys.Except(columns.Keys).Order().ToArray());
        foreach (var (table, cols) in columns) Assert.Equal(cols, afterColumns[table]); // no column added, dropped or moved
        var after = await DumpAsync(db, columns);
        foreach (var table in columns.Keys) Assert.True(before[table] == after[table], table);
        foreach (var table in NewTables) // names from the constant list above
        {
            var count = "SELECT COUNT(*) AS [Value] FROM [" + table + "]";
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>(count).SingleAsync());
        }

        // A second run changes nothing.
        await db.GetService<IMigrator>().MigrateAsync(Ecosystem);
        Assert.Equal(after, await DumpAsync(db, columns));

        // Down removes exactly the new tables; the rest is as before; Up again reaches the same state.
        await db.GetService<IMigrator>().MigrateAsync(Before);
        Assert.Equal(columns.Keys.Order(), (await ColumnsAsync(db)).Keys.Order());
        Assert.Equal(before, await DumpAsync(db, columns));
        await db.GetService<IMigrator>().MigrateAsync(Ecosystem);
        Assert.Equal(NewTables, (await ColumnsAsync(db)).Keys.Except(columns.Keys).Order().ToArray());
    }
}
