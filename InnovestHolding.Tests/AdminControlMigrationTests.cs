using System.Net;
using System.Reflection;
using System.Xml.Linq;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 AddAdminControl (Dev 1): T5.1 upgrade of a C4 database holding admin edits changes no existing value, a second run
/// is a no-op and Down/Up round-trips; T5.2 fresh == upgraded; T5.3 registry == frozen migration rows; T5.14 the TV intro
/// is filled only where empty; T5.13 the shared MoveAsync. Own LocalDB migration database (MigrationTests.Database).
/// </summary>
[Collection("App")]
public class AdminControlMigrationTests
{
    private const string Before = "20261022221815_AddLeadForms";
    private const string AdminControl = "20261023011038_AddAdminControl";

    private static readonly string[] NewTables = ["FooterColumns", "FooterLinks", "PageSections"];

    /// <summary>Real data of a C4 install + admin edits the migration must not touch (synthetic, no real person's data).</summary>
    private const string Fixture = """
        UPDATE SiteContents SET ContentAr = N'عنوان كتبه المشرف', ContentEn = N'Title the admin wrote' WHERE SectionKey = 'hero_title';
        INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt) VALUES ('hero_stats_limit', N'2', NULL, '2026-10-01T10:00:00');
        INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter) VALUES (N'رابط إضافي', N'Extra link', N'https://example.com/a', 50, 0, 1, 0);
        INSERT INTO NavItems (TitleAr, TitleEn, Href, SortOrder, HasDropdown, IsActive, ShowInFooter) VALUES (N'رابط معطل', NULL, N'https://example.com/b', 51, 0, 0, 1);
        UPDATE Pages SET ShowInMenu = 0 WHERE [Key] = 'forum.speakers';
        UPDATE Pages SET IntroAr = N'مقدمة كتبها المشرف', IntroEn = N'An intro the admin wrote' WHERE [Key] = 'tv';
        UPDATE Platforms SET IsActive = 0 WHERE Id = (SELECT MIN(Id) FROM Platforms);
        INSERT INTO Stats (LabelAr, LabelEn, [Value], Suffix, IconSvg, SortOrder) VALUES (N'رقم المشرف', NULL, 7, N'+', N'', 9);
        INSERT INTO Partners (NameAr, NameEn, SortOrder, IsActive, ShowOnHome, CreatedAt, UpdatedAt) VALUES (N'شريك', N'Partner', 3, 1, 0, '2026-10-01T10:00:00', '2026-10-01T10:00:00');
        UPDATE SocialLinks SET Url = N'https://www.linkedin.com/company/example' WHERE Network = 'linkedin';
        """;

    internal static async Task<Dictionary<string, List<string>>> ColumnsAsync(AppDbContext db) =>
        (await MigrationTests.Rows(db, "SELECT TABLE_NAME + '|' + COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME <> '__EFMigrationsHistory' ORDER BY TABLE_NAME, ORDINAL_POSITION"))
            .Select(r => r.Split('|')).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(p => p[1]).ToList());

    /// <summary>Every row of every table (the given columns) as JSON; with <paramref name="maxIds"/>, only the rows that existed then.</summary>
    internal static async Task<Dictionary<string, string>> DumpAsync(AppDbContext db, Dictionary<string, List<string>> columns,
        Dictionary<string, int>? maxIds = null, bool withoutIdsAndTimes = false)
    {
        var dump = new Dictionary<string, string>();
        foreach (var (table, all) in columns) // table and column names come from INFORMATION_SCHEMA
        {
            var cols = withoutIdsAndTimes ? all.Where(c => c is not ("Id" or "CreatedAt" or "UpdatedAt")).ToList() : all;
            var hasId = all.Contains("Id");
            var where = maxIds is not null && hasId ? " WHERE [Id] <= " + maxIds.GetValueOrDefault(table) : "";
            var order = hasId ? "[Id]" : string.Join(", ", cols.Select(c => "[" + c + "]"));
            var sql = "SELECT (SELECT " + string.Join(", ", cols.Select(c => "[" + c + "]")) + " FROM [" + table + "]" + where +
                      " ORDER BY " + order + " FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";
            dump[table] = await db.Database.SqlQueryRaw<string>(sql).SingleAsync() ?? "[]";
        }
        return dump;
    }

    private static async Task<Dictionary<string, int>> MaxIdsAsync(AppDbContext db, Dictionary<string, List<string>> columns)
    {
        var max = new Dictionary<string, int>();
        foreach (var table in columns.Where(c => c.Value.Contains("Id")).Select(c => c.Key))
            max[table] = await db.Database.SqlQueryRaw<int>("SELECT ISNULL(MAX(CAST([Id] AS int)), 0) AS [Value] FROM [" + table + "]").SingleAsync();
        return max;
    }

    /// <summary>The SQL of AddAdminControl's data steps (Up), as the migration runs them.</summary>
    internal static IReadOnlyList<string> UpDataSql()
    {
        var mb = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        typeof(AddAdminControl).GetMethod("UpData", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [mb]);
        return mb.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
    }

    private static Task<int> CountAsync(AppDbContext db, string sql) => db.Database.SqlQueryRaw<int>(sql).SingleAsync();

    [Fact] // T5.1 — NC-0.2: nothing existing changes; inserts only; idempotent; Down/Up round-trips
    public async Task Upgrade_from_C4_with_admin_edits_changes_no_existing_value_and_round_trips()
    {
        await using var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Before);
        await db.Database.ExecuteSqlRawAsync(Fixture);

        var columns = await ColumnsAsync(db);
        Assert.True(columns.Count >= 30, $"{columns.Count} tables");
        Assert.Empty(columns.Keys.Intersect(NewTables));
        var maxIds = await MaxIdsAsync(db, columns);
        var before = await DumpAsync(db, columns);
        Assert.Equal(7, await db.SocialLinks.CountAsync()); // all seven known networks already exist (C4)

        await migrator.MigrateAsync(AdminControl);

        // Every pre-existing row keeps every pre-existing column value (row checksum per table).
        var existingAfter = await DumpAsync(db, columns, maxIds);
        var changed = columns.Keys.Where(t => before[t] != existingAfter[t]).ToList();
        Assert.True(changed.Count == 0, "changed: " + string.Join(", ", changed));
        Assert.Equal(columns.Count + NewTables.Length, (await ColumnsAsync(db)).Count);

        // New columns reproduce today; footer membership = menu membership.
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM Pages WHERE ShowInFooter <> ShowInMenu"));
        Assert.False(await db.Pages.Where(p => p.Key == "forum.speakers").Select(p => p.ShowInFooter).SingleAsync());
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM Pages WHERE IsPublished = 0 OR EmptyActionPage IS NOT NULL"));
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM NavItems WHERE ShowInHeader = 0"));
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM Stats WHERE IsActive = 0"));
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM Platforms WHERE IconKey IS NOT NULL"));
        Assert.Equal(0, await CountAsync(db, "SELECT COUNT(*) AS [Value] FROM SocialLinks WHERE Label IS NOT NULL OR IconKey IS NOT NULL"));

        // Inserted rows.
        Assert.Equal(7, await db.SocialLinks.CountAsync()); // nothing missing → nothing inserted
        Assert.Equal(AddAdminControl.SectionRows.Sum(p => p.Sections.Length), await db.PageSections.CountAsync());
        Assert.Equal(0, await db.PageSections.CountAsync(s => !s.IsVisible));
        Assert.Equal(0, await db.FooterColumns.CountAsync() + await db.FooterLinks.CountAsync());
        var keys = await db.SiteContents.AsNoTracking().ToDictionaryAsync(k => k.SectionKey);
        foreach (var k in AddAdminControl.NewKeys.Where(k => k[0] != "hero_stats_limit"))
            Assert.Equal((k[1], k[2]), (keys[k[0]].ContentAr, keys[k[0]].ContentEn));
        Assert.Equal("2", keys["hero_stats_limit"].ContentAr); // the admin's value, not the migration's "4"
        Assert.Equal(("عنوان كتبه المشرف", "Title the admin wrote"), (keys["hero_title"].ContentAr, keys["hero_title"].ContentEn));
        var privacy = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == "privacy");
        Assert.Equal(AddAdminControl.PrivacyBodyAr, privacy.BodyAr);
        Assert.Equal(AddAdminControl.PrivacyBodyEn, privacy.BodyEn);
        Assert.True(privacy is { ShowInMenu: false, ShowInFooter: false, IsPublished: true });
        Assert.Equal(("سياسة الخصوصية", "Privacy Policy"), (privacy.MetaTitleAr, privacy.MetaTitleEn));
        Assert.StartsWith("كيف يتعامل موقع إنوفست هولدينغ", privacy.MetaDescriptionAr);
        Assert.StartsWith("How the Innovest Holding website handles", privacy.MetaDescriptionEn);
        Assert.Equal(1 + await db.Pages.Where(p => p.Key == "about" || p.Key == "ecosystem" || p.Key == "news" || p.Key == "contact").MaxAsync(p => p.SortOrder), privacy.SortOrder);
        var tv = await db.Pages.AsNoTracking().SingleAsync(p => p.Key == "tv");
        Assert.Equal(("مقدمة كتبها المشرف", "An intro the admin wrote"), (tv.IntroAr, tv.IntroEn)); // admin edit kept

        // A second run of the data steps changes nothing (every step is guarded).
        var allColumns = await ColumnsAsync(db);
        var after = await DumpAsync(db, allColumns);
        foreach (var sql in UpDataSql().Where(s => !s.Contains("ShowInFooter = ShowInMenu", StringComparison.Ordinal)))
            await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(after, await DumpAsync(db, allColumns));

        var afterWithoutIds = await DumpAsync(db, allColumns, withoutIdsAndTimes: true);

        // Down: the C4 schema and every C4 value come back (the admin's own hero_stats_limit stays).
        await migrator.MigrateAsync(Before);
        Assert.Equal(columns.Keys.Order(), (await ColumnsAsync(db)).Keys.Order());
        Assert.Equal(before, await DumpAsync(db, columns));

        // Up again reaches the same state (ids and timestamps of the re-inserted rows aside).
        await migrator.MigrateAsync(AdminControl);
        Assert.Equal(afterWithoutIds, await DumpAsync(db, allColumns, withoutIdsAndTimes: true));
    }

    [Fact] // T5.14 — the TV intro: only an EMPTY language is filled; Down clears only what Up wrote
    public async Task Tv_intro_is_filled_only_where_empty()
    {
        await using var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Before);

        async Task<(string Ar, string? En)> Intro()
        {
            var row = await db.Pages.AsNoTracking().Where(p => p.Key == "tv").Select(p => new { p.IntroAr, p.IntroEn }).SingleAsync();
            return (row.IntroAr, row.IntroEn);
        }

        // (initial Ar, initial En) → expected after Up
        var cases = new (string Ar, string? En, string ExpAr, string? ExpEn)[]
        {
            ("", null, AddAdminControl.TvIntroAr, AddAdminControl.TvIntroEn),                    // the C4 seed state
            ("   ", "  ", AddAdminControl.TvIntroAr, AddAdminControl.TvIntroEn),                  // blank counts as empty
            ("مقدمة المشرف", null, "مقدمة المشرف", AddAdminControl.TvIntroEn),                   // only En filled
            ("", "Admin intro", AddAdminControl.TvIntroAr, "Admin intro"),                        // only Ar filled
            ("مقدمة المشرف", "Admin intro", "مقدمة المشرف", "Admin intro"),                       // both kept
        };
        Assert.Equal(("", (string?)null), await Intro());
        foreach (var c in cases)
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE Pages SET IntroAr = {0}, IntroEn = {1} WHERE [Key] = 'tv'",
                new Microsoft.Data.SqlClient.SqlParameter("@p0", c.Ar),
                new Microsoft.Data.SqlClient.SqlParameter("@p1", System.Data.SqlDbType.NVarChar, 2000) { Value = (object?)c.En ?? DBNull.Value });
            await migrator.MigrateAsync(AdminControl);
            Assert.Equal((c.ExpAr, c.ExpEn), await Intro());
            foreach (var sql in UpDataSql()) await db.Database.ExecuteSqlRawAsync(sql);
            Assert.Equal((c.ExpAr, c.ExpEn), await Intro()); // second run: no-op
            await migrator.MigrateAsync(Before);
            var (ar, en) = await Intro();
            Assert.Equal(c.ExpAr == AddAdminControl.TvIntroAr ? "" : c.Ar, ar);
            Assert.Equal(c.ExpEn == AddAdminControl.TvIntroEn ? null : c.En, en);
        }
    }

    [Fact] // T5.3 — the frozen section rows of the migrations (AddAdminControl ∪ AddForumContent) are exactly the registry's
    public void Migration_section_rows_equal_the_registry()
    {
        // AddAdminControl wrote SortOrder 10, 20, …; AddForumContent (F10) interleaves its rows by their own SortOrder.
        var frozen = AddAdminControl.SectionRows
            .SelectMany(p => p.Sections.Select((s, i) => (p.Page, Section: s, Order: (i + 1) * 10)))
            .Concat(AddForumContent.SectionRows)
            .GroupBy(r => r.Page)
            .ToDictionary(g => g.Key, g => string.Join(",", g.OrderBy(r => r.Order).Select(r => r.Section)));
        var registry = PageSectionRegistry.Pages.ToDictionary(p => p.Key, p => string.Join(",", p.Value.Select(d => d.Key)));
        Assert.Equal(registry.Keys.Order(), frozen.Keys.Order());
        foreach (var (page, keys) in registry) Assert.True(keys == frozen[page], $"{page}: registry {keys} / migration {frozen[page]}");
        Assert.All(registry.Keys, k => Assert.True(SiteRoutes.IsKey(k), k));
        Assert.Equal(20, frozen.Count); // + forum.editions (F10)
    }

    [Fact] // T5.13 — MoveAsync: edges are no-ops, a move swaps and renumbers 1..n, foreign ids and bad directions are refused
    public async Task MoveAsync_swaps_within_the_scope_and_renumbers()
    {
        await using var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var mover = new Mover(db);
        IQueryable<PageSection> Home() => db.PageSections.Where(s => s.PageKey == "home");
        async Task<List<string>> Order() => await db.PageSections.AsNoTracking().Where(s => s.PageKey == "home")
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(s => s.SectionKey + ":" + s.SortOrder).ToListAsync();
        int Id(string page, string key) => db.PageSections.AsNoTracking().Single(s => s.PageKey == page && s.SectionKey == key).Id;

        var start = await Order();
        Assert.Equal("hero_stats:10", start[0]);

        var edge = await mover.Move(Home(), Id("home", "hero_stats"), -1);
        Assert.NotNull(edge);
        Assert.False(edge.Moved);
        Assert.Equal((1, 13), (edge.Position, edge.Count));
        Assert.Equal(start, await Order()); // nothing written

        var last = await mover.Move(Home(), Id("home", "cta_band"), 1);
        Assert.False(last!.Moved);
        Assert.Equal(start, await Order());

        var moved = await mover.Move(Home(), Id("home", "news"), -1);
        Assert.True(moved!.Moved);
        Assert.Equal(("news", 7, 13), (moved.Item.SectionKey, moved.Position, moved.Count));
        var order = await Order();
        Assert.Equal(["hero_stats:1", "about:2", "about_values:3", "ecosystem:4", "platforms:5", "stats:6", "news:7", "events:8",
            "partners:9", "faq:10", "register:11", "contact:12", "cta_band:13"], order);

        Assert.Null(await mover.Move(Home(), Id("about", "vision"), 1)); // another page's row → 404
        Assert.Null(await mover.Move(Home(), Id("home", "news"), 2));    // bad direction → 404
        Assert.Null(await mover.Move(Home(), Id("home", "news"), 0));
        Assert.Null(await mover.Move(Home(), -5, 1));
        Assert.Equal(order, await Order());
    }

    /// <summary>AdminControllerBase.MoveAsync with the request services resolving to the migration test context.</summary>
    private sealed class Mover : AdminControllerBase
    {
        public Mover(AppDbContext db)
        {
            var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } };
        }

        public Task<ViewModels.Admin.MoveResult<T>?> Move<T>(IQueryable<T> scope, int id, int direction) where T : class, ISortable =>
            MoveAsync(scope, id, direction);
    }
}

/// <summary>
/// C5 Dev 1 on the running app (its own fresh test database, migrated + seeded): T5.2 fresh == upgraded, T5.11 the
/// privacy page's SEO, T5.14 /tv indexable, T5.4 (sitemap/robots part) unpublished pages, and the Pages admin
/// (publication toggle, home dead fields, authorization both ways).
/// </summary>
[Collection("App")]
public class AdminControlSiteTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com";

    private static string? Robots(HttpResponseMessage r) =>
        r.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : null;

    private async Task<List<string>> SitemapAsync()
    {
        var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return xml.Root!.Elements(sm + "url").Select(u => u.Element(sm + "loc")!.Value).ToList();
    }

    private Task Set(string key, Action<Page> change) => f.DbAsync(async db =>
    {
        var row = await db.Pages.SingleAsync(p => p.Key == key);
        change(row);
        return await db.SaveChangesAsync();
    });

    [Fact] // T5.2 — a fresh install has exactly the rows an upgraded C4 install gets
    public async Task Fresh_install_equals_upgraded_install()
    {
        static async Task<List<string>> State(AppDbContext db)
        {
            var rows = new List<string>();
            rows.AddRange(await db.PageSections.AsNoTracking().OrderBy(s => s.PageKey).ThenBy(s => s.SortOrder)
                .Select(s => s.PageKey + "|" + s.SectionKey + "|" + s.IsVisible + "|" + s.SortOrder).ToListAsync());
            var newKeys = AddAdminControl.NewKeys.Select(k => k[0]).ToList();
            rows.AddRange(await db.SiteContents.AsNoTracking().Where(c => newKeys.Contains(c.SectionKey)).OrderBy(c => c.SectionKey)
                .Select(c => c.SectionKey + "|" + c.ContentAr + "|" + c.ContentEn).ToListAsync());
            rows.AddRange(await db.Pages.AsNoTracking().Where(p => p.Key == "privacy" || p.Key == "tv").OrderBy(p => p.Key)
                .Select(p => p.Key + "|" + p.TitleAr + "|" + p.TitleEn + "|" + p.IntroAr + "|" + p.IntroEn + "|" + p.BodyAr + "|" + p.BodyEn
                             + "|" + p.EmptyTextAr + "|" + p.EmptyTextEn + "|" + p.ShowInMenu + "|" + p.ShowInFooter + "|" + p.IsPublished).ToListAsync());
            rows.AddRange(await db.SocialLinks.AsNoTracking().OrderBy(s => s.Network).Select(s => s.Network + "|" + s.IsActive).ToListAsync());
            rows.Add("footer|" + await db.FooterColumns.CountAsync());
            return rows;
        }

        await using var upgraded = MigrationTests.NewContext();
        await upgraded.Database.EnsureDeletedAsync();
        await upgraded.GetService<IMigrator>().MigrateAsync("20261022221815_AddLeadForms");
        await upgraded.Database.MigrateAsync();

        var fresh = await f.DbAsync(State);
        Assert.True(fresh.Count >= 95, $"{fresh.Count} rows compared"); // 58 sections + 28 keys + 2 pages + 7 networks + footer
        var up = await State(upgraded);
        Assert.True(fresh.SequenceEqual(up), "fresh only: " + string.Join(" ‖ ", fresh.Except(up)) + " — upgraded only: " + string.Join(" ‖ ", up.Except(fresh)));
    }

    [Fact] // T5.11 (Dev 1 part) — the shipped policy is indexable and listed; emptied, it is thin and unlisted
    public async Task Privacy_page_ships_indexable_and_drops_out_when_emptied()
    {
        var r = await f.NewClient().GetAsync("/privacy");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Null(Robots(r));
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains(WebUtility.HtmlEncode("1. متى نجمع بياناتك"), html);
        Assert.Contains($"{Base}/privacy", await SitemapAsync());
        // RenameHoldingAndForum (N8) renames «إنوفست هولدينغ» in the shipped text.
        Assert.Contains(WebUtility.HtmlEncode(AddAdminControl.PrivacyRow.Single(c => c.Column == "MetaDescriptionAr").Value.Replace("هولدينغ", "القابضة")), html);
        // The row ships with an English meta pair too, so /en/privacy is indexable and listed.
        var en = await f.NewClient().GetAsync("/en/privacy");
        Assert.Equal(HttpStatusCode.OK, en.StatusCode);
        Assert.Null(Robots(en));
        var enHtml = await en.Content.ReadAsStringAsync();
        Assert.Contains("Privacy Policy", enHtml);
        Assert.Contains(WebUtility.HtmlEncode(AddAdminControl.PrivacyRow.Single(c => c.Column == "MetaDescriptionEn").Value), enHtml);
        Assert.Contains($"{Base}/en/privacy", await SitemapAsync());

        var stored = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == "privacy"));
        var (bodyAr, bodyEn) = (stored.BodyAr, stored.BodyEn);
        try
        {
            await Set("privacy", p => { p.BodyAr = ""; p.BodyEn = null; });
            var empty = await f.NewClient().GetAsync("/privacy");
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            Assert.Equal(RobotsHeader.FollowValue, Robots(empty));
            Assert.Equal(RobotsHeader.FollowValue, Robots(await f.NewClient().GetAsync("/en/privacy")));
            var sitemap = await SitemapAsync();
            Assert.DoesNotContain($"{Base}/privacy", sitemap);
            Assert.DoesNotContain($"{Base}/en/privacy", sitemap);
        }
        finally
        {
            await Set("privacy", p => { p.BodyAr = bodyAr; p.BodyEn = bodyEn; });
        }
    }

    [Fact] // T5.14 — the TV hub ships with its intro: indexable in both languages, in the sitemap with its pair
    public async Task Tv_hub_is_indexable_with_its_intro()
    {
        foreach (var path in new[] { "/tv", "/en/tv" })
        {
            var r = await f.NewClient().GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Null(Robots(r));
        }
        Assert.Contains("find its episodes, guests and sponsorship opportunities here", await f.NewClient().GetStringAsync("/en/tv"));
        var locs = await SitemapAsync();
        Assert.Contains($"{Base}/tv", locs);
        Assert.Contains($"{Base}/en/tv", locs);
    }

    [Fact] // T5.4 (Dev 1 part) — an unpublished page and everything under it leave the sitemap and are noindex,nofollow
    public async Task Unpublished_page_and_its_children_and_details_leave_the_sitemap()
    {
        var snapshot = await f.Services.GetRequiredService<ISiteContentCache>().GetAsync();
        Assert.All(SiteRoutes.All, p => Assert.True(snapshot.IsPublished(p.Key), p.Key));
        var before = await SitemapAsync();
        Assert.Contains($"{Base}/forum/events", before);
        Assert.Contains(before, l => l.StartsWith($"{Base}/forum/events/", StringComparison.Ordinal)); // an event detail

        try
        {
            await Set(PageKeys.Forum, p => p.IsPublished = false);
            var s = await f.Services.GetRequiredService<ISiteContentCache>().GetAsync();
            Assert.False(s.IsPublished(PageKeys.Forum));
            Assert.False(s.IsPublished(PageKeys.ForumEvents)); // the ancestor rule
            Assert.True(s.IsPublished(PageKeys.Tv));
            Assert.Equal(RobotsDecision.NoIndexNoFollow, SeoPolicy.Decide(SiteRoutes.Get(PageKeys.ForumEvents), false, s));
            var locs = await SitemapAsync();
            Assert.DoesNotContain(locs, l => l.Contains("/forum", StringComparison.Ordinal));
            Assert.Contains($"{Base}/tv", locs);
        }
        finally
        {
            await Set(PageKeys.Forum, p => p.IsPublished = true);
        }
        Assert.Equal(before, await SitemapAsync());
    }

    [Fact] // Pages admin: publication toggle both ways; home cannot be unpublished; anonymous / no token refused
    public async Task Pages_admin_toggles_publication_and_never_unpublishes_home()
    {
        var anonymous = f.NewClient();
        var a = await anonymous.PostAsync($"/Admin/Pages/TogglePublished/{PageKeys.TvGuests}", Form(("x", "1")));
        Assert.True(a.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.BadRequest, a.StatusCode.ToString());
        var token0 = await anonymous.GetTokenAsync("/Account/Login");
        var a2 = await anonymous.PostAsync($"/Admin/Pages/TogglePublished/{PageKeys.TvGuests}", Form(token0));
        Assert.Equal(HttpStatusCode.Redirect, a2.StatusCode);
        Assert.Contains("/Account/Login", a2.Headers.Location!.OriginalString);

        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/Admin/Pages/TogglePublished/{PageKeys.TvGuests}", Form(("x", "1")))).StatusCode);
        Assert.True(await f.DbAsync(db => db.Pages.Where(p => p.Key == PageKeys.TvGuests).Select(p => p.IsPublished).SingleAsync()));

        try
        {
            var token = await client.GetTokenAsync("/Admin/Pages");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Admin/Pages/TogglePublished/{PageKeys.TvGuests}", Form(token))).StatusCode);
            Assert.False(await f.DbAsync(db => db.Pages.Where(p => p.Key == PageKeys.TvGuests).Select(p => p.IsPublished).SingleAsync()));
            Assert.DoesNotContain($"{Base}/tv/guests", await SitemapAsync());
            var index = await client.GetStringAsync("/Admin/Pages");
            Assert.Contains("غير منشورة", index);

            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/Admin/Pages/TogglePublished/{PageKeys.Home}", Form(token))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Pages/TogglePublished/not-a-page", Form(token))).StatusCode);
        }
        finally
        {
            await Set(PageKeys.TvGuests, p => p.IsPublished = true);
        }
    }

    [Fact] // D11 — the home form has no dead fields, and saving it keeps the stored values; IsPublished posted false is ignored
    public async Task Home_edit_hides_dead_fields_and_keeps_their_values()
    {
        var client = await f.SignedInClientAsync();
        var form = await client.GetStringAsync("/Admin/Pages/Edit/home");
        foreach (var field in new[] { "SummaryAr", "IntroAr", "BodyAr", "EmptyTextAr", "EmptyActionPage", "IsPublished", "ShowInMenu", "ShowInFooter" })
            Assert.DoesNotContain($"name=\"{field}\"", form);
        Assert.Contains("name=\"TitleAr\"", form);
        Assert.Contains("name=\"MetaTitleAr\"", form);
        var other = await client.GetStringAsync("/Admin/Pages/Edit/about");
        foreach (var field in new[] { "IntroAr", "EmptyActionPage", "IsPublished", "ShowInMenu", "ShowInFooter" })
            Assert.Contains($"name=\"{field}\"", other);

        var home = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.Home));
        try
        {
            await Set(PageKeys.Home, p => { p.IntroAr = "مقدمة محفوظة"; p.BodyAr = "نص محفوظ"; });
            var token = await client.GetTokenAsync("/Admin/Pages/Edit/home");
            var r = await client.PostAsync("/Admin/Pages/Edit/home", Form(token, ("TitleAr", home.TitleAr), ("TitleEn", home.TitleEn ?? ""),
                ("IntroAr", "x"), ("BodyAr", ""), ("IsPublished", "false"), ("ShowInMenu", "false"), ("EmptyActionPage", "nope"),
                ("MetaTitleAr", home.MetaTitleAr), ("MetaDescriptionAr", home.MetaDescriptionAr),
                ("MetaTitleEn", home.MetaTitleEn ?? ""), ("MetaDescriptionEn", home.MetaDescriptionEn ?? "")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var after = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.Home));
            Assert.Equal(("مقدمة محفوظة", "نص محفوظ", true, true, (string?)null),
                (after.IntroAr, after.BodyAr, after.IsPublished, after.ShowInMenu, after.EmptyActionPage));
        }
        finally
        {
            await Set(PageKeys.Home, p => { p.IntroAr = home.IntroAr; p.BodyAr = home.BodyAr; });
        }
    }

    [Fact] // EmptyActionPage: a route key is stored, anything else is refused with a field error
    public async Task Empty_action_page_accepts_route_keys_only()
    {
        var client = await f.SignedInClientAsync();
        var page = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.TvGuests));
        (string, string)[] Fields(string target) =>
        [
            ("TitleAr", page.TitleAr), ("TitleEn", page.TitleEn ?? ""), ("SummaryAr", page.SummaryAr), ("SummaryEn", page.SummaryEn ?? ""),
            ("IntroAr", page.IntroAr), ("IntroEn", page.IntroEn ?? ""), ("BodyAr", page.BodyAr), ("BodyEn", page.BodyEn ?? ""),
            ("EmptyTextAr", page.EmptyTextAr), ("EmptyTextEn", page.EmptyTextEn ?? ""), ("MetaTitleAr", page.MetaTitleAr),
            ("MetaTitleEn", page.MetaTitleEn ?? ""), ("MetaDescriptionAr", page.MetaDescriptionAr), ("MetaDescriptionEn", page.MetaDescriptionEn ?? ""),
            ("ShowInMenu", page.ShowInMenu ? "true" : "false"), ("ShowInFooter", page.ShowInFooter ? "true" : "false"),
            ("IsPublished", "true"), ("EmptyActionPage", target),
        ];
        try
        {
            foreach (var bad in new[] { "not-a-page", "https://example.com", PageKeys.TvGuests })
            {
                var token = await client.GetTokenAsync("/Admin/Pages/Edit/tv.guests");
                var r = await client.PostAsync("/Admin/Pages/Edit/tv.guests", Form(token, Fields(bad)));
                Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with the error
                Assert.Contains("is-invalid", await r.Content.ReadAsStringAsync());
            }
            var t = await client.GetTokenAsync("/Admin/Pages/Edit/tv.guests");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Pages/Edit/tv.guests", Form(t, Fields(PageKeys.TvSponsorship)))).StatusCode);
            Assert.Equal(PageKeys.TvSponsorship, await f.DbAsync(db => db.Pages.Where(p => p.Key == PageKeys.TvGuests).Select(p => p.EmptyActionPage).SingleAsync()));
            var t2 = await client.GetTokenAsync("/Admin/Pages/Edit/tv.guests");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Pages/Edit/tv.guests", Form(t2, Fields("")))).StatusCode);
            Assert.Null(await f.DbAsync(db => db.Pages.Where(p => p.Key == PageKeys.TvGuests).Select(p => p.EmptyActionPage).SingleAsync()));
        }
        finally
        {
            await Set(PageKeys.TvGuests, p => { p.EmptyActionPage = null; p.UpdatedAt = page.UpdatedAt; });
        }
    }
}
