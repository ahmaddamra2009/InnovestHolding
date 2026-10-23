using InnovestHolding.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace InnovestHolding.Tests;

/// <summary>
/// AddSiteStructure + ApplyCompanyProfile on a database in the shape of the bilingual release (what production has now): built from the
/// frozen pre-bilingual seed migrated to AddBilingualContent, never from current code. Same LocalDB database as
/// <see cref="MigrationTests"/>, recreated per test.
/// </summary>
[Collection("App")]
public class CompanyProfileMigrationTests
{
    private const string Profile = "20261022153329_ApplyCompanyProfile";

    private static async Task<AppDbContextScope> AtPrBStateAsync()
    {
        var db = await MigrationTests.AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        await db.GetService<IMigrator>().MigrateAsync(MigrationTests.Bilingual);
        return new AppDbContextScope(db);
    }

    private sealed class AppDbContextScope(Data.AppDbContext db) : IAsyncDisposable
    {
        public Data.AppDbContext Db { get; } = db;
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static Task<List<string>> Rows(Data.AppDbContext db, string sql) => MigrationTests.Rows(db, sql);

    private static Task<string> One(Data.AppDbContext db, string sql) => db.Database.SqlQueryRaw<string>(sql).SingleAsync();

    /// <summary>The real-data tables as JSON (every column that existed before the multi-page site), compared byte for byte.</summary>
    private static async Task<List<string>> RealDataAsync(Data.AppDbContext db) =>
    [
        // Frozen column lists: the columns of the multi-page release (later migrations add columns of their own).
        await One(db, "SELECT (SELECT Id, TitleAr, TitleEn, DescriptionAr, DescriptionEn, EventDay, EventMonthAr, EventMonthEn, LocationAr, LocationEn, TimeAr, TimeEn, IsActive, SortOrder, CreatedAt FROM Events ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
        await One(db, "SELECT (SELECT Id, TitleAr, TitleEn, SummaryAr, SummaryEn, CategoryAr, CategoryEn, NewsDateAr, NewsDateEn, LinkUrl, SortOrder, IsActive, CreatedAt FROM News ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
        await One(db, "SELECT (SELECT Id, NameAr, NameEn, SortOrder FROM Partners ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
        await One(db, "SELECT (SELECT * FROM Faqs ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
        // Frozen column list: the columns before the lead forms (AddLeadForms adds its own).
        await One(db, "SELECT (SELECT Id, FullName, Email, Phone, Interest, InterestOptionId, Culture, IsRead, CreatedAt FROM Registrations ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
        await One(db, "SELECT (SELECT * FROM ContactMessages ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]"),
    ];

    private static Task<string?> Content(Data.AppDbContext db, string key, bool english = false) =>
        db.SiteContents.Where(c => c.SectionKey == key).Select(c => english ? c.ContentEn : c.ContentAr).SingleOrDefaultAsync();

    [Fact] // the profile replaces seed text only; admin edits and real data are untouched; a second run changes nothing
    public async Task Profile_replaces_only_unchanged_seed_values_and_keeps_real_data_and_admin_edits()
    {
        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        // What an admin may have done on production since the bilingual release.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE SiteContents SET ContentAr = N'رؤية عدّلها المدير' WHERE SectionKey = N'vision_text';
            UPDATE SiteContents SET ContentEn = N'Mission edited by the admin' WHERE SectionKey = N'mission_text';
            UPDATE SiteContents SET ContentAr = N'admin@example.com' WHERE SectionKey = N'contact_email';
            UPDATE Platforms SET DescriptionAr = N'وصف عدّله المدير' WHERE TitleAr = N'المسابقات';
            UPDATE Stats SET [Value] = 999 WHERE SortOrder = 3;
            INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter) VALUES (N'رابط المدير', NULL, N'/about', 20, 0, 1, 0);
            INSERT INTO Registrations (FullName, Email, Phone, Interest, IsRead, CreatedAt, Culture) VALUES (N'QA', N'a@example.com', N'', N'x', 0, '2026-07-01T12:00:00', N'ar');
            INSERT INTO ContactMessages (FullName, Email, Subject, Message, IsRead, CreatedAt, Culture) VALUES (N'QA', N'a@example.com', N's', N'm', 1, '2026-01-15T12:00:00', N'en');
            """);
        var realBefore = await RealDataAsync(db);

        // Up to the last migration before the N8 rename, which rewrites the brand names in real news/FAQ text on
        // purpose (covered by RenameHoldingAndForumTests); AddForumContent (F10) then adds forum events and partners
        // (covered by ForumContentMigrationTests).
        await db.GetService<IMigrator>().MigrateAsync("20261023011038_AddAdminControl");
        Assert.Equal(["20261023141145_RenameHoldingAndForum", "20261023144143_AddForumContent"], await db.Database.GetPendingMigrationsAsync());

        // Real data: byte-identical (events, news, partners are real); partners stay visible.
        Assert.Equal(realBefore, await RealDataAsync(db));
        Assert.Equal(10, await db.Partners.CountAsync(p => p.IsActive));

        // SiteContents: seed text replaced, admin edits kept.
        Assert.Equal("نربط رواد الأعمال بالمستثمرين في أنحاء الوطن العربي", await Content(db, "hero_title"));
        Assert.Equal("Connecting Entrepreneurs with Investors Across the Arab World", await Content(db, "hero_title", true));
        Assert.Equal("رؤية عدّلها المدير", await Content(db, "vision_text"));                       // Arabic edited → untouched
        Assert.Equal("To be the leading platform for investment and innovation in the region", await Content(db, "vision_text", true));
        Assert.StartsWith("بناء بيئة عربية مستدامة", await Content(db, "mission_text"));               // Arabic seed → profile
        Assert.Equal("Mission edited by the admin", await Content(db, "mission_text", true));         // its edited English kept
        Assert.Equal("admin@example.com", await Content(db, "contact_email"));                        // neutral, edited → kept
        Assert.Equal("+20 01141410808", await Content(db, "contact_phone"));                          // neutral, seed → profile
        Assert.Equal("network.join", await Content(db, "header_cta_page"));                           // new key
        var keys = await db.SiteContents.Select(c => c.SectionKey).ToListAsync();
        Assert.Empty(Infrastructure.Content.ContentKeys.All.Select(k => k.Key)                    // every registry key exists
            .Except(AddForumContent.NewKeys.Select(k => k[0])).Except(keys));                    // (but F10's, not yet applied)
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Equal("", await Content(db, "founder_message"));                                       // founder block hidden

        // Platforms → services 01–06, except the one the admin edited.
        var platforms = await Rows(db, "SELECT CONCAT(TitleAr, N'|', DescriptionAr) AS [Value] FROM Platforms ORDER BY Id");
        Assert.Equal(6, platforms.Count);
        Assert.StartsWith("تطوير الشركات الناشئة|", platforms[0]);
        Assert.Equal("المسابقات|وصف عدّله المدير", platforms[3]);
        Assert.Equal(6, await db.Platforms.CountAsync(p => p.IsActive));

        // Stats: rows still equal to the seed become profile figures; the edited one is kept, so the table does not
        // hold exactly the four profile rows and the two extra figures are NOT added.
        var stats = await Rows(db, "SELECT CONCAT(SortOrder, N'|', LabelAr, N'|', [Value]) AS [Value] FROM Stats ORDER BY SortOrder");
        Assert.Equal(["1|رائد أعمال|100000", "2|شركة ناشئة|10000", "3|فعالية سنوياً|999", "4|صندوق استثمار|500"], stats);

        // Menu: the eight one-page anchors deactivated (not deleted); the admin's own link stays active.
        Assert.Equal(9, await db.NavItems.CountAsync());
        Assert.Equal(["رابط المدير"], await db.NavItems.Where(n => n.IsActive).Select(n => n.TitleAr).ToListAsync());

        // New reference rows.
        Assert.Equal(20, await db.Pages.CountAsync()); // + privacy (AddAdminControl)
        // 13 page lists (AddSiteStructure, 101 rows) + the 4 choice lists of the Join form (AddLeadForms, 18 rows).
        Assert.Equal(17, await db.ContentItems.Select(i => i.Group).Distinct().CountAsync());
        Assert.Equal(119, await db.ContentItems.CountAsync());
        Assert.Equal("https://linktr.ee/Innovest.HoldingOfficial", await db.SocialLinks.Where(x => x.Network == "linktree").Select(x => x.Url).SingleAsync());

        // A second run of both migrations' SQL changes nothing (every write is guarded).
        var first = await MigrationTests.SnapshotAsync(db, maxNav: 100);
        foreach (var migration in new Migration[] { new AddSiteStructure(), new ApplyCompanyProfile() })
            foreach (var sql in migration.UpOperations.OfType<SqlOperation>())
                await db.Database.ExecuteSqlRawAsync(sql.Sql.Replace("{", "{{").Replace("}", "}}")); // literal braces of the {highlight} titles
        Assert.Equal(first, await MigrationTests.SnapshotAsync(db, maxNav: 100));
        Assert.Equal(realBefore, await RealDataAsync(db));
    }

    [Fact] // a clean database of the bilingual release gets the whole profile, including the two extra impact figures
    public async Task Unedited_database_gets_the_whole_profile()
    {
        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        await db.Database.MigrateAsync();
        Assert.Equal(
            ["1|رائد أعمال|100000|+", "2|شركة ناشئة|10000|+", "3|مستثمر|5000|+", "4|صندوق استثمار|500|+", "5|دولة عربية|22|", "6|شريك استراتيجي|100|+"],
            await Rows(db, "SELECT CONCAT(SortOrder, N'|', LabelAr, N'|', [Value], N'|', Suffix) AS [Value] FROM Stats ORDER BY SortOrder"));
        Assert.Equal("info@innovest-holding.com", await Content(db, "contact_email"));
        Assert.Equal("/assets/brand/og-default.jpg", await Content(db, "seo_og_image"));
        Assert.Equal(0, await db.NavItems.CountAsync(n => n.IsActive));
    }

    [Fact] // Down restores the values of the bilingual release; Up again reaches the same state
    public async Task Down_restores_the_pr_b_state_and_up_again_reaches_the_same_state()
    {
        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        const string prb = """
            SELECT CONCAT(N'Sc|', SectionKey, N'|', ContentAr, N'|', ContentEn) AS [Value] FROM SiteContents WHERE SectionKey <> N'sys_timestamps_utc'
            UNION ALL SELECT CONCAT(N'Pl|', TitleAr, N'|', TitleEn, N'|', DescriptionAr, N'|', DescriptionEn, N'|', ShortTitleAr, N'|', ShortTitleEn, N'|', ShowInFooter, N'|', IconSvg) FROM Platforms
            UNION ALL SELECT CONCAT(N'St|', LabelAr, N'|', LabelEn, N'|', ShortLabelAr, N'|', ShortLabelEn, N'|', [Value], N'|', Suffix, N'|', IconSvg) FROM Stats
            UNION ALL SELECT CONCAT(N'Nav|', TitleAr, N'|', IsActive) FROM NavItems
            UNION ALL SELECT CONCAT(N'So|', Network, N'|', Url) FROM SocialLinks
            """;
        var before = (await Rows(db, prb)).Order(StringComparer.Ordinal).ToList();
        Assert.True(before.Count > 80, $"{before.Count} rows compared");

        await db.Database.MigrateAsync();
        var head = await MigrationTests.SnapshotAsync(db);

        await db.GetService<IMigrator>().MigrateAsync(MigrationTests.Bilingual);
        Assert.Equal(before, (await Rows(db, prb)).Order(StringComparer.Ordinal).ToList());

        await db.Database.MigrateAsync(); // up again to the latest migration
        Assert.Equal(head, await MigrationTests.SnapshotAsync(db));
    }

    // ── Every written column is guarded on its own (an admin edit in any one column survives the profile) ──

    private const string StatCols = "Id, SortOrder, LabelAr, LabelEn, ShortLabelAr, ShortLabelEn, [Value], Suffix, IconSvg";
    private const string PlatformCols = "Id, TitleAr, TitleEn, DescriptionAr, DescriptionEn, ShortTitleAr, ShortTitleEn, ShowInFooter, IconSvg, SortOrder";

    /// <summary>Rows of a table as cell arrays (NULL written as ∅), ordered by Id.</summary>
    private static async Task<List<string[]>> Cells(Data.AppDbContext db, string table, string cols, string where = "1 = 1")
    {
        var concat = string.Join(", N'§', ", cols.Split(", ").Select(c => $"ISNULL(CONVERT(nvarchar(max), {c}), N'∅')"));
        return (await Rows(db, $"SELECT CONCAT({concat}) AS [Value] FROM {table} WHERE {where} ORDER BY Id"))
            .Select(r => r.Split('§')).ToList();
    }

    private static Task<List<string[]>> SiteCells(Data.AppDbContext db, string keys) =>
        Cells(db, "SiteContents", "Id, SectionKey, ContentAr, ContentEn", $"SectionKey IN ({keys})");

    private static int Col(string cols, string name) => Array.IndexOf(cols.Split(", "), name);

    /// <summary>The result of the profile on an unedited database of the bilingual release.</summary>
    private static async Task<(List<string[]> Stats, List<string[]> Platforms, List<string[]> Site)> UneditedResultAsync(string keys)
    {
        await using var scope = await AtPrBStateAsync();
        await scope.Db.Database.MigrateAsync();
        return (await Cells(scope.Db, "Stats", StatCols), await Cells(scope.Db, "Platforms", PlatformCols), await SiteCells(scope.Db, keys));
    }

    private static string Flat(IEnumerable<string[]> rows) => string.Join("\n", rows.Select(r => string.Join("|", r)));

    [Fact] // an admin edit in ANY non-identity column (English or Arabic short text, suffix, icon, footer flag) is kept,
           // and every other column of the same row still receives the profile value
    public async Task Admin_edit_in_each_guarded_column_survives_and_the_rest_of_the_row_is_updated()
    {
        const string keys = "N'hero_title', N'tagline'";
        var unedited = await UneditedResultAsync(keys);

        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        // One edited column per row, so each row proves one guard (Stats 1–4, Platforms 1–6, two SiteContents keys).
        var edits = new (string Table, int Id, string Column, string Value)[]
        {
            ("Stats", 1, "LabelEn", "Partners (admin)"),
            ("Stats", 2, "ShortLabelAr", "مستثمرون"),
            ("Stats", 2, "ShortLabelEn", "Investors (admin)"),
            ("Stats", 3, "Suffix", "%"),
            ("Stats", 4, "IconSvg", "<circle cx=\"12\" cy=\"12\" r=\"2\"/>"),
            ("Platforms", 1, "TitleEn", "Training (admin)"),
            ("Platforms", 2, "DescriptionEn", "Investors description (admin)"),
            ("Platforms", 3, "ShortTitleAr", "إعلام"),
            ("Platforms", 4, "ShortTitleEn", "Contests (admin)"),
            ("Platforms", 5, "IconSvg", "<circle cx=\"12\" cy=\"12\" r=\"3\"/>"),
            ("Platforms", 6, "ShowInFooter", "0"),
        };
        foreach (var e in edits)
        {
            // Table and column names are test constants; the value travels as a parameter.
            object value = e.Column == "ShowInFooter" ? e.Value == "1" : e.Value;
            await db.Database.ExecuteSqlRawAsync("UPDATE " + e.Table + " SET " + e.Column + " = {0} WHERE Id = {1};", value, e.Id);
        }
        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentEn = N'Hero title (admin)' WHERE SectionKey = N'hero_title';");
        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentEn = NULL WHERE SectionKey = N'tagline';"); // cleared by the admin

        await db.Database.MigrateAsync();

        // Expected = the unedited result with exactly the edited cells holding the admin's value.
        var stats = unedited.Stats.Select(r => (string[])r.Clone()).ToList();
        var platforms = unedited.Platforms.Select(r => (string[])r.Clone()).ToList();
        foreach (var e in edits)
        {
            var (rows, cols) = e.Table == "Stats" ? (stats, StatCols) : (platforms, PlatformCols);
            rows.Single(r => r[0] == e.Id.ToString())[Col(cols, e.Column)] = e.Value;
        }
        var site = unedited.Site.Select(r => (string[])r.Clone()).ToList();
        site.Single(r => r[1] == "hero_title")[3] = "Hero title (admin)";
        site.Single(r => r[1] == "tagline")[3] = "∅";

        Assert.Equal(6, stats.Count); // the four rows are still profile figures, so the two extra figures are added
        Assert.Equal(Flat(stats), Flat(await Cells(db, "Stats", StatCols)));
        Assert.Equal(Flat(platforms), Flat(await Cells(db, "Platforms", PlatformCols)));
        Assert.Equal(Flat(site), Flat(await SiteCells(db, keys)));
        // The Arabic of those rows did receive the profile (the guard is per column, not per row).
        Assert.Equal("نربط رواد الأعمال بالمستثمرين في أنحاء الوطن العربي", site.Single(r => r[1] == "hero_title")[2]);
        Assert.Equal("رائد أعمال", stats[0][Col(StatCols, "LabelAr")]);
    }

    [Fact] // an admin edit in an identity column (Arabic label/title/description, figure value, menu title) keeps the WHOLE row
    public async Task Admin_edit_in_an_identity_column_keeps_the_whole_row()
    {
        const string keys = "N'vision_text', N'contact_email'";
        var unedited = await UneditedResultAsync(keys);

        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Stats SET LabelAr = N'شريك استراتيجي معتمد' WHERE Id = 1;
            UPDATE Stats SET [Value] = 2600 WHERE Id = 2;
            UPDATE Platforms SET TitleAr = N'التدريب' WHERE Id = 1;
            UPDATE Platforms SET DescriptionAr = N'وصف عدّله المدير' WHERE Id = 2;
            UPDATE SiteContents SET ContentAr = N'رؤية عدّلها المدير' WHERE SectionKey = N'vision_text';
            UPDATE SiteContents SET ContentAr = N'admin@example.com' WHERE SectionKey = N'contact_email';
            UPDATE NavItems SET TitleAr = N'الصفحة الرئيسية' WHERE Href = N'#hero';
            """);
        var statsBefore = await Cells(db, "Stats", StatCols);
        var platformsBefore = await Cells(db, "Platforms", PlatformCols);
        var siteBefore = await SiteCells(db, keys);

        await db.Database.MigrateAsync();

        // Edited rows: byte-identical to before; the other rows: as on an unedited database. No extra figures (the table
        // no longer holds exactly the four profile rows).
        var stats = await Cells(db, "Stats", StatCols);
        Assert.Equal(4, stats.Count);
        Assert.Equal(Flat(statsBefore.Take(2).Concat(unedited.Stats.Skip(2).Take(2))), Flat(stats));
        Assert.Equal(Flat(platformsBefore.Take(2).Concat(unedited.Platforms.Skip(2))), Flat(await Cells(db, "Platforms", PlatformCols)));
        Assert.Equal(Flat(siteBefore), Flat(await SiteCells(db, keys)));
        Assert.Equal(["الصفحة الرئيسية"], await db.NavItems.Where(n => n.IsActive).Select(n => n.TitleAr).ToListAsync());
    }

    [Fact] // the shortened English meta texts replace only values still equal to what AddSiteStructure wrote; Down mirrors it
    public async Task Meta_shortening_changes_only_unedited_values_and_down_restores_them()
    {
        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        await db.GetService<IMigrator>().MigrateAsync(Profile);
        await db.Database.ExecuteSqlRawAsync("UPDATE Pages SET MetaTitleEn = N'About (admin)' WHERE [Key] = N'about';");
        Task<string> Meta(string key, string column) =>
            One(db, "SELECT " + column + " AS [Value] FROM Pages WHERE [Key] = N'" + key + "'");
        Task<int> PlatformSortIndex() =>
            db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE name = N'IX_Platforms_SortOrder'").SingleAsync();
        Assert.Equal(1, await PlatformSortIndex());

        await db.Database.MigrateAsync();
        Assert.Equal("Arab Platform for Innovation & Investment", await Meta("home", "MetaTitleEn"));
        Assert.Equal("About (admin)", await Meta("about", "MetaTitleEn"));                       // admin edit kept
        Assert.Equal(147, (await Meta("forum", "MetaDescriptionEn")).Length);
        Assert.Equal(0, await PlatformSortIndex());

        await db.GetService<IMigrator>().MigrateAsync(Profile);
        Assert.Equal("The Arab Platform for Entrepreneurship, Innovation & Investment", await Meta("home", "MetaTitleEn"));
        Assert.Equal("About (admin)", await Meta("about", "MetaTitleEn"));
        Assert.Equal(165, (await Meta("forum", "MetaDescriptionEn")).Length);
        Assert.Equal(1, await PlatformSortIndex());
    }

    [Fact] // a database that ran the first profile text gets the 7 English values; an admin's English is never replaced (CorrectCoreValuesEnglish)
    public async Task English_core_values_are_corrected_only_while_still_the_first_profile_text()
    {
        const string first = "Innovation, partnership, transparency, sustainability, professionalism, impact and empowerment";
        const string corrected = "Innovation, Partnership, Transparency, Sustainability, Professionalism, Impact, Empowerment";
        await using var scope = await AtPrBStateAsync();
        var db = scope.Db;
        await db.GetService<IMigrator>().MigrateAsync(Profile);
        Assert.Equal(corrected, await Content(db, "values_text", true));                      // a fresh run writes it directly

        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentEn = {0} WHERE SectionKey = N'values_text';", first);
        await db.Database.MigrateAsync();
        Assert.Equal(corrected, await Content(db, "values_text", true));                      // the first text is corrected

        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentEn = N'Values (admin)' WHERE SectionKey = N'values_text';");
        foreach (var sql in new CorrectCoreValuesEnglish().UpOperations.OfType<SqlOperation>())
            await db.Database.ExecuteSqlRawAsync(sql.Sql.Replace("{", "{{").Replace("}", "}}"));
        Assert.Equal("Values (admin)", await Content(db, "values_text", true));               // an admin edit is kept
    }
}
