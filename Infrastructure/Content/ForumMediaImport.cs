using System.Data.Common;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// F10 — one-time import of the bundled forum media (Data/SeedMedia/forum: the 31 edition photos and the 8 organisation
/// logos of the event profile) into the uploads folder, at startup, after the orphan sweep. A migration cannot copy
/// files; with this importer every bundled image becomes an ordinary MediaFile that every admin tool handles unchanged.
///
/// Rules (ARCHITECTURE-F10 §5):
/// <list type="bullet">
/// <item>Marker <see cref="Marker"/> present → nothing, for ever. It is written only after a run in which every item was
/// imported or skipped by rule (a file that fails inspection is skipped and logged; the admin can upload it by hand).</item>
/// <item>A logo goes only to a partner of that exact Arabic name (binary) whose LogoId is NULL; an edition's photos only to
/// an edition of that year and exact Arabic title that has NO photo. A row the admin deleted is never re-created, a
/// choice the admin made is never overwritten.</item>
/// <item>Files go through <see cref="MediaStore.SaveBytesAsync"/> (the upload checks, a new random key, CreateNew): no
/// existing file is ever opened for writing. Source paths are constants confined to the SeedMedia root.</item>
/// <item>One transaction per target (one partner, or one edition's ≤ 6 photos): a failed commit discards that chunk's
/// files at once; a crash leaves at most one chunk of orphans for the sweep.</item>
/// <item>Two instances (IIS overlapped recycle): a session app lock taken with timeout 0; not granted → skip.</item>
/// <item>An IO or database error is logged (file names only) and the site starts anyway.</item>
/// </list>
/// </summary>
public static class ForumMediaImport
{
    public const string Marker = "sys_forum_media_v1";
    public const string LockResource = "innovest_forum_media_import";

    public sealed record Photo(string File, string AltAr, string AltEn);

    /// <summary>The photos of the edition (Year, TitleAr) inserted by AddForumContent, in display order.</summary>
    public sealed record EditionMedia(short Year, string TitleAr, Photo[] Photos);

    /// <summary>The logo of the partner NameAr inserted by AddForumContent.</summary>
    public sealed record LogoMedia(string NameAr, string File, string AltAr, string AltEn);

    public sealed record Result(string Outcome, int Imported, int Skipped, int Failed);

    /// <summary>{ContentRoot}/Data/SeedMedia/forum (copied to the output and publish folders; never under wwwroot).</summary>
    public static string SourceRoot(string contentRoot) => Path.Combine(contentRoot, "Data", "SeedMedia", "forum");

    public static readonly LogoMedia[] Logos =
    [
        new("جامعة الدول العربية", "logos/arab-league.png", "شعار جامعة الدول العربية", "Arab League logo"),
        new("وزارة البيئة", "logos/ministry-environment.png", "شعار وزارة البيئة", "Ministry of Environment logo"),
        new("وزارة الشباب والرياضة", "logos/ministry-youth-sports.png", "شعار وزارة الشباب والرياضة", "Ministry of Youth and Sports logo"),
        new("وزارة الصناعة", "logos/ministry-industry.png", "شعار وزارة الصناعة", "Ministry of Industry logo"),
        new("وزارة التخطيط والتنمية الاقتصادية", "logos/ministry-planning-economic-development.png", "شعار وزارة التخطيط والتنمية الاقتصادية", "Ministry of Planning and Economic Development logo"),
        new("مؤسسة شباب قادرون للتنمية المستدامة", "logos/qadroon.png", "شعار مؤسسة شباب قادرون للتنمية المستدامة", "Qadroon Youth Foundation logo"),
        new("كابيتال للعلوم والتكنولوجيا", "logos/capital-science-technology.png", "شعار كابيتال للعلوم والتكنولوجيا", "CAPITAL for Science & Technology logo"),
        new("أكاديمية ألوندرا", "logos/alondra-academy.png", "شعار أكاديمية ألوندرا", "Alondra Academy logo"),
    ];

    public static readonly EditionMedia[] Editions =
    [
        new(2026, "منتدى إنوفست 2026",
        [
            new("photos/2026-forum-1.jpg", "صورة جماعية أمام مقر جامعة الدول العربية", "Group photo in front of the Arab League headquarters"),
            new("photos/2026-forum-2.jpg", "صورة جماعية على المسرح أمام شاشة إنوفست", "Group photo on stage at the Innovest screen"),
            new("photos/2026-forum-3.jpg", "جلسة في قاعة مجلس جامعة الدول العربية", "A session in the Arab League council hall"),
            new("photos/2026-forum-4.jpg", "قاعة المجلس الدائرية في جامعة الدول العربية", "The circular council hall at the Arab League"),
            new("photos/2026-forum-5.jpg", "جلسة حوارية على المسرح", "A panel session on stage"),
            new("photos/2026-forum-6.jpg", "الفائزون يحملون شيكات الجوائز", "Award winners holding their prize cheques"),
        ]),
        new(2025, "قمة الابتكار والتكنولوجيا للشباب العربي – الأردن 2025",
        [
            new("photos/2025-jordan-summit-1.jpg", "جلسة حوارية على المسرح في قمة الأردن", "A panel on stage at the Jordan summit"),
            new("photos/2025-jordan-summit-2.jpg", "صورة جماعية أمام خلفية القمة", "Group photo at the summit backdrop"),
            new("photos/2025-jordan-summit-3.jpg", "ضيوف حول طاولة مستديرة", "Guests at a round table"),
            new("photos/2025-jordan-summit-4.jpg", "القاعة بطاولاتها المستديرة", "The hall with its round tables"),
        ]),
        new(2025, "ملتقى Innovest Tech University",
        [
            new("photos/2025-tech-university-1.jpg", "صورة جماعية أمام مبنى الجامعة", "Group photo outside the university building"),
            new("photos/2025-tech-university-2.jpg", "مسؤولون أمام لافتة إنوفست", "Officials at the Innovest banner"),
            new("photos/2025-tech-university-3.jpg", "متحدثون على المسرح", "Speakers on stage"),
            new("photos/2025-tech-university-4.jpg", "الحضور في القاعة", "The audience in the auditorium"),
            new("photos/2025-tech-university-5.jpg", "مسؤولون يتجولون في المعرض", "Officials touring the exhibition"),
        ]),
        new(2025, "منتدى إنوفست 2025",
        [
            new("photos/2025-forum-1.jpg", "صورة جماعية مسائية أمام خلفية إنوفست", "Evening group photo at the Innovest backdrop"),
            new("photos/2025-forum-2.jpg", "صورة جماعية في مقر جامعة الدول العربية", "Group photo at the Arab League headquarters"),
            new("photos/2025-forum-3.jpg", "الفائزون في المسابقة يحملون لوحات الفوز", "Competition winners holding their winner boards"),
            new("photos/2025-forum-4.jpg", "مشاركون عند مدخل مقوّس", "Participants at an arched doorway"),
            new("photos/2025-forum-5.jpg", "اجتماع حول طاولة مستديرة", "A round-table meeting"),
            new("photos/2025-forum-6.jpg", "جلسة في الهواء الطلق تحت مظلة", "An outdoor session under a canopy"),
        ]),
        new(2024, "مؤتمر ريادة الأعمال والاستثمار العربي 2024",
        [
            new("photos/2024-conference-1.jpg", "صورة جماعية على المسرح", "Group photo on stage"),
            new("photos/2024-conference-2.jpg", "طاولة المتحدثين في المؤتمر الصحفي", "The speakers' table at the press conference"),
            new("photos/2024-conference-3.jpg", "الحضور حول الطاولات المستديرة", "The audience at round tables"),
            new("photos/2024-conference-4.jpg", "متحدث على المسرح", "A speaker on stage"),
            new("photos/2024-conference-5.jpg", "متحدث على منصة المؤتمر الصحفي", "A speaker at the press lectern"),
            new("photos/2024-conference-6.jpg", "مقدّم على المسرح أمام خلفية شباب قادرون", "A presenter on stage in front of the Qadroon backdrop"),
        ]),
        new(2023, "مؤتمر ريادة الأعمال والتحول الرقمي 2023",
        [
            new("photos/2023-conference-1.jpg", "لقاء مع أحد الوزراء", "A meeting with a minister"),
            new("photos/2023-conference-2.jpg", "صورة جماعية مع درع تكريمي", "Group photo with an honorary plaque"),
            new("photos/2023-conference-3.jpg", "الحضور أمام شاشة المسرح", "The audience in front of the stage screen"),
            new("photos/2023-conference-4.jpg", "الحضور في الصفوف الأمامية", "The audience in the front rows"),
        ]),
    ];

    private const string Bin = "Latin1_General_100_BIN2";

    /// <summary>A bundled file's full path, refused when it would leave <paramref name="root"/>.</summary>
    public static string PathOf(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var rootFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (!full.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Seed media path escapes its root.");
        return full;
    }

    /// <param name="beforeCommit">Tests only: called with the target's first file after its files are written and before
    /// its commit (to simulate a failure there). Null in the app.</param>
    public static async Task<Result> RunAsync(AppDbContext db, MediaStore media, string sourceRoot, ILogger logger,
        TimeProvider time, CancellationToken ct = default, Func<string, Task>? beforeCommit = null)
    {
        // Guard polarity: the marker makes this do LESS (nothing).
        if (await db.SiteContents.AnyAsync(c => c.SectionKey == Marker, ct))
            return new("done before", 0, 0, 0);

        var imported = 0;
        var skipped = 0;
        var failed = 0;
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        var locked = false;
        try
        {
            // Constant SQL, no data: an exclusive session lock, never waited for.
            locked = await AppLockAsync(connection, "EXEC @r = sp_getapplock @Resource = N'" + LockResource +
                                                    "', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;", ct) >= 0;
            if (!locked)
            {
                logger.LogInformation("Forum media import: skipped, another instance holds the import lock.");
                return new("locked", 0, 0, 0);
            }
            if (await db.SiteContents.AnyAsync(c => c.SectionKey == Marker, ct))
                return new("done before", 0, 0, 0);

            foreach (var logo in Logos)
            {
                var partner = await db.Partners
                    .Where(p => EF.Functions.Collate(p.NameAr, Bin) == logo.NameAr)
                    .OrderBy(p => p.Id).FirstOrDefaultAsync(ct);
                if (partner is null || partner.LogoId is not null) { skipped++; continue; }

                var file = await ReadAndStoreAsync(media, sourceRoot, logo.File, ImageUse.Logo, logo.AltAr, logo.AltEn, logger, ct);
                if (file is null) { failed++; continue; }
                await CommitAsync(db, media, [file], logo.File, beforeCommit, () =>
                {
                    db.MediaFiles.Add(file);
                    partner.Logo = file;
                    partner.UpdatedAt = time.GetUtcNow().UtcDateTime;
                }, ct);
                imported++;
            }

            foreach (var edition in Editions)
            {
                var row = await db.ForumEditions
                    .Where(e => e.Year == edition.Year && EF.Functions.Collate(e.TitleAr, Bin) == edition.TitleAr)
                    .OrderBy(e => e.Id).FirstOrDefaultAsync(ct);
                if (row is null || await db.ForumEditionPhotos.AnyAsync(p => p.ForumEditionId == row.Id, ct))
                {
                    skipped += edition.Photos.Length;
                    continue;
                }

                var files = new List<MediaFile>();
                foreach (var photo in edition.Photos)
                {
                    var file = await ReadAndStoreAsync(media, sourceRoot, photo.File, ImageUse.General, photo.AltAr, photo.AltEn, logger, ct);
                    if (file is null) failed++;
                    else files.Add(file);
                }
                if (files.Count == 0) continue;
                await CommitAsync(db, media, files, edition.Photos[0].File, beforeCommit, () =>
                {
                    var now = time.GetUtcNow().UtcDateTime;
                    for (var i = 0; i < files.Count; i++)
                    {
                        db.MediaFiles.Add(files[i]);
                        db.ForumEditionPhotos.Add(new ForumEditionPhoto { ForumEditionId = row.Id, Image = files[i], SortOrder = i + 1, CreatedAt = now });
                    }
                    row.UpdatedAt = now;
                }, ct);
                imported += files.Count;
            }

            db.SiteContents.Add(new SiteContent
            {
                SectionKey = Marker,
                ContentAr = $"imported {imported}, skipped {skipped}, failed {failed}",
                UpdatedAt = time.GetUtcNow().UtcDateTime,
            });
            await db.SaveChangesAsync(ct);
            // Once in the life of a database: a Warning, so the line survives Production's minimum log level (deploy note).
            logger.LogWarning("Forum media import: {Imported} imported, {Skipped} skipped by rule, {Failed} refused by inspection.",
                imported, skipped, failed);
            return new("imported", imported, skipped, failed);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbException or DbUpdateException)
        {
            // No connection string, no path beyond file names (the messages of these exceptions carry neither here).
            logger.LogError("Forum media import stopped ({Error}) after {Imported} imported; it runs again at the next start.",
                ex.GetType().Name, imported);
            return new("failed", imported, skipped, failed);
        }
        finally
        {
            db.ChangeTracker.Clear();
            if (locked)
            {
                try
                {
                    await AppLockAsync(connection, "EXEC @r = sp_releaseapplock @Resource = N'" + LockResource + "', @LockOwner = 'Session';", CancellationToken.None);
                }
                catch (DbException)
                {
                    // Closing the connection below releases a session lock anyway.
                }
            }
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<int> AppLockAsync(DbConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "DECLARE @r int; " + sql + " SELECT @r;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Reads a bundled file and stores it under a new key; null (logged as a Warning) when it is missing or refused.</summary>
    private static async Task<MediaFile?> ReadAndStoreAsync(MediaStore media, string root, string relative, ImageUse use,
        string altAr, string altEn, ILogger logger, CancellationToken ct)
    {
        var path = PathOf(root, relative);
        if (!File.Exists(path))
        {
            logger.LogWarning("Forum media import: bundled file {File} is missing; skipped.", Path.GetFileName(path));
            return null;
        }
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var saved = await media.SaveBytesAsync(bytes, Path.GetFileName(path), use, altAr, altEn, ct);
        if (saved.File is null)
            logger.LogWarning("Forum media import: bundled file {File} refused ({Problem}); skipped.", Path.GetFileName(path), saved.Problem);
        return saved.File;
    }

    /// <summary>One target's rows in one transaction; on any failure its files are discarded before the error goes up.</summary>
    private static async Task CommitAsync(AppDbContext db, MediaStore media, IReadOnlyList<MediaFile> files, string label,
        Func<string, Task>? beforeCommit, Action addRows, CancellationToken ct)
    {
        try
        {
            if (beforeCommit is not null) await beforeCommit(label);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            addRows();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            foreach (var f in files) media.Discard(f);
            throw;
        }
    }
}
