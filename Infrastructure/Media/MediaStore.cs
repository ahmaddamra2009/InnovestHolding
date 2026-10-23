using System.Text.RegularExpressions;
using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure.Media;

/// <summary>
/// Where uploads live. Default <c>{ContentRoot}/App_Data/uploads</c> (outside wwwroot, excluded from build and publish,
/// gitignored). The host may move it with the environment variable <c>Uploads__RootPath</c> — no appsettings entry.
/// </summary>
public sealed class MediaOptions
{
    public const string SectionName = "Uploads";

    public string? RootPath { get; set; }
}

/// <summary>Result of an upload: the new row (not yet saved), or the reason it was refused.</summary>
public sealed record MediaSaveResult(MediaFile? File, ImageProblem Problem);

/// <summary>
/// Validates, writes and deletes uploaded images (a focused infrastructure helper like the login throttle, not a
/// service layer). The path of a file is built ONLY from its random key and its validated format — no request value
/// ever reaches the file system — and is checked to stay under the root.
/// </summary>
public sealed partial class MediaStore
{
    /// <summary>Per-file cap. The admin body cap (8 MB) bounds the whole form.</summary>
    public const int MaxBytes = 5 * 1024 * 1024;

    public const string UrlPrefix = "/media/";

    private readonly ILogger<MediaStore> _logger;
    private readonly TimeProvider _time;

    public string Root { get; }

    public MediaStore(IOptions<MediaOptions> options, IWebHostEnvironment environment, ILogger<MediaStore> logger, TimeProvider time)
    {
        _logger = logger;
        _time = time;
        var configured = options.Value.RootPath;
        Root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "uploads")
            : configured));
    }

    [GeneratedRegex(@"^[0-9a-f]{32}\z")]
    public static partial Regex KeyPattern();

    /// <summary>The public URL of a stored image (Views never build it themselves).</summary>
    public static string Url(string fileKey, string format) =>
        UrlPrefix + fileKey + "." + ImageInspector.Extension(ImageInspector.FromName(format) ?? throw new ArgumentException("format"));

    /// <summary>The file's path: {root}/{key[0..2]}/{key}.{ext}. Throws for anything that is not a key/format pair.</summary>
    public string PathOf(string fileKey, ImageFormat format)
    {
        if (!KeyPattern().IsMatch(fileKey)) throw new ArgumentException("Invalid media key.", nameof(fileKey));
        var path = Path.GetFullPath(Path.Combine(Root, fileKey[..2], fileKey + "." + ImageInspector.Extension(format)));
        if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Media path escapes the root.");
        return path;
    }

    /// <summary>
    /// Checks the upload (size, extension, declared type, signature — all four must agree — then the whole container
    /// and the header dimensions), strips metadata and trailing bytes, and writes the file under a new random key.
    /// The returned row is not saved: the caller adds it in the same SaveChanges as its owner, and calls
    /// <see cref="Discard"/> if that save fails.
    /// </summary>
    public async Task<MediaSaveResult> SaveAsync(IFormFile upload, string? altAr, string? altEn, CancellationToken ct, ImageUse? use = null)
    {
        use ??= ImageUse.General;
        if (upload.Length <= 0 || upload.Length > use.MaxBytes) return new(null, ImageProblem.Size);

        var byExtension = ImageInspector.FromExtension(upload.FileName);
        var byType = ImageInspector.FromContentType(upload.ContentType);
        if (byExtension is null || byType is null) return new(null, ImageProblem.Type);

        byte[] data;
        await using (var stream = upload.OpenReadStream())
        using (var buffer = new MemoryStream((int)upload.Length))
        {
            await stream.CopyToAsync(buffer, ct);
            if (buffer.Length > use.MaxBytes) return new(null, ImageProblem.Size);
            data = buffer.ToArray();
        }

        var sniffed = ImageInspector.Sniff(data);
        if (sniffed is null) return new(null, ImageProblem.Type);
        if (sniffed != byExtension || sniffed != byType) return new(null, ImageProblem.Mismatch);

        var problem = ImageInspector.Inspect(data, out var info);
        if (problem != ImageProblem.None) return new(null, problem);
        if (!use.Allows(info!)) return new(null, ImageProblem.Dimensions);

        var clean = ImageInspector.Sanitize(data, info!.Format);
        // The rebuilt file must pass the same checks with the same result.
        if (ImageInspector.Inspect(clean, out var again) != ImageProblem.None || again != info)
            return new(null, ImageProblem.Corrupt);

        var key = Guid.NewGuid().ToString("N");
        var path = PathOf(key, info.Format);
        var temp = path + ".tmp";
        try
        {
            await WriteNewAsync(temp, path, clean, ct);
        }
        catch (DirectoryNotFoundException)
        {
            // A delete or the sweep removed the (empty) fan-out folder between our CreateDirectory and the write:
            // create it again and retry once.
            await WriteNewAsync(temp, path, clean, ct);
        }

        return new(new MediaFile
        {
            FileKey = key,
            Format = ImageInspector.Name(info.Format),
            Width = info.Width,
            Height = info.Height,
            Bytes = clean.Length,
            AltAr = (altAr ?? "").Trim(),
            AltEn = string.IsNullOrWhiteSpace(altEn) ? null : altEn.Trim(),
            CreatedAt = _time.GetUtcNow().UtcDateTime,
        }, ImageProblem.None);
    }

    private static async Task WriteNewAsync(string temp, string path, byte[] bytes, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.WriteAsync(bytes, ct);
            await file.FlushAsync(ct);
        }
        File.Move(temp, path, overwrite: false);
    }

    /// <summary>Deletes the files of rows that were removed (call after the commit). A failure is logged; the
    /// startup sweep removes the orphan later.</summary>
    public void DeleteFiles(IEnumerable<MediaFile?> files)
    {
        foreach (var file in files)
        {
            if (file is null || ImageInspector.FromName(file.Format) is not { } format) continue;
            try
            {
                var path = PathOf(file.FileKey, format);
                File.Delete(path);
                RemoveIfEmpty(Path.GetDirectoryName(path)!);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete media file {Key}; the startup sweep will quarantine it.", file.FileKey);
            }
        }
    }

    /// <summary>A file written for a save that then failed.</summary>
    public void Discard(MediaFile? file) => DeleteFiles([file]);

    /// <summary>Removes an empty fan-out folder ({root}/xx). Never the root itself, never a folder outside it.</summary>
    private void RemoveIfEmpty(string directory)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        if (!full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            if (Directory.Exists(full) && !Directory.EnumerateFileSystemEntries(full).Any()) Directory.Delete(full);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Another request wrote into it meanwhile, or the host refuses: an empty folder is harmless.
        }
    }

    /// <summary>Most files one sweep may move. More candidates than this means something is wrong (another database,
    /// a restore): the sweep then moves nothing and logs an error.</summary>
    public const int MaxSweepMoves = 50;

    /// <summary>Where orphans go: a sibling of the uploads folder ({root}-quarantine), outside what /media serves.</summary>
    public string QuarantineRoot => Root + "-quarantine";

    /// <summary>Marker in the uploads folder: the id of the database the files belong to (SiteContents
    /// <c>sys_media_instance</c>, a random GUID). It survives server renames and restarts; a restored backup carries it.</summary>
    public const string DatabaseMarker = ".database";

    public sealed record SweepResult(int Moved, int Candidates, string Outcome);

    private const string ContentInstanceLabel = "SiteContents sys_media_instance";

    /// <summary>The marker format of the first C2 text: SHA-256 of server/database name (64 upper-case hex).</summary>
    [GeneratedRegex(@"^[0-9A-F]{64}\z")]
    private static partial Regex LegacyMarker();

    /// <summary>
    /// Startup janitor. It NEVER deletes: candidates are moved to {root}-quarantine/{utc-date}/ and stay there until
    /// someone purges them by hand (deploy note). Candidates are files older than 24 h whose key has no MediaFiles row
    /// (a delete that failed, a save that died between file and row) and *.tmp files older than 1 h; anything not
    /// named like a media file is ignored. The whole sweep is skipped when:
    /// <list type="bullet">
    /// <item>the database holds no MediaFiles row (a new or wrong database would make every real upload an "orphan");</item>
    /// <item>the folder's marker names another database than the one the app is connected to;</item>
    /// <item>there are more than <see cref="MaxSweepMoves"/> candidates (logged as an error, nothing moved).</item>
    /// </list>
    /// </summary>
    public async Task<SweepResult> SweepAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!Directory.Exists(Root)) return new(0, 0, "no uploads folder");
        if (!await db.MediaFiles.AnyAsync(ct))
            return new(0, 0, "skipped: the database has no media rows");

        // The database's own id (not the server's name, which changes with LocalDB restarts or a host move).
        var instance = await db.SiteContents.Where(c => c.SectionKey == Content.ContentKeys.MediaInstance)
            .Select(c => c.ContentAr).FirstOrDefaultAsync(ct);
        if (instance is null || !KeyPattern().IsMatch(instance))
        {
            instance = Guid.NewGuid().ToString("N");
            var row = await db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == Content.ContentKeys.MediaInstance, ct);
            if (row is null) db.SiteContents.Add(new SiteContent { SectionKey = Content.ContentKeys.MediaInstance, ContentAr = instance, UpdatedAt = _time.GetUtcNow().UtcDateTime });
            else row.ContentAr = instance;
            await db.SaveChangesAsync(ct);
        }

        var markerPath = Path.Combine(Root, DatabaseMarker);
        var marker = File.Exists(markerPath) ? (await File.ReadAllTextAsync(markerPath, ct)).Trim() : null;
        if (marker is null || LegacyMarker().IsMatch(marker))
        {
            // First sweep of this folder (or the earlier server-name hash): the folder is now this database's.
            await File.WriteAllTextAsync(markerPath, instance, ct);
        }
        else if (!string.Equals(marker, instance, StringComparison.Ordinal))
        {
            _logger.LogError("Orphan sweep skipped, no file moved: the uploads folder's marker ({Marker}) names another database " +
                             "than the one the app is connected to ({Instance}). If the database was replaced on purpose, delete " +
                             "the file .database in the uploads folder; otherwise check the connection string.", DatabaseMarker, ContentInstanceLabel);
            return new(0, 0, "skipped: the uploads folder belongs to another database");
        }

        var known = (await db.MediaFiles.AsNoTracking().Select(m => m.FileKey).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var now = _time.GetUtcNow().UtcDateTime;
        var candidates = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(path);
            var key = name.Split('.')[0];
            if (!KeyPattern().IsMatch(key)) continue;
            var age = now - File.GetLastWriteTimeUtc(path);
            var orphan = name.EndsWith(".tmp", StringComparison.Ordinal)
                ? age > TimeSpan.FromHours(1)
                : !known.Contains(key) && age > TimeSpan.FromHours(24);
            if (orphan) candidates.Add(path);
        }
        if (candidates.Count > MaxSweepMoves)
        {
            _logger.LogError("Orphan sweep refused: {Candidates} files have no database row (limit {Limit}); nothing moved. " +
                             "The database may not match the uploads folder.", candidates.Count, MaxSweepMoves);
            return new(0, candidates.Count, "refused: too many candidates");
        }

        var target = Path.Combine(QuarantineRoot, now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        var moved = 0;
        foreach (var path in candidates)
        {
            try
            {
                Directory.CreateDirectory(target);
                var destination = Path.Combine(target, Path.GetFileName(path));
                if (File.Exists(destination)) destination += "." + Guid.NewGuid().ToString("N")[..8];
                File.Move(path, destination);
                RemoveIfEmpty(Path.GetDirectoryName(path)!);
                moved++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not move orphan media file {Name} to the quarantine.", Path.GetFileName(path));
            }
        }
        if (moved > 0)
            _logger.LogWarning("Orphan sweep: {Moved} of {Candidates} file(s) moved to the uploads quarantine ({Folder}).", moved, candidates.Count, target);
        return new(moved, candidates.Count, "swept");
    }
}
