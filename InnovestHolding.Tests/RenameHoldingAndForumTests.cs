using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

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
    private const string NewForumAr = RenameHoldingAndForum.ForumNameAr;
    private const string ForumEn = "Innovest Arab Forum"; // the official English name: never rewritten

    /// <summary>Tables the migration must never write (lead/visitor data, accounts, slug history).</summary>
    private static readonly string[] Untouchable =
        ["Registrations", "ContactMessages", "NetworkApplications", "SponsorshipEnquiries", "OpportunityInterests", "Users", "SlugRedirects"];

    /// <summary>Hand-made 100-character title (NavItems.TitleAr was nvarchar(100)) holding the old word.</summary>
    private static readonly string NearLimit = ("إنوفست هولدينغ " + new string('ب', 100))[..100];

    /// <summary>The worst case for a 100-character column: the old forum name 5 times (95) + 5 letters, exactly the old
    /// limit; renamed in full it is 165 characters (the column is widened to 170 before the rename).</summary>
    private static readonly string MaxForum = string.Concat(Enumerable.Repeat(OldForumAr, 5)) + new string('ب', 5);

    /// <summary>A 200-character event title (Events.TitleAr was nvarchar(200)) with one old forum name: 213 after.</summary>
    private static readonly string MaxForumTitle = OldForumAr + " " + new string('ب', 200 - OldForumAr.Length - 1);

    /// <summary>Admin edits and non-content data on top of the C5 state (synthetic, no real person's data).</summary>
    private static readonly string Fixture = $"""
        UPDATE SiteContents SET ContentAr = N'نص كتبه المشرف: إنوفست هولدينغ (مجموعة الهولدينغ) ترعى منتدى إنوفست العربي.',
            ContentEn = N'Admin text: Innovest Holding hosts the Innovest Arab Forum, the largest Arab forum.' WHERE SectionKey = 'about_text_2';
        UPDATE Events SET DescriptionAr = N'تنظمها إنوفست هولدينغ ضمن منتدى إنوفست العربي.', DescriptionEn = N'Organised by Innovest Holding as part of the Innovest Arab Forum.'
            WHERE Id = (SELECT MIN(Id) FROM Events);
        INSERT INTO Partners (NameAr, NameEn, Url, SortOrder, IsActive, ShowOnHome, CreatedAt, UpdatedAt)
            VALUES (N'المنتدى العربي للبيئة والتنمية', N'Arab Forum for Environment and Development', NULL, 5, 1, 0, '2026-10-01T10:00:00', '2026-10-01T10:00:00');
        INSERT INTO Partners (NameAr, NameEn, Url, SortOrder, IsActive, ShowOnHome, CreatedAt, UpdatedAt)
            VALUES (N'منتدى الابتكار والاستثمار العربي', N'Innovation and Investment Arab Forum', NULL, 6, 1, 0, '2026-10-01T10:00:00', '2026-10-01T10:00:00');
        UPDATE SiteContents SET ContentAr = N'الشركة القابضة للاستثمار' WHERE SectionKey = 'tagline';
        UPDATE SiteContents SET ContentAr = N'هولدينغ' WHERE SectionKey = 'org_address_locality';
        INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter, ShowInHeader)
            VALUES (N'{NearLimit}', N'INNOVEST ARAB FORUM', N'https://example.com/هولدينغ/Innovest-Arab-Forum', 60, 0, 1, 0, 1);
        INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter, ShowInHeader)
            VALUES (N'{MaxForum}', N'{MaxForum}', N'/forum', 61, 0, 1, 0, 1);
        UPDATE Events SET TitleAr = N'{MaxForumTitle}' WHERE Id = (SELECT MAX(Id) FROM Events);
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

    /// <summary>"Table.Column = definition" of every default constraint on a bounded nvarchar …Ar/…En column.</summary>
    private static Task<List<string>> TextDefaultsAsync(AppDbContext db) => db.Database.SqlQueryRaw<string>(
        "SELECT OBJECT_NAME(d.parent_object_id) + '.' + c.name + ' = ' + d.definition AS [Value] FROM sys.default_constraints d " +
        "JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id " +
        "WHERE TYPE_NAME(c.user_type_id) = 'nvarchar' AND c.max_length > 0 AND (c.name LIKE '%Ar' OR c.name LIKE '%En') ORDER BY 1").ToListAsync();

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
        var defaults = await TextDefaultsAsync(db);
        Assert.Equal(8, defaults.Count); // the N'' defaults of 8 widened columns (Events, MediaFiles, News)

        await migrator.MigrateAsync(Rename);
        Assert.Equal(defaults, await TextDefaultsAsync(db)); // widening keeps every column default

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
        Assert.Equal("نص كتبه المشرف: إنوفست القابضة (مجموعة القابضة) ترعى منتدى الإبتكار والإستثمار العربي.", await ContentArAsync(db, "about_text_2"));
        Assert.Equal("Admin text: Innovest Holding hosts the Innovest Arab Forum, the largest Arab forum.",
            await db.SiteContents.AsNoTracking().Where(c => c.SectionKey == "about_text_2").Select(c => c.ContentEn).SingleAsync());
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM SiteContents WHERE ContentAr LIKE N'%الالقابضة%'"));
        // Projected: the database is at RenameHoldingAndForum, before F10's Events.DateIsMonth column.
        var ev = await db.Events.AsNoTracking().OrderBy(e => e.Id).Select(e => new { e.DescriptionAr, e.DescriptionEn }).FirstAsync();
        Assert.Equal(("تنظمها إنوفست القابضة ضمن منتدى الإبتكار والإستثمار العربي.", "Organised by Innovest Holding as part of the Innovest Arab Forum."),
            (ev.DescriptionAr, ev.DescriptionEn));
        // Other organisations' names and the forum's long name are not the brand: kept in both languages (H2).
        var partners = await db.Partners.AsNoTracking().Where(p => p.Url == null && p.NameEn!.Contains("Forum")).OrderBy(p => p.SortOrder)
            .Select(p => p.NameAr + "|" + p.NameEn).ToListAsync();
        Assert.Equal(["المنتدى العربي للبيئة والتنمية|Arab Forum for Environment and Development",
            "منتدى الابتكار والاستثمار العربي|Innovation and Investment Arab Forum"], partners);
        Assert.Equal("الشركة القابضة للاستثمار", await ContentArAsync(db, "tagline"));
        Assert.Equal(OldHolding, await ContentArAsync(db, "org_address_locality")); // a non-text key is never rewritten
        var nav = await db.NavItems.AsNoTracking().SingleAsync(n => n.SortOrder == 60);
        Assert.Equal(NearLimit.Replace(OldHolding, "القابضة"), nav.TitleAr);
        Assert.Equal(100, nav.TitleAr.Length);
        Assert.Equal("INNOVEST ARAB FORUM", nav.TitleEn); // English is not rewritten
        Assert.Equal("https://example.com/هولدينغ/Innovest-Arab-Forum", nav.Href);
        // Max-length values holding the longer new name: renamed in full, nothing skipped or cut (Anas: no skipped row).
        var widest = await db.NavItems.AsNoTracking().SingleAsync(n => n.SortOrder == 61);
        Assert.Equal(string.Concat(Enumerable.Repeat(NewForumAr, 5)) + new string('ب', 5), widest.TitleAr);
        Assert.Equal(165, widest.TitleAr.Length);
        Assert.Equal(widest.TitleAr, widest.TitleEn); // Arabic typed into an English column is renamed too
        var title = await db.Events.AsNoTracking().OrderByDescending(e => e.Id).Select(e => e.TitleAr).FirstAsync();
        Assert.Equal(MaxForumTitle.Replace(OldForumAr, NewForumAr, StringComparison.Ordinal), title);
        Assert.Equal(213, title.Length);
        var forum = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == "forum");
        Assert.Equal((NewForumAr, ForumEn), (forum.TitleAr, forum.TitleEn));
        Assert.Equal(NewForumAr, forum.MetaTitleAr); // «… العربي للابتكار والاستثمار» does not repeat the long name
        Assert.StartsWith("The Innovest Arab Forum is the largest Arab forum", forum.IntroEn);
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
        Assert.Equal(defaults, await TextDefaultsAsync(db));
        Assert.Equal(100, await CountAsync(db, "SELECT CHARACTER_MAXIMUM_LENGTH AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NavItems' AND COLUMN_NAME = 'TitleAr'"));

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
        // about_text_2 IS in the log (Up changed it), so only Down's guard can keep this later edit (review H1).
        Assert.Equal(1, await CountAsync(db,
            $"SELECT COUNT(*) AS [Value] FROM [{RenameHoldingAndForum.LogTable}] l JOIN SiteContents s ON s.Id = l.RowId " +
            "WHERE l.TableName = 'SiteContents' AND l.ColumnName = 'ContentAr' AND s.SectionKey = 'about_text_2'"));
        await db.Database.ExecuteSqlRawAsync("UPDATE SiteContents SET ContentAr = N'إنوفست القابضة — عدّله المشرف' WHERE SectionKey = 'about_text_2'");

        await migrator.MigrateAsync(Before);

        Assert.Equal("إنوفست القابضة — عدّله المشرف", await ContentArAsync(db, "about_text_2"));
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
            "UPDATE Registrations SET EventTitle = REPLACE(EventTitle COLLATE Latin1_General_100_BIN2, N'منتدى إنوفست العربي', N'منتدى الإبتكار والإستثمار العربي');" +
            "UPDATE SponsorshipEnquiries SET PackageTitle = REPLACE(PackageTitle COLLATE Latin1_General_100_BIN2, N'منتدى إنوفست العربي', N'منتدى الإبتكار والإستثمار العربي');");
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
        // F10's ForumEditions table was created after this migration was frozen: AddForumContent runs AFTER the rename and
        // inserts text that already uses the new names (ForumContentMigrationTests pins that), so N8's list stays as it is.
        var expected = model.Where(m => !forbidden.Contains(m.Key) && m.Key != "ForumEditions")
            .SelectMany(m => m.Value.GetProperties().Where(p => p.ClrType == typeof(string) && Regex.IsMatch(p.GetColumnName(), "(Ar|En)$"))
                .Select(p => m.Key + "." + p.GetColumnName()))
            .Append("SocialLinks.Label").Order().ToList();
        var actual = targets.SelectMany(t => t.Columns.Select(c => t.Table + "." + c)).Order().ToList();
        Assert.Equal(expected, actual);

        // SiteContents: the non-text keys cover every key with a rule or marked neutral, every image, the markers —
        // and no plain text key is excluded.
        var nonText = ContentKeys.All.Where(k => k.Neutral || k.Rule != ContentRule.None).Select(k => k.Key)
            // The forum identity's image keys (F9) were added after this migration was frozen; like every image key they
            // hold random FileKeys, never text, so the REPLACE can never touch them — the frozen list stays as it is.
            .Concat(SiteImages.All.Select(i => i.Key).Except(ForumIdentity.Images.Select(i => i.Key))).Append(BrandImages.ThemeColorKey)
            .Append(ContentKeys.TimestampsUtcMarker).Append(ContentKeys.MediaInstance)
            // F10's keys come later too (AddForumContent inserts them already renamed): not part of the frozen list.
            .Except(AddForumContent.NewKeys.Select(k => k[0])).Distinct().Order().ToList();
        Assert.Equal(nonText, RenameHoldingAndForum.NonTextKeys.Order().ToList());

        // A phrase contained in another comes after it (longest first); no replacement holds a phrase to find (a later
        // pair never rewrites an earlier result). Growth is covered by the widening test.
        var pairs = RenameHoldingAndForum.Pairs;
        Assert.All(pairs, p => Assert.DoesNotContain(pairs, q => p.Replace.Contains(q.Find, StringComparison.Ordinal)));
        for (var i = 0; i < pairs.Length; i++)
            for (var j = 0; j < i; j++) // an earlier, shorter phrase would consume part of a later, longer one
                Assert.False(pairs[i].Find.Contains(pairs[j].Find, StringComparison.Ordinal), $"{pairs[i].Find} must come before {pairs[j].Find}");
        var withArticle = Array.FindIndex(pairs, p => p.Find == "الهولدينغ");
        Assert.True(withArticle >= 0 && withArticle < Array.FindIndex(pairs, p => p.Find == "هولدينغ"));
        Assert.DoesNotContain(pairs, p => p.Find.Contains("Holding", StringComparison.OrdinalIgnoreCase));
        // English is never rewritten (Anas, 2026-10-23: "Innovest Arab Forum" is the official English name).
        Assert.All(pairs, p => Assert.Matches("^[\u0600-\u06FF ]+$", p.Find));
        Assert.All(pairs.Where(p => p.Find.Contains("منتدى", StringComparison.Ordinal)), p => Assert.Equal(NewForumAr, p.Replace));
        // Only brand phrases (CTO decision H2/N1): no generic «المنتدى العربي» / "Arab Forum" pair.
        Assert.All(pairs, p => Assert.True(p.Find.Contains("هولدينغ") || p.Find.Contains("إنوفست") ||
                                           p.Find.Contains("Innovest", StringComparison.OrdinalIgnoreCase), p.Find));
    }

    [Fact] // Anas 2026-10-23 (no skipped rows): every bounded target column is widened, before the UPDATEs, enough for any value
    public void Up_widens_every_bounded_target_column_enough_for_the_longer_name()
    {
        using var db = MigrationTests.NewContext();
        var ops = new RenameHoldingAndForum().UpOperations;
        var alters = ops.OfType<AlterColumnOperation>().ToList();
        var firstSql = ops.ToList().FindIndex(o => o is SqlOperation);
        Assert.True(firstSql > 0 && ops.Take(firstSql).All(o => o is AlterColumnOperation), "the widening runs before the rename");
        Assert.Equal(firstSql, alters.Count);

        // The bound: a value of N characters holds at most N / (shortest growing phrase) phrases, each growing by at most
        // the largest growth. Pairs that do not grow need nothing.
        var growing = RenameHoldingAndForum.Pairs.Where(p => p.Replace.Length > p.Find.Length).ToList();
        Assert.NotEmpty(growing); // «منتدى إنوفست العربي» (19) → the official name (32)
        var shortest = growing.Min(p => p.Find.Length);
        var growth = growing.Max(p => p.Replace.Length - p.Find.Length);
        Assert.Equal((19, 13), (shortest, growth));

        var model = db.Model.GetEntityTypes().ToDictionary(e => e.GetTableName()!, e => e);
        var expected = new List<string>();
        foreach (var (table, cols) in RenameHoldingAndForum.Targets)
            foreach (var col in cols)
            {
                var alter = alters.SingleOrDefault(a => a.Table == table && a.Name == col);
                var now = model[table].GetProperties().Single(p => p.GetColumnName() == col).GetMaxLength();
                if (alter is null)
                {
                    Assert.True(now is null, $"{table}.{col} is bounded ({now}) but not widened");
                    continue;
                }
                expected.Add(table + "." + col);
                var old = alter.OldColumn.MaxLength!.Value;
                Assert.True(alter.MaxLength >= old + growth * (old / shortest), $"{table}.{col}: {old} -> {alter.MaxLength}");
                Assert.True(alter.MaxLength <= 4000, $"{table}.{col}"); // stays nvarchar(n): REPLACE cannot truncate it
                Assert.Equal(alter.MaxLength, now); // the EF model has the new length
                Assert.Equal(alter.OldColumn.IsNullable, alter.IsNullable);
                Assert.Equal(typeof(string), alter.ClrType);
            }
        Assert.Equal(expected.Order(), alters.Select(a => a.Table + "." + a.Name).Order()); // nothing else is altered
        Assert.Equal(115, alters.Count);
    }

    /// <summary>The page text as a reader (and a search engine) gets it: HTML entities and JSON \u escapes decoded.</summary>
    private static string Decode(string html) =>
        Regex.Replace(WebUtility.HtmlDecode(html), @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

    [Theory] // N8a-1/2, N8b-1: the rendered pages carry only the new names
    [InlineData("/", "إنوفست القابضة")]
    [InlineData("/forum", RenameHoldingAndForum.ForumNameAr)]
    [InlineData("/forum/events", RenameHoldingAndForum.ForumNameAr)]
    [InlineData("/en", "Innovest Holding")]
    [InlineData("/en/forum", ForumEn)]
    [InlineData("/en/forum/events", ForumEn)]
    public async Task Rendered_pages_show_the_new_names(string path, string expected)
    {
        var response = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = Decode(await response.Content.ReadAsStringAsync());
        Assert.Contains(expected, text);
        AssertNoOldName(path, text);
    }

    [Theory] // Anas 2026-10-23: the home forum card carries the official name (title and the components line)
    [InlineData("/", RenameHoldingAndForum.ForumNameAr)]
    [InlineData("/en", ForumEn)]
    public async Task Home_forum_card_shows_the_official_name(string path, string name)
    {
        var html = Decode(await f.NewClient().GetStringAsync(path)).Replace("\n", " ");
        var esc = Regex.Escape(name);
        Assert.Matches($"<article class=\"service-card[^\"]*\">(?:(?!</article>).)*<h3[^>]*>{esc}</h3>(?:(?!</article>).)*<li[^>]*>{esc}</li>", html);
    }

    /// <summary>Old forum names: «منتدى إنوفست» on its own or with «العربي» (any use), "Innovest Forum" (not followed by
    /// " Arab"), the unofficial spelling of the long name, the generic «المنتدى العربي», and the old company word.</summary>
    private static readonly Regex OldNames = new(
        "هولدينغ|منتدى إنوفست|Innovest Forum(?! Arab)|INNOVEST FORUM|منتدى الابتكار والاستثمار العربي|المنتدى العربي", RegexOptions.CultureInvariant);

    internal static void AssertNoOldName(string path, string text)
    {
        var m = OldNames.Match(text);
        if (!m.Success) return;
        var from = Math.Max(0, m.Index - 60);
        Assert.Fail($"{path} contains «{m.Value}» in …{text.Substring(from, Math.Min(140, text.Length - from))}…");
    }

    [Fact] // Standing Check #6 for the page check itself: each old name IS caught, the official names are not
    public void Old_name_check_catches_old_names_and_passes_the_official_ones()
    {
        foreach (var old in new[] { "منتدى إنوفست", "منتدى إنوفست العربي", "Innovest Forum", "the Innovest Forum 2025", "INNOVEST FORUM",
                     "إنوفست هولدينغ", "منتدى الابتكار والاستثمار العربي" })
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNoOldName("x", "<p>" + old + "</p>"));
        AssertNoOldName("x", "<p>" + RenameHoldingAndForum.ForumNameAr + " 2025 — Innovest Arab Forum 2025 — INNOVEST ARAB FORUM — إنوفست القابضة</p>");
    }

    [Fact] // SEO-N8: every sitemap URL (ar + en) carries no old name
    public async Task Every_sitemap_url_shows_no_old_name()
    {
        var client = f.NewClient();
        var xml = System.Xml.Linq.XDocument.Parse(await client.GetStringAsync("/sitemap.xml"));
        var paths = xml.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => new Uri(e.Value).PathAndQuery).Distinct().ToList();
        Assert.True(paths.Count >= 20, $"{paths.Count} sitemap URLs");
        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {(int)response.StatusCode}");
            AssertNoOldName(path, Decode(await response.Content.ReadAsStringAsync()));
        }
    }
}
