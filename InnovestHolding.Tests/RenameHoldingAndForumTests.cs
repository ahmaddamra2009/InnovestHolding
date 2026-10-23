using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InnovestHolding.Tests;

/// <summary>
/// N8 RenameHoldingAndForum: on a C5 database (old production rows from <see cref="PreBilingualSeed"/>, admin edits,
/// visitor leads) Up renames «هولدينغ» → «القابضة» and the forum name in content text only; lead tables, Users,
/// SlugRedirects, links and non-text keys stay byte-identical; English "Innovest Holding" stays; a second run is a
/// no-op; Down restores exactly what Up changed and keeps later admin edits. Own LocalDB migration database.
/// </summary>
[Collection("App")]
public class RenameHoldingAndForumTests(TestAppFactory f)
{
    private const string Before = "20261023011038_AddAdminControl";
    private const string Rename = "20261023141145_RenameHoldingAndForum";

    private const string OldHolding = "هولدينغ";
    private const string OldForumAr = "منتدى إنوفست العربي";
    private const string OldForumEn = "Arab Forum";

    /// <summary>Tables the migration must never write (lead/visitor data, accounts, slug history).</summary>
    private static readonly string[] Untouchable =
        ["Registrations", "ContactMessages", "NetworkApplications", "SponsorshipEnquiries", "OpportunityInterests", "Users", "SlugRedirects"];

    /// <summary>Hand-made 100-character title (NavItems.TitleAr is nvarchar(100)) holding the old word.</summary>
    private static readonly string NearLimit = ("إنوفست هولدينغ " + new string('ب', 100))[..100];

    /// <summary>Admin edits and non-content data on top of the C5 state (synthetic, no real person's data).</summary>
    private static readonly string Fixture = $"""
        UPDATE SiteContents SET ContentAr = N'نص كتبه المشرف: إنوفست هولدينغ ترعى منتدى إنوفست العربي والمنتدى العربي.',
            ContentEn = N'Admin text: Innovest Holding hosts the Innovest Arab Forum, the Arab Forum Events and the largest Arab forum.' WHERE SectionKey = 'about_text_2';
        UPDATE SiteContents SET ContentAr = N'الشركة القابضة للاستثمار' WHERE SectionKey = 'tagline';
        UPDATE SiteContents SET ContentAr = N'هولدينغ' WHERE SectionKey = 'org_address_locality';
        INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter, ShowInHeader)
            VALUES (N'{NearLimit}', N'INNOVEST ARAB FORUM', N'https://example.com/هولدينغ/Innovest-Arab-Forum', 60, 0, 1, 0, 1);
        INSERT INTO Partners (NameAr, NameEn, Url, SortOrder, IsActive, ShowOnHome, CreatedAt, UpdatedAt)
            VALUES (N'شركة القابضة الأولى', N'First Holding Co', N'https://example.com/innovest-arab-forum', 4, 1, 0, '2026-10-01T10:00:00', '2026-10-01T10:00:00');
        INSERT INTO SlugRedirects (EntityType, OldSlug, EntityId) VALUES ('event', 'innovest-arab-forum', 1);
        """;

    private static async Task SeedLeadsAsync(AppDbContext db)
    {
        var t = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        db.Registrations.Add(new Registration
        {
            FullName = "زائر تجريبي", Email = "visitor@example.com", Phone = "0100000000", Interest = "منتدى إنوفست العربي",
            Kind = Registration.Quick, EventTitle = "فعالية منتدى إنوفست العربي — إنوفست هولدينغ", Culture = "ar", CreatedAt = t,
            AdminNote = "Innovest Arab Forum lead",
        });
        db.ContactMessages.Add(new ContactMessage
            { FullName = "Test Visitor", Email = "visitor@example.com", Subject = "إنوفست هولدينغ", Message = "About the Innovest Arab Forum", CreatedAt = t });
        db.NetworkApplications.Add(new NetworkApplication
            { FullName = "مستثمر تجريبي", Email = "investor@example.com", Country = "EG", Message = "منتدى إنوفست العربي — هولدينغ", ConsentAt = t, CreatedAt = t });
        db.SponsorshipEnquiries.Add(new SponsorshipEnquiry
            { PackageTitle = "راعي منتدى إنوفست العربي", CompanyName = "شركة هولدينغ", ContactName = "Test", Email = "s@example.com", ConsentAt = t, CreatedAt = t });
        db.OpportunityInterests.Add(new OpportunityInterest
            { OpportunityTitle = "Innovest Arab Forum opportunity", FullName = "Test", Email = "o@example.com", Message = "إنوفست هولدينغ", ConsentAt = t, CreatedAt = t });
        db.Users.Add(new AppUser { Username = "هولدينغ-admin", PasswordHash = "x" });
        await db.SaveChangesAsync();
    }

    private static async Task<AppDbContext> AtC5WithDataAsync()
    {
        var db = await MigrationTests.AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql); // what production held before the bilingual release
        await db.GetService<IMigrator>().MigrateAsync(Before);
        await db.Database.ExecuteSqlRawAsync(Fixture);
        await SeedLeadsAsync(db);
        return db;
    }

    private static Task<Dictionary<string, List<string>>> ColumnsAsync(AppDbContext db) => AdminControlMigrationTests.ColumnsAsync(db);

    /// <summary>The C5 tables only (the migration's own log table is not part of the before/after comparison).</summary>
    private static async Task<Dictionary<string, List<string>>> ModelColumnsAsync(AppDbContext db) =>
        (await ColumnsAsync(db)).Where(c => c.Key != RenameHoldingAndForum.LogTable).ToDictionary();

    /// <summary>Every table, every column NOT in the migration's targets.</summary>
    private static Dictionary<string, List<string>> NonTargetColumns(Dictionary<string, List<string>> columns)
    {
        var targets = RenameHoldingAndForum.Targets.ToDictionary(t => t.Table, t => t.Columns);
        return columns.ToDictionary(c => c.Key, c => c.Value.Where(col => !targets.TryGetValue(c.Key, out var t) || !t.Contains(col)).ToList());
    }

    private static Task<int> CountAsync(AppDbContext db, string sql) => db.Database.SqlQueryRaw<int>(sql).SingleAsync();

    /// <summary>Rows of target columns that still hold an old phrase (binary match; the excluded keys aside).</summary>
    private static async Task<int> OldPhraseCountAsync(AppDbContext db)
    {
        var n = 0;
        foreach (var (table, columns) in RenameHoldingAndForum.Targets) // names from the migration's constant list
            foreach (var column in columns)
            {
                var keys = table == "SiteContents"
                    ? " AND SectionKey NOT IN (" + string.Join(", ", RenameHoldingAndForum.NonTextKeys.Select(k => "'" + k + "'")) + ")"
                    : "";
                var any = string.Join(" OR ", RenameHoldingAndForum.Pairs.Select(p => $"[{column}] COLLATE Latin1_General_100_BIN2 LIKE N'%{p.Find}%'"));
                n += await CountAsync(db, $"SELECT COUNT(*) AS [Value] FROM [{table}] WHERE ({any}){keys}");
            }
        return n;
    }

    private static async Task<int> EnglishHoldingCountAsync(AppDbContext db)
    {
        var n = 0;
        foreach (var (table, columns) in RenameHoldingAndForum.Targets)
            foreach (var column in columns)
                n += await CountAsync(db, $"SELECT COUNT(*) AS [Value] FROM [{table}] WHERE [{column}] COLLATE Latin1_General_100_BIN2 LIKE N'%Innovest Holding%'");
        return n;
    }

    private static Task<string> ContentArAsync(AppDbContext db, string key) =>
        db.SiteContents.AsNoTracking().Where(c => c.SectionKey == key).Select(c => c.ContentAr).SingleAsync();

    [Fact] // N8a-8/9, N8b-3/5: content renamed, everything else byte-identical, idempotent, Down/Up round-trips
    public async Task Up_renames_content_only_is_idempotent_and_down_restores()
    {
        await using var db = await AtC5WithDataAsync();
        var migrator = db.GetService<IMigrator>();

        var columns = await ColumnsAsync(db);
        Assert.DoesNotContain(RenameHoldingAndForum.LogTable, columns.Keys);
        var before = await AdminControlMigrationTests.DumpAsync(db, columns);
        var nonTarget = NonTargetColumns(columns);
        var nonTargetBefore = await AdminControlMigrationTests.DumpAsync(db, nonTarget);
        var oldBefore = await OldPhraseCountAsync(db);
        Assert.True(oldBefore >= 40, $"{oldBefore} content values hold an old name before Up"); // the scope of the run
        var englishHolding = await EnglishHoldingCountAsync(db);
        Assert.True(englishHolding >= 10, $"{englishHolding} values hold \"Innovest Holding\"");
        foreach (var table in Untouchable) Assert.True(await CountAsync(db, $"SELECT COUNT(*) AS [Value] FROM [{table}]") >= 1, table);

        await migrator.MigrateAsync(Rename);

        // Lead tables, Users, SlugRedirects and every non-target column (slugs, links, keys, ids, dates): identical.
        var nonTargetAfter = await AdminControlMigrationTests.DumpAsync(db, nonTarget);
        foreach (var table in Untouchable) Assert.True(before[table] == nonTargetAfter[table], table + " changed");
        var changed = nonTarget.Keys.Where(t => nonTargetBefore[t] != nonTargetAfter[t]).ToList();
        Assert.True(changed.Count == 0, "non-target columns changed: " + string.Join(", ", changed));

        // No content value keeps an old name; English "Innovest Holding" is untouched.
        Assert.Equal(0, await OldPhraseCountAsync(db));
        Assert.Equal(englishHolding, await EnglishHoldingCountAsync(db));
        var logged = await CountAsync(db, $"SELECT COUNT(*) AS [Value] FROM [{RenameHoldingAndForum.LogTable}]");
        Assert.Equal(oldBefore, logged); // one log row per changed value

        // Specific rows.
        Assert.Equal("نص كتبه المشرف: إنوفست القابضة ترعى منتدى إنوفست والمنتدى.", await ContentArAsync(db, "about_text_2"));
        Assert.Equal("Admin text: Innovest Holding hosts the Innovest Forum, the Forum Events and the largest Arab forum.",
            await db.SiteContents.AsNoTracking().Where(c => c.SectionKey == "about_text_2").Select(c => c.ContentEn).SingleAsync());
        Assert.Equal("الشركة القابضة للاستثمار", await ContentArAsync(db, "tagline"));
        Assert.Equal(OldHolding, await ContentArAsync(db, "org_address_locality")); // a non-text key is never rewritten
        var nav = await db.NavItems.AsNoTracking().SingleAsync(n => n.SortOrder == 60);
        Assert.Equal(NearLimit.Replace(OldHolding, "القابضة"), nav.TitleAr);
        Assert.Equal(100, nav.TitleAr.Length);
        Assert.Equal("INNOVEST FORUM", nav.TitleEn);
        Assert.Equal("https://example.com/هولدينغ/Innovest-Arab-Forum", nav.Href);
        var forum = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == "forum");
        Assert.Equal(("منتدى إنوفست", "Innovest Forum"), (forum.TitleAr, forum.TitleEn));
        Assert.StartsWith("The Innovest Forum is the largest Arab forum", forum.IntroEn);
        var privacy = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == "privacy");
        Assert.StartsWith("كيف يتعامل موقع إنوفست القابضة", privacy.MetaDescriptionAr);
        Assert.StartsWith("How the Innovest Holding website handles", privacy.MetaDescriptionEn);
        Assert.Contains("إنوفست القابضة", await ContentArAsync(db, "site_name"));
        Assert.Equal("ما هي إنوفست القابضة؟", (await db.Faqs.AsNoTracking().SingleAsync(q => q.Id == 1)).QuestionAr);

        // A second run of Up's statements changes nothing (and logs nothing).
        var allColumns = await ColumnsAsync(db);
        var after = await AdminControlMigrationTests.DumpAsync(db, allColumns);
        foreach (var sql in RenameHoldingAndForum.UpSql()) await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(after, await AdminControlMigrationTests.DumpAsync(db, allColumns));

        // Down: every C5 value comes back exactly (including the pre-existing «القابضة»), the log table is gone.
        await migrator.MigrateAsync(Before);
        Assert.Equal(columns.Keys.Order(), (await ColumnsAsync(db)).Keys.Order());
        Assert.Equal(before, await AdminControlMigrationTests.DumpAsync(db, columns));

        // Up again reaches the same state.
        await migrator.MigrateAsync(Rename);
        Assert.Equal(await AdminControlMigrationTests.DumpAsync(db, await ModelColumnsAsync(db)),
            after.Where(a => a.Key != RenameHoldingAndForum.LogTable).ToDictionary());
    }

    [Fact] // Security MUST 8: Down keeps what an admin wrote after Up, and text that said «القابضة» before Up
    public async Task Down_keeps_admin_edits_made_after_up()
    {
        await using var db = await AtC5WithDataAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Rename);
        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentAr = N'إنوفست القابضة — عدّله المشرف' WHERE SectionKey = 'hero_title'");

        await migrator.MigrateAsync(Before);

        Assert.Equal("إنوفست القابضة — عدّله المشرف", await ContentArAsync(db, "hero_title"));
        Assert.Equal("الشركة القابضة للاستثمار", await ContentArAsync(db, "tagline"));
        Assert.Equal("شركة القابضة الأولى", await db.Partners.AsNoTracking().Where(p => p.SortOrder == 4 && p.NameEn == "First Holding Co").Select(p => p.NameAr).SingleAsync());
        Assert.Contains("إنوفست هولدينغ", await ContentArAsync(db, "about_text_1")); // an untouched-since value is restored
    }

    [Fact] // Standing Check #6 for N8a-9: the lead checksum DOES see an over-broad REPLACE
    public async Task Negative_control_an_over_broad_replace_is_caught_by_the_lead_checksum()
    {
        await using var db = await AtC5WithDataAsync();
        var columns = await ColumnsAsync(db);
        var leads = columns.Where(c => Untouchable.Contains(c.Key)).ToDictionary();
        var before = await AdminControlMigrationTests.DumpAsync(db, leads);
        await db.GetService<IMigrator>().MigrateAsync(Rename);
        Assert.Equal(before, await AdminControlMigrationTests.DumpAsync(db, leads));

        // The same rename, wrongly widened to the lead snapshot columns, must make the check fail.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Registrations SET EventTitle = REPLACE(EventTitle COLLATE Latin1_General_100_BIN2, N'منتدى إنوفست العربي', N'منتدى إنوفست');" +
            "UPDATE SponsorshipEnquiries SET PackageTitle = REPLACE(PackageTitle COLLATE Latin1_General_100_BIN2, N'منتدى إنوفست العربي', N'منتدى إنوفست');");
        var wrong = await AdminControlMigrationTests.DumpAsync(db, leads);
        Assert.NotEqual(before["Registrations"], wrong["Registrations"]);
        Assert.NotEqual(before["SponsorshipEnquiries"], wrong["SponsorshipEnquiries"]);
    }

    [Fact] // Security MUST 1-3, 6, 7: the frozen lists are complete, safe and non-growing
    public void Frozen_lists_cover_the_model_and_exclude_non_content()
    {
        using var db = MigrationTests.NewContext();
        var forbidden = new HashSet<string>(Untouchable);
        var targets = RenameHoldingAndForum.Targets;
        Assert.DoesNotContain(targets, t => forbidden.Contains(t.Table));
        Assert.Equal(targets.Length, targets.Select(t => t.Table).Distinct().Count());

        // Every target column is a string column of the EF model, and no slug/link/key/contact column is named.
        var model = db.Model.GetEntityTypes().ToDictionary(e => e.GetTableName()!, e => e);
        foreach (var (table, cols) in targets)
            foreach (var col in cols)
            {
                var p = model[table].GetProperties().SingleOrDefault(p => p.GetColumnName() == col);
                Assert.True(p is not null && p.ClrType == typeof(string), $"{table}.{col}");
                Assert.DoesNotMatch(new Regex("Slug|Href|Url|FileKey|Key|Email|Phone|Svg", RegexOptions.IgnoreCase), col);
            }

        // Completeness, discovered from AppDbContext: every …Ar/…En text column of every content table is a target.
        var expected = model.Where(m => !forbidden.Contains(m.Key))
            .SelectMany(m => m.Value.GetProperties().Where(p => p.ClrType == typeof(string) && Regex.IsMatch(p.GetColumnName(), "(Ar|En)$"))
                .Select(p => m.Key + "." + p.GetColumnName()))
            .Append("SocialLinks.Label").Order().ToList();
        var actual = targets.SelectMany(t => t.Columns.Select(c => t.Table + "." + c)).Order().ToList();
        Assert.Equal(expected, actual);

        // SiteContents: the non-text keys cover every key with a rule or marked neutral, every image, the markers —
        // and no plain text key is excluded.
        var nonText = ContentKeys.All.Where(k => k.Neutral || k.Rule != ContentRule.None).Select(k => k.Key)
            .Concat(SiteImages.All.Select(i => i.Key)).Append(BrandImages.ThemeColorKey)
            .Append(ContentKeys.TimestampsUtcMarker).Append(ContentKeys.MediaInstance).Distinct().Order().ToList();
        Assert.Equal(nonText, RenameHoldingAndForum.NonTextKeys.Order().ToList());

        // Pairs never grow a value; a phrase contained in another comes after it (longest first).
        var pairs = RenameHoldingAndForum.Pairs;
        foreach (var (find, replace) in pairs) Assert.True(replace.Length <= find.Length, find);
        for (var i = 0; i < pairs.Length; i++)
            for (var j = 0; j < i; j++) // an earlier, shorter phrase would consume part of a later, longer one
                Assert.False(pairs[i].Find.Contains(pairs[j].Find, StringComparison.Ordinal), $"{pairs[i].Find} must come before {pairs[j].Find}");
        var forumEn = Array.FindIndex(pairs, p => p.Find == "Innovest Arab Forum");
        Assert.True(forumEn >= 0 && forumEn < Array.FindIndex(pairs, p => p.Find == "Arab Forum"));
        Assert.DoesNotContain(pairs, p => p.Find.Contains("Holding", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The page text as a reader (and a search engine) gets it: HTML entities and JSON \u escapes decoded.</summary>
    private static string Decode(string html) =>
        Regex.Replace(WebUtility.HtmlDecode(html), @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

    [Theory] // N8a-1/2, N8b-1: the rendered pages carry only the new names
    [InlineData("/", "إنوفست القابضة")]
    [InlineData("/forum", "منتدى إنوفست")]
    [InlineData("/forum/events", "منتدى إنوفست")]
    [InlineData("/en", "Innovest Holding")]
    [InlineData("/en/forum", "Innovest Forum")]
    [InlineData("/en/forum/events", "Innovest Forum")]
    public async Task Rendered_pages_show_the_new_names(string path, string expected)
    {
        var response = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = Decode(await response.Content.ReadAsStringAsync());
        Assert.Contains(expected, text);
        Assert.DoesNotContain(OldHolding, text);
        Assert.DoesNotContain(OldForumAr, text);
        Assert.DoesNotContain("المنتدى العربي", text);
        Assert.DoesNotContain(OldForumEn, text);
    }
}
