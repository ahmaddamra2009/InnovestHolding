using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace InnovestHolding.Tests;

/// <summary>
/// AddBilingualContent on a database in the shape before the bilingual change (what production had before the bilingual release). Own LocalDB database
/// <see cref="Database"/>, recreated per test; the server and name are asserted before anything is dropped.
/// The old-shape rows are <see cref="PreBilingualSeed"/> — frozen literals of the seeder as it was before the change,
/// never taken from current code — so the migration is checked against what existing databases really contain.
/// </summary>
[Collection("App")]
public class MigrationTests(TestAppFactory f)
{
    public static readonly string Database = TestDb.Name("InnovestHolding_MigrationTests");
    private const string Initial = "20260601163804_InitialCreate";
    public const string Bilingual = "20261022120642_AddBilingualContent";
    private static readonly string Cs = TestDb.ConnectionString(Database);

    internal static AppDbContext NewContext()
    {
        var b = new SqlConnectionStringBuilder(Cs);
        if (!b.DataSource.StartsWith(@"(localdb)\", StringComparison.OrdinalIgnoreCase) || b.InitialCatalog != Database)
            throw new InvalidOperationException("Migration tests may only use the LocalDB migration test database.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Cs).Options);
    }

    internal static async Task<AppDbContext> AtInitialCreateAsync()
    {
        var db = NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.GetService<IMigrator>().MigrateAsync(Initial);
        return db;
    }

    private static string N(string? s) => s is null ? "NULL" : "N'" + s.Replace("'", "''") + "'";

    internal static Task<List<string>> Rows(AppDbContext db, string sql) => db.Database.SqlQueryRaw<string>(sql).ToListAsync();

    /// <summary>Every entity row (Arabic, English and new fields) as text, ordered by id — the comparison unit of the fresh-vs-upgraded test.</summary>
    internal static async Task<List<string>> SnapshotAsync(AppDbContext db, int maxNav = 8)
    {
        var all = new List<string>();
        // NavItems are not compared: since the multi-page site the menu comes from the route table; the one-page anchor
        // items exist (deactivated) only on upgraded databases, and other tests add and remove items on the fixture.
        all.AddRange((await db.Platforms.AsNoTracking().OrderBy(n => n.Id).ToListAsync()).Select(p => $"Pl|{p.TitleAr}|{p.TitleEn}|{p.DescriptionAr}|{p.DescriptionEn}|{p.ShortTitleAr}|{p.ShortTitleEn}|{p.ShowInFooter}|{p.LinkUrl}|{p.IsActive}|{p.IconSvg}"));
        all.AddRange((await db.Stats.AsNoTracking().OrderBy(n => n.Id).ToListAsync()).Select(s => $"St|{s.LabelAr}|{s.LabelEn}|{s.ShortLabelAr}|{s.ShortLabelEn}|{s.Value}|{s.Suffix}|{s.SortOrder}|{s.IconSvg}"));
        all.AddRange((await db.News.AsNoTracking().Where(n => n.Id <= 3).OrderBy(n => n.Id).ToListAsync()).Select(n => $"Nw|{n.TitleAr}|{n.TitleEn}|{n.SummaryAr}|{n.SummaryEn}|{n.CategoryAr}|{n.CategoryEn}|{n.NewsDateAr}|{n.NewsDateEn}|{n.SortOrder}"));
        all.AddRange((await db.Events.AsNoTracking().Where(n => n.Id <= 3).OrderBy(n => n.Id).ToListAsync()).Select(e => $"Ev|{e.TitleAr}|{e.TitleEn}|{e.DescriptionAr}|{e.DescriptionEn}|{e.EventMonthAr}|{e.EventMonthEn}|{e.LocationAr}|{e.LocationEn}|{e.TimeAr}|{e.TimeEn}"));
        all.AddRange((await db.Partners.AsNoTracking().Where(n => n.Id <= 10).OrderBy(n => n.Id).ToListAsync()).Select(p => $"Pa|{p.NameAr}|{p.NameEn}|{p.IsActive}"));
        all.AddRange((await db.Faqs.AsNoTracking().Where(n => n.Id <= 6).OrderBy(n => n.Id).ToListAsync()).Select(q => $"Fq|{q.QuestionAr}|{q.QuestionEn}|{q.AnswerAr}|{q.AnswerEn}"));
        all.AddRange((await db.SiteContents.AsNoTracking().Where(c => c.SectionKey != ContentKeys.TimestampsUtcMarker).OrderBy(c => c.SectionKey).ToListAsync())
            .Select(c => $"Sc|{c.SectionKey}|{c.ContentAr}|{c.ContentEn}"));
        all.AddRange((await db.InterestOptions.AsNoTracking().Where(o => o.Id <= 6).OrderBy(o => o.Id).ToListAsync()).Select(o => $"Io|{o.NameAr}|{o.NameEn}|{o.SortOrder}"));
        all.AddRange((await db.RegisterBenefits.AsNoTracking().OrderBy(o => o.Id).ToListAsync()).Select(o => $"Rb|{o.TextAr}|{o.TextEn}"));
        all.AddRange((await db.SocialLinks.AsNoTracking().OrderBy(o => o.Id).ToListAsync()).Select(o => $"So|{o.Network}|{o.Url}"));
        all.AddRange((await db.Pages.AsNoTracking().OrderBy(o => o.Key).ToListAsync()).Select(p =>
            $"Pg|{p.Key}|{p.TitleAr}|{p.TitleEn}|{p.SummaryAr}|{p.SummaryEn}|{p.IntroAr}|{p.IntroEn}|{p.BodyAr}|{p.BodyEn}|{p.EmptyTextAr}|{p.EmptyTextEn}|{p.MetaTitleAr}|{p.MetaTitleEn}|{p.MetaDescriptionAr}|{p.MetaDescriptionEn}|{p.ShowInMenu}|{p.SortOrder}"));
        all.AddRange((await db.ContentItems.AsNoTracking().OrderBy(o => o.Id).ToListAsync()).Select(i =>
            $"Ci|{i.Group}|{i.TextAr}|{i.TextEn}|{i.DescriptionAr}|{i.DescriptionEn}|{i.IconKey}|{i.SortOrder}|{i.IsActive}"));
        return all;
    }

    [Fact] // the data survives the renames; English and keys are added without touching edits
    public async Task Upgrade_keeps_arabic_data_adds_english_and_never_overwrites_or_duplicates()
    {
        await using var db = await AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        // An admin-edited Arabic row, an admin-created key that the migration also inserts, two registrations.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Faqs SET Question = N'سؤال عدّله المدير' WHERE Id = 2;
            INSERT INTO SiteContents (SectionKey, ContentAr, UpdatedAt) VALUES (N'cta_register', N'انضم الآن', SYSUTCDATETIME());
            INSERT INTO Registrations (FullName, Email, Phone, Interest, IsRead, CreatedAt) VALUES (N'QA', N'a@example.com', N'', N'المسابقات', 0, '2026-07-01T12:00:00');
            INSERT INTO ContactMessages (FullName, Email, Subject, Message, IsRead, CreatedAt) VALUES (N'QA', N'a@example.com', N's', N'm', 1, '2026-01-15T12:00:00');
            """);
        var arabicBefore = await Rows(db, """
            SELECT CONCAT(N'Nav|', Title) AS [Value] FROM NavItems UNION ALL SELECT CONCAT(N'Sub|', Title) FROM NavSubItems
            UNION ALL SELECT CONCAT(N'Pl|', Title, N'|', Description) FROM Platforms UNION ALL SELECT CONCAT(N'St|', Label) FROM Stats
            UNION ALL SELECT CONCAT(N'Nw|', Title, N'|', Summary, N'|', Category, N'|', NewsDate) FROM News
            UNION ALL SELECT CONCAT(N'Ev|', Title, N'|', Description, N'|', EventMonth, N'|', Location, N'|', Time) FROM Events
            UNION ALL SELECT CONCAT(N'Pa|', Name) FROM Partners UNION ALL SELECT CONCAT(N'Fq|', Question, N'|', Answer) FROM Faqs
            """);

        await db.GetService<IMigrator>().MigrateAsync(Bilingual);

        var arabicAfter = await Rows(db, """
            SELECT CONCAT(N'Nav|', TitleAr) AS [Value] FROM NavItems UNION ALL SELECT CONCAT(N'Sub|', TitleAr) FROM NavSubItems
            UNION ALL SELECT CONCAT(N'Pl|', TitleAr, N'|', DescriptionAr) FROM Platforms UNION ALL SELECT CONCAT(N'St|', LabelAr) FROM Stats
            UNION ALL SELECT CONCAT(N'Nw|', TitleAr, N'|', SummaryAr, N'|', CategoryAr, N'|', NewsDateAr) FROM News
            UNION ALL SELECT CONCAT(N'Ev|', TitleAr, N'|', DescriptionAr, N'|', EventMonthAr, N'|', LocationAr, N'|', TimeAr) FROM Events
            UNION ALL SELECT CONCAT(N'Pa|', NameAr) FROM Partners UNION ALL SELECT CONCAT(N'Fq|', QuestionAr, N'|', AnswerAr) FROM Faqs
            """);
        Assert.True(arabicBefore.Count >= 46, $"{arabicBefore.Count} rows compared");
        Assert.Equal(arabicBefore.OrderBy(x => x, StringComparer.Ordinal), arabicAfter.OrderBy(x => x, StringComparer.Ordinal));

        // English only where the Arabic is still the seed text.
        var faq2 = await db.Faqs.AsNoTracking().SingleAsync(q => q.Id == 2);
        Assert.Null(faq2.QuestionEn);
        Assert.NotNull(faq2.AnswerEn);
        Assert.Equal("What is Innovest Holding?", (await db.Faqs.AsNoTracking().SingleAsync(q => q.Id == 1)).QuestionEn);

        // Keys: none duplicated, none overwritten, all registry keys present, hero keys renamed.
        var keys = await db.SiteContents.AsNoTracking().ToListAsync();
        Assert.Equal(keys.Count, keys.Select(k => k.SectionKey).Distinct().Count());
        // Every key of the registry except those the later migration ApplyCompanyProfile adds.
        string[] later = ["brand_positioning", "header_cta_label", "header_cta_page", "contact_whatsapp", "contact_website",
            "list_page_size", "ecosystem_badge", "ecosystem_title", "ecosystem_subtitle", "cta_band_title", "cta_band_text",
            "impact_title", "impact_intro", "default_utc_offset"];
        Assert.Empty(ContentKeys.All.Select(k => k.Key)
            .Where(k => !later.Contains(k) && !k.StartsWith("section_") && !k.StartsWith("founder_"))
            .Except(keys.Select(k => k.SectionKey)));
        var cta = keys.Single(k => k.SectionKey == "cta_register");
        Assert.Equal("انضم الآن", cta.ContentAr);
        Assert.Null(cta.ContentEn);
        Assert.DoesNotContain(keys, k => k.SectionKey is "hero_title_ar" or "hero_title_en");
        Assert.Equal(("إنوفست هولدينغ", "INNOVEST HOLDING"), keys.Where(k => k.SectionKey == "hero_title").Select(k => (k.ContentAr, k.ContentEn)).Single());
        Assert.Equal(("INNOVEST HOLDING", (string?)null), keys.Where(k => k.SectionKey == "hero_kicker").Select(k => (k.ContentAr, k.ContentEn)).Single());
        var address = keys.Single(k => k.SectionKey == "contact_address");
        Assert.Equal(("Salah Salem St, Heliopolis, Cairo", "Salah Salem St, Heliopolis, Cairo"), (address.ContentAr, address.ContentEn));
        Assert.Equal("https://www.innovest-holding.com", keys.Single(k => k.SectionKey == "site_base_url").ContentAr);

        // New columns and tables.
        Assert.Equal([1, 2, 3], await db.News.OrderBy(n => n.Id).Select(n => n.SortOrder).ToListAsync());
        Assert.Equal(6, await db.InterestOptions.CountAsync());
        Assert.Equal(4, await db.RegisterBenefits.CountAsync());
        Assert.Equal(4, await db.SocialLinks.CountAsync(s => s.Url == null));
        Assert.Equal("ar", await db.Registrations.Select(r => r.Culture).SingleAsync());
        Assert.Equal("ar", await db.ContactMessages.Select(r => r.Culture).SingleAsync());
        Assert.Null(await db.Registrations.Select(r => r.InterestOptionId).SingleAsync());
        Assert.Equal(5, await db.NavItems.CountAsync(n => n.ShowInFooter));
        Assert.False(await db.Platforms.Where(p => p.TitleAr == "الفعاليات والمعارض").Select(p => p.ShowInFooter).SingleAsync());

        // Registration/contact timestamps are NOT changed by the migration (startup converts them — next test).
        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0), await db.Registrations.Select(r => r.CreatedAt).SingleAsync());

        // Only the later migrations (multi-page site) are still pending.
        Assert.Equal(["20261022152940_AddSiteStructure", "20261022153329_ApplyCompanyProfile", "20261022172550_RefineSiteStructure",
            "20261022200000_CorrectCoreValuesEnglish", "C2_MIGRATION_ID"], await db.Database.GetPendingMigrationsAsync());
    }

    [Fact] // a fresh install (migration + DbSeeder) equals an upgraded old install, row by row
    public async Task Fresh_and_upgraded_databases_have_the_same_content()
    {
        await using var db = await AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        await db.Database.MigrateAsync();

        var upgraded = await SnapshotAsync(db);
        var fresh = await f.DbAsync(fdb => SnapshotAsync(fdb));
        Assert.True(fresh.Count > 100, $"{fresh.Count} rows compared");
        Assert.Equal(fresh, upgraded);
    }

    [Fact] // Down then Up again
    public async Task Down_restores_the_old_columns_with_their_data_and_up_again_reaches_the_same_state()
    {
        await using var db = await AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync(PreBilingualSeed.Sql);
        await db.Database.MigrateAsync();
        var first = await SnapshotAsync(db);

        await db.GetService<IMigrator>().MigrateAsync(Initial);
        var old = await Rows(db, "SELECT CONCAT(Title, N'|', Description) AS [Value] FROM Platforms ORDER BY Id");
        Assert.Equal(6, old.Count);
        Assert.StartsWith("التدريب والتعليم|", old[0]);
        Assert.Equal(16, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM SiteContents").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM SiteContents WHERE SectionKey = N'hero_title_ar'").SingleAsync());

        await db.Database.MigrateAsync();
        Assert.Equal(first, await SnapshotAsync(db));
    }

    [Fact] // startup conversion (it must run on the server that wrote the local times), idempotent
    public async Task Timestamp_conversion_runs_once_with_each_rows_own_offset()
    {
        await using var db = await AtInitialCreateAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Registrations (FullName, Email, Phone, Interest, IsRead, CreatedAt) VALUES (N'QA', N'a@example.com', N'', N'x', 0, '2026-07-01T12:00:00');
            INSERT INTO ContactMessages (FullName, Email, Subject, Message, IsRead, CreatedAt) VALUES (N'QA', N'a@example.com', N's', N'm', 0, '2026-01-15T12:00:00');
            """);
        await db.Database.MigrateAsync();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

        Assert.Equal(2, await TimestampUtcConversion.RunAsync(db, zone, NullLogger.Instance));
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0), await db.Registrations.Select(r => r.CreatedAt).SingleAsync());   // CEST +2
        Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0), await db.ContactMessages.Select(r => r.CreatedAt).SingleAsync()); // CET +1

        Assert.Equal(0, await TimestampUtcConversion.RunAsync(db, zone, NullLogger.Instance)); // second start: nothing
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0), await db.Registrations.Select(r => r.CreatedAt).SingleAsync());
        Assert.Equal(1, await db.SiteContents.CountAsync(c => c.SectionKey == ContentKeys.TimestampsUtcMarker));
    }

    [Fact]
    public async Task The_model_and_the_migrated_schema_agree()
    {
        // EnsureCreated builds the schema straight from the model; Migrate builds it from the migrations.
        var modelDb = TestDb.Name("InnovestHolding_MigrationTests_Model");
        var modelCs = TestDb.ConnectionString(modelDb);
        await using var model = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(modelCs).Options);
        await model.Database.EnsureDeletedAsync();
        await model.Database.EnsureCreatedAsync();
        await using var migrated = NewContext();
        await migrated.Database.EnsureDeletedAsync();
        await migrated.Database.MigrateAsync();

        const string q = """
            SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', DATA_TYPE, '(', CHARACTER_MAXIMUM_LENGTH, ') ', IS_NULLABLE) AS [Value]
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME <> '__EFMigrationsHistory' ORDER BY TABLE_NAME, COLUMN_NAME
            """;
        var a = await Rows(model, q);
        var b = await Rows(migrated, q);
        Assert.True(a.Count > 100, $"{a.Count} columns");
        Assert.Equal(a, b);
        await model.Database.EnsureDeletedAsync();
    }
}
