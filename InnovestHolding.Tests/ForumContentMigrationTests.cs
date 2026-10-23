using System.Text.RegularExpressions;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InnovestHolding.Tests;

/// <summary>
/// F10 AddForumContent (Dev 1): T1 upgrade of a C5 database holding admin rows changes no existing value (but the guarded
/// forum_components swap), a second run is a no-op, Down/Up round-trips; T2 insert-only guards with in-test negative
/// controls; T3 the forum_components swap rule; T4 frozen literals (lengths, old names, registries); T6 the migration
/// before AddForumContent is found by position (it becomes N8's RenameHoldingAndForum after the rebase).
/// Own LocalDB migration database (MigrationTests.Database).
/// </summary>
[Collection("App")]
public class ForumContentMigrationTests
{
    /// <summary>T6: the migration just before AddForumContent, and AddForumContent itself (by position, not by name).</summary>
    internal static (string Before, string Target) Ids(AppDbContext db)
    {
        var all = db.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal).ToList();
        var i = all.FindIndex(m => m.EndsWith("_AddForumContent", StringComparison.Ordinal));
        Assert.True(i > 0, "AddForumContent not found");
        return (all[i - 1], all[i]);
    }

    /// <summary>Rows an admin of the C5 site may have typed that the migration must leave alone (synthetic).</summary>
    private const string Fixture = """
        INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt) VALUES ('forum_tagline', N'شعار كتبه المشرف', NULL, '2026-10-01T10:00:00');
        INSERT INTO ContentItems ([Group], TextAr, TextEn, SortOrder, IsActive, CreatedAt) VALUES ('forum_goals', N'هدف كتبه المشرف', NULL, 1, 1, '2026-10-01T10:00:00');
        INSERT INTO PartnerGroups (NameAr, NameEn, SortOrder, IsActive, CreatedAt) VALUES (N'برعاية', N'Patron (admin)', 5, 1, '2026-10-01T10:00:00');
        INSERT INTO PartnerGroups (NameAr, NameEn, SortOrder, IsActive, CreatedAt) VALUES (N'رعاة ذهبيون', N'Gold', 7, 1, '2026-10-01T10:00:00');
        INSERT INTO Partners (NameAr, NameEn, SortOrder, IsActive, ShowOnHome, CreatedAt, UpdatedAt) VALUES (N'جامعة الدول العربية', N'League (admin)', 4, 1, 1, '2026-10-01T10:00:00', '2026-10-01T10:00:00');
        INSERT INTO Events (Slug, TitleAr, DescriptionAr, EventDay, EventMonthAr, LocationAr, TimeAr, IsActive, SortOrder, CreatedAt, SummaryAr, CityAr, AddressAr, Status, AttendanceMode, RegistrationOpen, MetaTitleAr, MetaDescriptionAr, UpdatedAt)
            VALUES ('admin-finale', N'ختام مسابقة إنوفست', N'', 20, N'نوفمبر', N'', N'', 1, 6, '2026-10-01T10:00:00', N'', N'', N'', 0, 0, 1, N'', N'', '2026-10-01T10:00:00');
        INSERT INTO Events (Slug, TitleAr, DescriptionAr, EventDay, EventMonthAr, LocationAr, TimeAr, IsActive, SortOrder, CreatedAt, SummaryAr, CityAr, AddressAr, Status, AttendanceMode, RegistrationOpen, MetaTitleAr, MetaDescriptionAr, UpdatedAt)
            VALUES ('2026-12-annual-closing-arab-league', N'حفل الختام كما كتبه المشرف', N'', 15, N'ديسمبر', N'', N'', 1, 7, '2026-10-01T10:00:00', N'', N'', N'', 0, 0, 1, N'', N'', '2026-10-01T10:00:00');
        """;

    /// <summary>Every page column except the hub's English meta title (the only page value AddForumContent may rename).</summary>
    private const string OtherPages =
        "SELECT Id, [Key], TitleAr, TitleEn, SummaryAr, SummaryEn, IntroAr, IntroEn, BodyAr, BodyEn, EmptyTextAr, EmptyTextEn, MetaTitleAr, " +
        "CASE WHEN [Key] = 'forum' THEN NULL ELSE MetaTitleEn END AS MetaTitleEn, MetaDescriptionAr, MetaDescriptionEn, ShowInMenu, IsPublished, " +
        "ShowInFooter, SortOrder, UpdatedAt FROM Pages WHERE 1 = 1 ORDER BY Id";

    private const string ItemsByContent = "SELECT [Group], TextAr, TextEn, DescriptionAr, DescriptionEn, IconKey, SortOrder, IsActive FROM ContentItems ORDER BY [Group], SortOrder, TextAr";
    private const string KeysByContent = "SELECT SectionKey, ContentAr, ContentEn FROM SiteContents ORDER BY SectionKey";

    /// <summary>The first difference of two dumps (for the message).</summary>
    private static string Diff(string a, string b)
    {
        var i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
        var from = Math.Max(0, i - 60);
        return "«" + a.Substring(from, Math.Min(160, a.Length - from)) + "» / «" + b.Substring(from, Math.Min(160, b.Length - from)) + "»";
    }

    private static Task<int> CountAsync(AppDbContext db, string sql) => db.Database.SqlQueryRaw<int>(sql).SingleAsync();

    private static Task<string> JsonAsync(AppDbContext db, string sql) =>
        db.Database.SqlQueryRaw<string>("SELECT (" + sql + " FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]").SingleAsync()!;

    private static async Task<AppDbContext> AtBeforeAsync(string? fixture = Fixture)
    {
        var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.GetService<IMigrator>().MigrateAsync(Ids(db).Before);
        if (fixture is not null) await db.Database.ExecuteSqlRawAsync(fixture);
        return db;
    }

    [Fact] // T1 + T2 — upgrade with admin rows: nothing existing changes (but the guarded swap), inserts only, idempotent, Down/Up
    public async Task Upgrade_inserts_only_missing_rows_is_idempotent_and_round_trips()
    {
        await using var db = await AtBeforeAsync();
        var (before, target) = Ids(db);
        var columns = await AdminControlMigrationTests.ColumnsAsync(db);
        var dumpBefore = await AdminControlMigrationTests.DumpAsync(db, columns);
        const string otherItems = "SELECT * FROM ContentItems WHERE [Group] <> 'forum_components' ORDER BY Id";
        const string otherKeys = "SELECT * FROM SiteContents WHERE SectionKey <> 'section_forum_components_title' ORDER BY Id";
        var itemsBefore = await JsonAsync(db, otherItems);
        var itemsByContent = await JsonAsync(db, ItemsByContent);
        var keysByContent = await JsonAsync(db, KeysByContent);
        var keysBefore = await JsonAsync(db, otherKeys);
        var pagesBefore = await JsonAsync(db, OtherPages);
        var maxIds = new Dictionary<string, int>();
        foreach (var t in columns.Keys.Where(t => columns[t].Contains("Id")))
            maxIds[t] = await CountAsync(db, $"SELECT ISNULL(MAX(CAST([Id] AS int)), 0) AS [Value] FROM [{t}]");

        await db.GetService<IMigrator>().MigrateAsync(target);

        // Every pre-existing row keeps every pre-existing column, except ContentItems/SiteContents (checked below).
        var existingAfter = await AdminControlMigrationTests.DumpAsync(db, columns, maxIds);
        var changed = columns.Keys.Where(t => dumpBefore[t] != existingAfter[t]).Order().ToList();
        Assert.Equal(["ContentItems", "Pages", "SiteContents"], changed);
        // Pages: only the hub's English meta title (the naming update of a still-seeded value).
        Assert.Equal(pagesBefore, await JsonAsync(db, OtherPages.Replace("ORDER BY Id", $"AND Id <= {maxIds["Pages"]} ORDER BY Id")));
        Assert.Equal(AddForumContent.HubMetaTitleEn, await db.Pages.Where(p => p.Key == "forum").Select(p => p.MetaTitleEn).SingleAsync());
        Assert.Equal(itemsBefore, await JsonAsync(db, otherItems.Replace("ORDER BY Id", $"AND Id <= {maxIds["ContentItems"]} ORDER BY Id")));
        Assert.Equal(keysBefore, await JsonAsync(db, otherKeys.Replace("ORDER BY Id", $"AND Id <= {maxIds["SiteContents"]} ORDER BY Id")));

        // The admin's rows win: tagline, a started list, a group, a partner and two events of the same identity.
        var keys = await db.SiteContents.AsNoTracking().ToDictionaryAsync(k => k.SectionKey);
        Assert.Equal("شعار كتبه المشرف", keys["forum_tagline"].ContentAr);
        foreach (var k in AddForumContent.NewKeys.Where(k => k[0] != "forum_tagline"))
            Assert.Equal((k[1], k[2]), (keys[k[0]].ContentAr, keys[k[0]].ContentEn));
        Assert.Equal(["هدف كتبه المشرف"], await db.ContentItems.Where(i => i.Group == "forum_goals").Select(i => i.TextAr).ToListAsync());
        foreach (var (group, rows) in AddForumContent.ItemGroups.Where(g => g.Group != "forum_goals"))
            Assert.Equal(rows.Select(r => r.TextAr), await db.ContentItems.Where(i => i.Group == group).OrderBy(i => i.SortOrder).Select(i => i.TextAr).ToListAsync());
        Assert.Equal(("Patron (admin)", false), await db.PartnerGroups.Where(g => g.NameAr == "برعاية").Select(g => ValueTuple.Create(g.NameEn, g.ShowOnForumHub)).SingleAsync());
        Assert.Equal(6, await db.PartnerGroups.CountAsync());
        Assert.True(await db.PartnerGroups.Where(g => g.NameAr == "بتنظيم").Select(g => g.ShowOnForumHub).SingleAsync());
        // New groups sort before the admin's (min existing 5 → -4…0).
        Assert.True(await db.PartnerGroups.Where(g => g.NameAr == "شريك استراتيجي").Select(g => g.SortOrder).SingleAsync() < 5);
        Assert.Equal("League (admin)", await db.Partners.Where(p => p.NameAr == "جامعة الدول العربية").Select(p => p.NameEn).SingleAsync());
        var partners = await db.Partners.AsNoTracking().Include(p => p.PartnerGroup).Where(p => p.NameAr != "جامعة الدول العربية").ToListAsync();
        Assert.Equal(7, partners.Count);
        Assert.All(partners, p => Assert.False(p.ShowOnHome));
        Assert.Equal(4, partners.Count(p => !p.IsActive && p.PartnerGroup!.NameAr == "بمشاركة"));
        Assert.Equal("Qadroon Youth Foundation for Sustainable Development", partners.Single(p => p.PartnerGroup!.NameAr == "بتنظيم").NameEn);
        Assert.Equal(1, await db.Events.CountAsync(e => e.TitleAr == "ختام مسابقة إنوفست"));
        Assert.Equal("حفل الختام كما كتبه المشرف", await db.Events.Where(e => e.Slug == "2026-12-annual-closing-arab-league").Select(e => e.TitleAr).SingleAsync());
        var events = await db.Events.AsNoTracking().Where(e => e.DateIsMonth).OrderBy(e => e.SortOrder).ToListAsync();
        Assert.Equal(7, events.Count);
        Assert.All(events, e => Assert.True(e.IsActive && e.RegistrationOpen && e.StartsAt!.Value.Day == 1 && e.EndsAt!.Value.Hour == 23 && e.SortOrder > 7));
        Assert.Equal(0, await db.Events.CountAsync(e => !e.DateIsMonth && e.Slug.StartsWith("2026-0")));
        Assert.Equal(6, await db.ForumEditions.CountAsync(e => e.IsActive));
        Assert.Equal(0, await db.ForumEditionPhotos.CountAsync());
        Assert.Equal(AddForumContent.NewComponents.Select(c => c.TextAr),
            await db.ContentItems.Where(i => i.Group == "forum_components").OrderBy(i => i.SortOrder).Select(i => i.TextAr).ToListAsync());
        Assert.Equal(AddForumContent.NewComponentsTitle.Ar, keys["section_forum_components_title"].ContentAr);
        var page = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.ForumEditions);
        Assert.True(page is { ShowInMenu: true, ShowInFooter: true, IsPublished: true, SortOrder: 5 });
        Assert.Equal(AddForumContent.SectionRows.Length, await db.PageSections.CountAsync(s =>
            AddForumContent.SectionRows.Select(r => r.Page + "|" + r.Section).Contains(s.PageKey + "|" + s.SectionKey)));

        // A second run of every data statement changes nothing.
        var allColumns = await AdminControlMigrationTests.ColumnsAsync(db);
        var after = await AdminControlMigrationTests.DumpAsync(db, allColumns);
        foreach (var sql in AddForumContent.UpSql()) await db.Database.ExecuteSqlRawAsync(sql.Replace("{", "{{").Replace("}", "}}"));
        var rerun = await AdminControlMigrationTests.DumpAsync(db, allColumns);
        var rerunChanged = allColumns.Keys.Where(t => after[t] != rerun[t]).ToList();
        Assert.True(rerunChanged.Count == 0, "second run changed: " + string.Join(", ", rerunChanged));
        var afterWithoutIds = await AdminControlMigrationTests.DumpAsync(db, allColumns, withoutIdsAndTimes: true);

        // Down: the C5 schema and every C5 value (the admin rows included) come back.
        await db.GetService<IMigrator>().MigrateAsync(before);
        Assert.Equal(columns.Keys.Order(), (await AdminControlMigrationTests.ColumnsAsync(db)).Keys.Order());
        var down = await AdminControlMigrationTests.DumpAsync(db, columns);
        // The restored forum_components rows are re-inserted (new ids), the restored heading keeps Up's UpdatedAt: those two
        // tables are compared by content.
        var downChanged = columns.Keys.Where(t => t is not ("ContentItems" or "SiteContents") && dumpBefore[t] != down[t]).Select(t => t + ": " + Diff(dumpBefore[t], down[t])).ToList();
        Assert.True(downChanged.Count == 0, "Down left: " + string.Join(" ‖ ", downChanged));
        Assert.Equal(itemsByContent, await JsonAsync(db, ItemsByContent));
        Assert.Equal(keysByContent, await JsonAsync(db, KeysByContent));

        // Up again reaches the same state (ids and timestamps aside).
        await db.GetService<IMigrator>().MigrateAsync(target);
        var again = await AdminControlMigrationTests.DumpAsync(db, allColumns, withoutIdsAndTimes: true);
        // Re-inserted groups have new ids: the partners' group FK is compared without its id value.
        static string NoGroupId(string json) => Regex.Replace(json, "\"PartnerGroupId\":\\d+", "\"PartnerGroupId\":#");
        var againChanged = allColumns.Keys.Where(t => NoGroupId(afterWithoutIds[t]) != NoGroupId(again[t])).Select(t => t + ": " + Diff(afterWithoutIds[t], again[t])).ToList();
        Assert.True(againChanged.Count == 0, "Up again differs: " + string.Join(" ‖ ", againChanged));
    }

    [Fact] // T2 — negative controls: without its NOT EXISTS guard a step duplicates rows or breaks (so the guards are what holds)
    public async Task Guards_are_what_prevents_duplicates()
    {
        await using var db = await AtBeforeAsync(fixture: null);
        await db.GetService<IMigrator>().MigrateAsync(Ids(db).Target);
        var sql = AddForumContent.UpSql();

        var partners = sql.Single(s => s.StartsWith("INSERT INTO Partners", StringComparison.Ordinal));
        var unguarded = Regex.Replace(partners, @"WHERE NOT EXISTS \(SELECT 1 FROM Partners p WHERE .*?\);\s*$", ";", RegexOptions.Singleline);
        Assert.NotEqual(partners, unguarded);
        await db.Database.ExecuteSqlRawAsync(partners.Replace("{", "{{").Replace("}", "}}"));
        Assert.Equal(8, await db.Partners.CountAsync());
        await db.Database.ExecuteSqlRawAsync(unguarded.Replace("{", "{{").Replace("}", "}}"));
        Assert.Equal(16, await db.Partners.CountAsync()); // red without the guard

        var keys = sql[0];
        var unguardedKeys = keys[..keys.IndexOf("WHERE NOT EXISTS", StringComparison.Ordinal)] + ";";
        await Assert.ThrowsAnyAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(unguardedKeys.Replace("{", "{{").Replace("}", "}}")));

        var items = sql.Single(s => s.Contains("'forum_sectors'", StringComparison.Ordinal) && s.StartsWith("IF NOT EXISTS", StringComparison.Ordinal));
        await db.Database.ExecuteSqlRawAsync(items[(items.IndexOf('\n') + 1)..]); // the INSERT without its IF
        Assert.Equal(12, await db.ContentItems.CountAsync(i => i.Group == "forum_sectors"));
    }

    [Theory] // T3 — forum_components: swapped only while the 8 rows are untouched; the heading only while it is the frozen one
    [InlineData("none", true, true)]
    [InlineData("text", false, false)]
    [InlineData("hidden", false, false)]
    [InlineData("extra", false, false)]
    [InlineData("title", true, false)]
    public async Task Components_swap_only_untouched_rows(string edit, bool swapped, bool titleChanged)
    {
        await using var db = await AtBeforeAsync(fixture: null);
        var (before, target) = Ids(db);
        await db.Database.ExecuteSqlRawAsync(edit switch
        {
            "text" => "UPDATE ContentItems SET TextEn = N'Pitch Night' WHERE [Group] = 'forum_components' AND TextEn = N'Pitch Competition';",
            "hidden" => "UPDATE ContentItems SET IsActive = 0 WHERE [Group] = 'forum_components' AND TextEn = N'AI Summit';",
            "extra" => "INSERT INTO ContentItems ([Group], TextAr, SortOrder, IsActive, CreatedAt) VALUES ('forum_components', N'بند أضافه المشرف', 9, 1, SYSUTCDATETIME());",
            "title" => "UPDATE SiteContents SET ContentAr = N'عنوان المشرف' WHERE SectionKey = 'section_forum_components_title';",
            _ => "SELECT 1;",
        });
        var rowsBefore = await JsonAsync(db, "SELECT [Group], TextAr, TextEn, IconKey, DescriptionAr, DescriptionEn, SortOrder, IsActive FROM ContentItems WHERE [Group] = 'forum_components' ORDER BY Id");
        var titleBefore = await JsonAsync(db, "SELECT ContentAr, ContentEn FROM SiteContents WHERE SectionKey = 'section_forum_components_title'");

        await db.GetService<IMigrator>().MigrateAsync(target);
        var texts = await db.ContentItems.Where(i => i.Group == "forum_components").OrderBy(i => i.SortOrder).Select(i => i.TextEn).ToListAsync();
        var title = await JsonAsync(db, "SELECT ContentAr, ContentEn FROM SiteContents WHERE SectionKey = 'section_forum_components_title'");
        if (swapped)
        {
            Assert.Equal(AddForumContent.NewComponents.Select(c => c.TextEn), texts);
            Assert.All(await db.ContentItems.Where(i => i.Group == "forum_components").ToListAsync(), i => Assert.False(string.IsNullOrEmpty(i.DescriptionAr)));
        }
        else
        {
            Assert.Equal(rowsBefore, await JsonAsync(db, "SELECT [Group], TextAr, TextEn, IconKey, DescriptionAr, DescriptionEn, SortOrder, IsActive FROM ContentItems WHERE [Group] = 'forum_components' ORDER BY Id"));
        }
        if (titleChanged) Assert.Contains(AddForumContent.NewComponentsTitle.En, title);
        else Assert.Equal(titleBefore, title);

        // Down restores the 8 rows (and the heading) only from an untouched swap.
        await db.GetService<IMigrator>().MigrateAsync(before);
        Assert.Equal(rowsBefore, await JsonAsync(db, "SELECT [Group], TextAr, TextEn, IconKey, DescriptionAr, DescriptionEn, SortOrder, IsActive FROM ContentItems WHERE [Group] = 'forum_components' ORDER BY Id"));
        Assert.Equal(titleBefore, await JsonAsync(db, "SELECT ContentAr, ContentEn FROM SiteContents WHERE SectionKey = 'section_forum_components_title'"));
    }

    [Fact] // T3 — an admin edit of the 7 new rows survives Down
    public async Task Down_keeps_an_edited_components_list()
    {
        await using var db = await AtBeforeAsync(fixture: null);
        var (before, target) = Ids(db);
        await db.GetService<IMigrator>().MigrateAsync(target);
        await db.Database.ExecuteSqlRawAsync("UPDATE ContentItems SET TextEn = N'Talks' WHERE [Group] = 'forum_components' AND TextEn = N'INNOVEST Talks';");
        await db.GetService<IMigrator>().MigrateAsync(before);
        Assert.Equal(7, await db.ContentItems.CountAsync(i => i.Group == "forum_components"));
    }

    // ── T4: frozen literals ──

    private static readonly string[] OldNames =
        // Anas 2026-10-23: the forum is «منتدى الإبتكار والإستثمار العربي» / "Innovest Arab Forum" — every other form is old.
        ["هولدينغ", "منتدى إنوفست", "منتدى الابتكار والاستثمار العربي", "المنتدى العربي", "Innovest Forum", "INNOVEST FORUM", "[?]", "ملتقي ", "الإصنطاعي", "يوينو", "الأقليمية", "الإجتماعى", "الأولي ", "يُعكس",
         // COPY-F10 B1–B7: the PDF's hamza on these masdars — matched as STEMS, so prefixed forms («للإستثمار»,
         // «بالإبتكار», «والإقتصاد») are caught too (Hassan F10 🔵1).
         "إستثمار", "إبتكار", "إنطلاق", "إقتصاد", "إصطناع"];

    /// <summary>Every literal Up inserts or writes (not the frozen old components, which are only a guard and Down's restore).</summary>
    private static IEnumerable<string> InsertedLiterals() =>
        AddForumContent.NewKeys.SelectMany(k => new[] { k[1], k[2] })
            .Concat(AddForumContent.EditionsPageRow.Select(c => c.Value))
            .Concat(AddForumContent.ItemGroups.SelectMany(g => g.Rows).Concat(AddForumContent.NewComponents)
                .SelectMany(r => new[] { r.TextAr, r.TextEn, r.DescriptionAr, r.DescriptionEn }))
            .Concat([AddForumContent.NewComponentsTitle.Ar, AddForumContent.NewComponentsTitle.En])
            .Concat(AddForumContent.PartnerGroups.SelectMany(g => new[] { g.NameAr, g.NameEn }))
            .Concat(AddForumContent.Partners.SelectMany(p => new[] { p.NameAr, p.NameEn }))
            .Concat(AddForumContent.Events.SelectMany(e => new[] { e.TitleAr, e.TitleEn, e.PlaceAr, e.PlaceEn, e.MonthAr, e.MonthEn }))
            .Concat(AddForumContent.Editions.SelectMany(e => new[] { e.LabelAr, e.LabelEn, e.TitleAr, e.TitleEn, e.PlaceAr, e.PlaceEn }))
            .Concat(AddForumContent.NamingUpdates.Select(u => u.New))
            .Where(s => s is not null);

    /// <summary>The official name keeps the hamza spelling of the logo; it is the one exception to the stem check.</summary>
    private static string WithoutOfficialName(string s) => s.Replace(RenameHoldingAndForum.ForumNameAr, "«name»", StringComparison.Ordinal);

    [Fact] // T4 — no literal carries an old name, a "[?]" or a corrected typo
    public void No_literal_holds_an_old_name_or_a_printed_typo()
    {
        var all = InsertedLiterals().ToList();
        Assert.True(all.Count > 250, $"{all.Count} literals searched");
        var hits = all.SelectMany(s => OldNames.Where(o => WithoutOfficialName(s).Contains(o, StringComparison.Ordinal)).Select(o => $"{o} in «{s[..Math.Min(40, s.Length)]}»")).ToList();
        Assert.True(hits.Count == 0, string.Join(" ‖ ", hits));
        // The search catches prefixed forms (the negative control of 🔵1: «للإستثمار» was missed by «الإستثمار»).
        Assert.All(new[] { "للإستثمار", "بالإبتكار", "والإقتصاد", "قبل إنطلاق" }, w => Assert.Contains(OldNames, o => w.Contains(o, StringComparison.Ordinal)));
        // ... and the exemption is the exact official name only: the stems around it are still caught.
        Assert.Contains(OldNames, o => WithoutOfficialName(RenameHoldingAndForum.ForumNameAr + " والإستثمار").Contains(o, StringComparison.Ordinal));
        Assert.Contains(all, s => s.Contains(RenameHoldingAndForum.ForumNameAr, StringComparison.Ordinal)); // the official name is used
        Assert.Contains(all, s => s.Contains("Innovest Arab Forum", StringComparison.Ordinal));
        // N8 (RenameHoldingAndForum, frozen, runs BEFORE this migration) never sees F10's rows: they must arrive renamed.
        var n8 = all.SelectMany(s => RenameHoldingAndForum.Pairs.Where(p => s.Contains(p.Find, StringComparison.Ordinal))
            .Select(p => $"{p.Find} in «{s[..Math.Min(40, s.Length)]}»")).ToList();
        Assert.True(n8.Count == 0, "N8 old phrases: " + string.Join(" ‖ ", n8));
        Assert.Contains(RenameHoldingAndForum.Pairs, p => p.Find == "هولدينغ"); // the pairs are the real list
        // CTO D1: Western digits in all Arabic site text (Cultures.cs renders numbers so), edition titles and address included.
        static bool Indic(char c) => c is >= '٠' and <= '٩' or >= '۰' and <= '۹';
        var indic = all.Where(s => s.Any(Indic)).ToList();
        Assert.True(indic.Count == 0, "Arabic-Indic digits in: " + string.Join(" ‖ ", indic));
        Assert.All(ForumMediaImport.Editions, e => Assert.False(e.TitleAr.Any(Indic), e.TitleAr));
        // The negative control of the search itself: the frozen old components DO contain an old name.
        Assert.Contains(AddForumContent.OldComponents, c => c.TextAr.Contains("منتدى الابتكار والاستثمار العربي", StringComparison.Ordinal));
    }

    [Fact] // T4 — every literal fits its column (EF model) and, for content keys, the registry's MaxLength and rule
    public void Every_literal_fits_its_column_and_rule()
    {
        using var db = MigrationTests.NewContext();
        int Max<T>(string property) => db.Model.FindEntityType(typeof(T))!.FindProperty(property)!.GetMaxLength() ?? int.MaxValue;
        void Fits<T>(string property, string? value) =>
            Assert.True((value?.Length ?? 0) <= Max<T>(property), $"{typeof(T).Name}.{property}: {value?.Length} > {Max<T>(property)} «{value}»");

        foreach (var k in AddForumContent.NewKeys)
        {
            var def = ContentKeys.Find(k[0]);
            Assert.True(def is not null, k[0] + " is not in the registry");
            Fits<SiteContent>("SectionKey", k[0]);
            Assert.True(k[1].Length <= def!.MaxLength && (k[2]?.Length ?? 0) <= def.MaxLength, k[0] + " is longer than its registry MaxLength");
            Assert.True(!def.Neutral || k[2] is null, k[0] + " is neutral but has English");
            Assert.True(def.AllowEmpty || k[1].Length > 0 || k[0].EndsWith("_intro", StringComparison.Ordinal), k[0] + " is empty");
            switch (def.Rule)
            {
                case ContentRule.Phone: Assert.True(ContactFormats.IsPhone(k[1]), k[0]); break;
                case ContentRule.HttpsUrl: Assert.True(Infrastructure.Seo.SiteUrl.IsHttpsUrl(k[1]) && Infrastructure.SafeHrefAttribute.IsSafe(k[1]), k[0]); break;
                case ContentRule.Count: Assert.Equal(int.Parse(k[1]), ContentKeys.ParseCount(k[1], -1, def.CountMax)); break;
                case ContentRule.None: break;
                default: Assert.Fail(k[0] + " has an unchecked rule " + def.Rule); break;
            }
        }
        foreach (var (column, value) in AddForumContent.EditionsPageRow) Fits<Page>(column, value);
        foreach (var r in AddForumContent.ItemGroups.SelectMany(g => g.Rows).Concat(AddForumContent.NewComponents))
        {
            Fits<ContentItem>("TextAr", r.TextAr); Fits<ContentItem>("TextEn", r.TextEn);
            Fits<ContentItem>("DescriptionAr", r.DescriptionAr); Fits<ContentItem>("DescriptionEn", r.DescriptionEn);
            Assert.True(r.Icon is null || Icons.IsKnown(r.Icon), r.Icon);
        }
        foreach (var g in AddForumContent.PartnerGroups) { Fits<PartnerGroup>("NameAr", g.NameAr); Fits<PartnerGroup>("NameEn", g.NameEn); }
        foreach (var p in AddForumContent.Partners)
        {
            Fits<Partner>("NameAr", p.NameAr); Fits<Partner>("NameEn", p.NameEn);
            Assert.Contains(AddForumContent.PartnerGroups, g => g.NameAr == p.GroupAr);
        }
        foreach (var e in AddForumContent.Events)
        {
            Fits<Event>("Slug", e.Slug); Fits<Event>("TitleAr", e.TitleAr); Fits<Event>("TitleEn", e.TitleEn);
            Fits<Event>("LocationAr", e.PlaceAr); Fits<Event>("LocationEn", e.PlaceEn);
            Fits<Event>("EventMonthAr", e.MonthAr); Fits<Event>("EventMonthEn", e.MonthEn);
            Assert.True(Slugs.IsValid(e.Slug), e.Slug);
            var first = DateTimeOffset.Parse(AddForumContent.MonthRange(e.Month).Starts, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal((EventDates.Month(first, false), EventDates.Month(first, true)), (e.MonthAr, e.MonthEn));
        }
        Assert.Equal(AddForumContent.Events.Select(e => e.Month).Order(), AddForumContent.Events.Select(e => e.Month)); // chronological
        foreach (var e in AddForumContent.Editions)
        {
            Fits<ForumEdition>("EditionLabelAr", e.LabelAr); Fits<ForumEdition>("EditionLabelEn", e.LabelEn);
            Fits<ForumEdition>("TitleAr", e.TitleAr); Fits<ForumEdition>("TitleEn", e.TitleEn);
            Fits<ForumEdition>("PlaceAr", e.PlaceAr); Fits<ForumEdition>("PlaceEn", e.PlaceEn);
            Assert.InRange(e.Year, ForumEdition.MinYear, ForumEdition.MaxYear);
        }
    }

    [Fact] // T4 — the registries and the migration's frozen lists are the same (keys and list groups)
    public void Registries_equal_the_frozen_lists()
    {
        string[] existing = ["section_forum_components_title", "section_forum_components_intro"]; // AddSiteStructure/ApplyCompanyProfile
        var registry = ContentKeys.All.Where(k => k.Group == ContentKeys.ForumGroup || k.Key == ContentKeys.ForumEditionsLimit)
            .Select(k => k.Key).Except(existing).Order(StringComparer.Ordinal);
        Assert.Equal(registry, AddForumContent.NewKeys.Select(k => k[0]).Order(StringComparer.Ordinal));
        Assert.Equal(ContentItemGroups.All.Where(g => g.Key.StartsWith("forum_", StringComparison.Ordinal) && g.Key != ContentItemGroups.ForumComponents)
            .Select(g => g.Key).Order(StringComparer.Ordinal), AddForumContent.ItemGroups.Select(g => g.Group).Order(StringComparer.Ordinal));
        Assert.Equal(AddForumContent.Editions.Length, AddForumContent.Editions.Select(e => (e.Year, e.TitleAr)).Distinct().Count());
        Assert.Equal(AddForumContent.EditionsPageKey, PageKeys.ForumEditions);
        Assert.Equal(AddForumContent.MediaMarker, ForumMediaImport.Marker);
    }
}
