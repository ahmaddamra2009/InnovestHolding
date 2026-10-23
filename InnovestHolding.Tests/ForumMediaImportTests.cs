using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Tests;

/// <summary>
/// F10 T5 — the startup importer of the bundled forum media (Infrastructure/Content/ForumMediaImport), on the migration
/// test database with its own temp uploads folder per test, plus its run inside the app's startup (fresh test database).
/// </summary>
[Collection("App")]
public sealed class ForumMediaImportTests(TestAppFactory f) : IDisposable
{
    private readonly string _uploads = Path.Combine(Path.GetTempPath(), TestDb.Name("InnovestHolding_ForumImport") + "_" + Guid.NewGuid().ToString("N")[..8]);
    private readonly string _source = Path.Combine(Path.GetTempPath(), TestDb.Name("InnovestHolding_ForumSeed") + "_" + Guid.NewGuid().ToString("N")[..8]);
    private readonly ListLogger _log = new();

    public void Dispose()
    {
        foreach (var dir in new[] { _uploads, _source })
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private string BundledRoot => ForumMediaImport.SourceRoot(f.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath);

    private MediaStore Store() => new(Options.Create(new MediaOptions { RootPath = _uploads }),
        f.Services.GetRequiredService<IWebHostEnvironment>(), NullLogger<MediaStore>.Instance, TimeProvider.System);

    private static async Task<AppDbContext> FreshAsync()
    {
        var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        return db;
    }

    private Task<ForumMediaImport.Result> RunAsync(AppDbContext db, string? source = null, Func<string, Task>? beforeCommit = null) =>
        ForumMediaImport.RunAsync(db, Store(), source ?? BundledRoot, _log, TimeProvider.System, default, beforeCommit);

    private List<string> Files() => Directory.Exists(_uploads)
        ? Directory.EnumerateFiles(_uploads, "*", SearchOption.AllDirectories).Select(p => Path.GetFileName(p)).Order(StringComparer.Ordinal).ToList()
        : [];

    private static async Task<List<string>> RowFilesAsync(AppDbContext db) =>
        (await db.MediaFiles.AsNoTracking().ToListAsync())
        .Select(m => m.FileKey + "." + ImageInspector.Extension(ImageInspector.FromName(m.Format)!.Value)).Order(StringComparer.Ordinal).ToList();

    private static Task<List<int>> PhotoCountsAsync(AppDbContext db) =>
        db.ForumEditions.OrderBy(e => e.SortOrder).Select(e => e.Photos.Count).ToListAsync();

    private static int Total => ForumMediaImport.Logos.Length + ForumMediaImport.Editions.Sum(e => e.Photos.Length);

    [Fact] // the manifest: every target is a row AddForumContent inserts, every file exists and passes the upload checks
    public void Manifest_matches_the_migration_and_the_bundled_files()
    {
        Assert.Equal(39, Total);
        Assert.Equal(AddForumContent.Partners.Select(p => p.NameAr).Order(), ForumMediaImport.Logos.Select(l => l.NameAr).Order());
        Assert.Equal(AddForumContent.Editions.Select(e => (e.Year, e.TitleAr)), ForumMediaImport.Editions.Select(e => (e.Year, e.TitleAr)));
        Assert.True(ForumMediaImport.Editions.Max(e => e.Photos.Length) <= 6, "a chunk (one transaction) holds at most 6 files");
        var files = ForumMediaImport.Logos.Select(l => (l.File, Use: ImageUse.Logo, l.AltAr, l.AltEn))
            .Concat(ForumMediaImport.Editions.SelectMany(e => e.Photos).Select(p => (p.File, Use: ImageUse.General, p.AltAr, p.AltEn))).ToList();
        Assert.Equal(files.Count, files.Select(x => x.File).Distinct().Count());
        Assert.Equal(files.Count, Directory.EnumerateFiles(BundledRoot, "*", SearchOption.AllDirectories).Count());
        foreach (var (file, use, altAr, altEn) in files)
        {
            var bytes = File.ReadAllBytes(ForumMediaImport.PathOf(BundledRoot, file));
            Assert.Equal(ImageProblem.None, ImageInspector.Inspect(bytes, out var info));
            Assert.True(use.Allows(info!) && bytes.Length <= use.MaxBytes, file);
            Assert.True(use == ImageUse.Logo || Math.Max(info!.Width, info.Height) <= 1280, file + " is over 1280 px");
            Assert.True(altAr.Length is > 0 and <= 200 && altEn.Length is > 0 and <= 200, file);
        }
        Assert.Throws<InvalidOperationException>(() => ForumMediaImport.PathOf(BundledRoot, "../../Program.cs"));
    }

    [Fact] // first run imports 39 files as ordinary media; the marker makes the second a no-op (negative control included)
    public async Task Imports_once_and_the_marker_stops_later_runs()
    {
        await using var db = await FreshAsync();
        var first = await RunAsync(db);
        Assert.Equal(("imported", 39, 0, 0), (first.Outcome, first.Imported, first.Skipped, first.Failed));
        Assert.Equal([6, 4, 5, 6, 6, 4], await PhotoCountsAsync(db));
        Assert.Equal(8, await db.Partners.CountAsync(p => p.LogoId != null));
        var rows = await RowFilesAsync(db);
        Assert.Equal(39, rows.Count);
        Assert.Equal(rows, Files()); // every row has its file, no stray file
        foreach (var m in await db.MediaFiles.AsNoTracking().ToListAsync())
        {
            var bytes = await File.ReadAllBytesAsync(Store().PathOf(m.FileKey, ImageInspector.FromName(m.Format)!.Value));
            Assert.Equal(ImageProblem.None, ImageInspector.Inspect(bytes, out var info));
            Assert.Equal((m.Width, m.Height, m.Bytes), (info!.Width, info.Height, bytes.Length));
            Assert.NotEqual("", m.AltAr);
            Assert.False(string.IsNullOrEmpty(m.AltEn));
        }
        Assert.Contains(_log.Lines, l => l.StartsWith("Warning|Forum media import: 39 imported", StringComparison.Ordinal));
        Assert.True(await db.SiteContents.AnyAsync(c => c.SectionKey == ForumMediaImport.Marker));
        var cover = await db.ForumEditionPhotos.Where(p => p.ForumEdition!.Year == 2023).OrderBy(p => p.SortOrder).Select(p => p.Image!.AltEn).FirstAsync();
        Assert.Equal("A meeting with a minister", cover);

        // Second run: nothing.
        Assert.Equal("done before", (await RunAsync(db)).Outcome);
        Assert.Equal(rows, Files());
        Assert.Equal(rows, await RowFilesAsync(db));

        // Negative control: with a partner's logo removed, the marker alone still stops the run …
        var partner = await db.Partners.SingleAsync(p => p.NameAr == "أكاديمية ألوندرا");
        var oldLogo = partner.LogoId;
        partner.LogoId = null;
        await db.SaveChangesAsync();
        Assert.Equal("done before", (await RunAsync(db)).Outcome);
        Assert.Null(await db.Partners.Where(p => p.Id == partner.Id).Select(p => p.LogoId).SingleAsync());
        // … and without the marker the same run imports that logo again (and nothing else: the per-item rules).
        await db.Database.ExecuteSqlRawAsync("DELETE FROM SiteContents WHERE SectionKey = {0}", ForumMediaImport.Marker);
        var again = await RunAsync(db);
        Assert.Equal(("imported", 1, 38), (again.Outcome, again.Imported, again.Skipped));
        Assert.NotEqual(oldLogo, await db.Partners.Where(p => p.Id == partner.Id).Select(p => p.LogoId).SingleAsync());
        Assert.Equal(40, Files().Count); // a new file; the old one is never touched (it is the admin's to delete)
    }

    [Fact] // the admin's choices win: a set logo, an edition with a photo, a deleted partner or edition
    public async Task Never_overwrites_or_recreates()
    {
        await using var db = await FreshAsync();
        var adminImage = new MediaFile { FileKey = Guid.NewGuid().ToString("N"), Format = "png", Width = 100, Height = 100, Bytes = 10, AltAr = "شعار المشرف" };
        var edPhoto = new MediaFile { FileKey = Guid.NewGuid().ToString("N"), Format = "jpeg", Width = 100, Height = 100, Bytes = 10, AltAr = "صورة المشرف" };
        db.MediaFiles.AddRange(adminImage, edPhoto);
        await db.SaveChangesAsync();
        var arabLeague = await db.Partners.SingleAsync(p => p.NameAr == "جامعة الدول العربية");
        arabLeague.LogoId = adminImage.Id;
        db.Partners.Remove(await db.Partners.SingleAsync(p => p.NameAr == "وزارة البيئة"));
        var e2024 = await db.ForumEditions.SingleAsync(e => e.Year == 2024);
        db.ForumEditionPhotos.Add(new ForumEditionPhoto { ForumEditionId = e2024.Id, ImageId = edPhoto.Id, SortOrder = 1 });
        db.ForumEditions.Remove(await db.ForumEditions.SingleAsync(e => e.Year == 2023));
        await db.SaveChangesAsync();

        var r = await RunAsync(db);
        Assert.Equal(("imported", 39 - 2 - 6 - 4, 2 + 6 + 4), (r.Outcome, r.Imported, r.Skipped));
        Assert.Equal(adminImage.Id, await db.Partners.Where(p => p.NameAr == "جامعة الدول العربية").Select(p => p.LogoId).SingleAsync());
        Assert.False(await db.Partners.AnyAsync(p => p.NameAr == "وزارة البيئة"));
        Assert.False(await db.ForumEditions.AnyAsync(e => e.Year == 2023));
        Assert.Equal([edPhoto.Id], await db.ForumEditionPhotos.Where(p => p.ForumEditionId == e2024.Id).Select(p => p.ImageId).ToListAsync());
        Assert.Equal(27, Files().Count);
    }

    [Fact] // a missing or corrupt bundled file is skipped and logged; the marker is still written
    public async Task Missing_or_corrupt_file_is_skipped_and_logged()
    {
        foreach (var file in Directory.EnumerateFiles(BundledRoot, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_source, Path.GetRelativePath(BundledRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        File.Delete(Path.Combine(_source, "logos", "qadroon.png"));
        var corrupt = Path.Combine(_source, "photos", "2025-forum-3.jpg");
        var bytes = await File.ReadAllBytesAsync(corrupt);
        await File.WriteAllBytesAsync(corrupt, bytes[..(bytes.Length / 2)]);

        await using var db = await FreshAsync();
        var r = await RunAsync(db, _source);
        Assert.Equal(("imported", 37, 0, 2), (r.Outcome, r.Imported, r.Skipped, r.Failed));
        Assert.Contains(_log.Lines, l => l.StartsWith("Warning|", StringComparison.Ordinal) && l.Contains("qadroon.png is missing", StringComparison.Ordinal));
        Assert.Contains(_log.Lines, l => l.StartsWith("Warning|", StringComparison.Ordinal) && l.Contains("2025-forum-3.jpg refused", StringComparison.Ordinal));
        Assert.DoesNotContain(_log.Lines, l => l.Contains(_source, StringComparison.OrdinalIgnoreCase)); // file names only
        Assert.Null(await db.Partners.Where(p => p.NameAr == "مؤسسة شباب قادرون للتنمية المستدامة").Select(p => p.LogoId).SingleAsync());
        Assert.Equal([6, 4, 5, 5, 6, 4], await PhotoCountsAsync(db));
        Assert.True(await db.SiteContents.AnyAsync(c => c.SectionKey == ForumMediaImport.Marker));
        Assert.Equal(await RowFilesAsync(db), Files());
    }

    [Fact] // another instance holds the lock: skip, no marker, no file; once released, the import runs
    public async Task Held_lock_skips_without_marker()
    {
        await using var db = await FreshAsync();
        await using (var other = new SqlConnection(TestDb.ConnectionString(MigrationTests.Database)))
        {
            await other.OpenAsync();
            await using var take = other.CreateCommand();
            take.CommandText = "DECLARE @r int; EXEC @r = sp_getapplock @Resource = N'innovest_forum_media_import', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @r;";
            Assert.True((int)(await take.ExecuteScalarAsync())! >= 0);

            Assert.Equal("locked", (await RunAsync(db)).Outcome);
            Assert.False(await db.SiteContents.AnyAsync(c => c.SectionKey == ForumMediaImport.Marker));
            Assert.Empty(Files());
            Assert.Equal(0, await db.MediaFiles.CountAsync());
        }
        Assert.Equal(("imported", 39), await RunAsync(db) is var r ? (r.Outcome, r.Imported) : default);
    }

    [Fact] // a failure before a chunk's commit: that chunk's files are discarded, earlier chunks stay, no marker; a rerun completes
    public async Task Failure_mid_import_leaves_no_stray_and_a_rerun_completes()
    {
        await using var db = await FreshAsync();
        var failAt = ForumMediaImport.Editions[1].Photos[0].File;
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(db, beforeCommit: label =>
            label == failAt ? throw new InvalidOperationException("simulated crash") : Task.CompletedTask));
        Assert.False(await db.SiteContents.AnyAsync(c => c.SectionKey == ForumMediaImport.Marker));
        Assert.Equal(8 + 6, await db.MediaFiles.CountAsync()); // the logos and the first edition were committed
        Assert.Equal(await RowFilesAsync(db), Files());          // the failed chunk's files are gone (no stray)

        var r = await RunAsync(db);
        Assert.Equal(("imported", 39 - 14, 14), (r.Outcome, r.Imported, r.Skipped));
        Assert.Equal(39, await db.MediaFiles.CountAsync());
        Assert.Equal(await RowFilesAsync(db), Files());
    }

    [Fact] // the app's own startup ran the importer on the fresh test database: photos and logos are served from /media
    public async Task Startup_imported_the_media_into_the_app()
    {
        var (counts, logos, marker, key) = await f.DbAsync(async db => (
            await db.ForumEditions.Where(e => e.IsActive).OrderBy(e => e.SortOrder).Select(e => e.Photos.Count).ToListAsync(),
            await db.Partners.CountAsync(p => p.LogoId != null && p.ShowOnHome == false),
            await db.SiteContents.Where(c => c.SectionKey == ForumMediaImport.Marker).Select(c => c.ContentAr).SingleOrDefaultAsync(),
            await db.ForumEditionPhotos.OrderBy(p => p.Id).Select(p => p.Image!.FileKey).FirstAsync()));
        Assert.Equal([6, 4, 5, 6, 6, 4], counts);
        Assert.Equal(8, logos);
        Assert.StartsWith("imported 39", marker);
        var response = await f.NewClient().GetAsync(MediaStore.Url(key, "jpeg"));
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
    }

    private sealed class ListLogger : ILogger
    {
        public List<string> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(logLevel + "|" + formatter(state, exception));
    }
}
