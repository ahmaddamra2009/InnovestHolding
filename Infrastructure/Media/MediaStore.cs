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

    /// <summary>Marker in the uploads folder: a hash of the database the files belong to (no connection details).</summary>
    public const string DatabaseMarker = ".database";

    public sealed record SweepResult(int Moved, int Candidates, string Outcome);

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

        var identity = await db.Database.SqlQueryRaw<string>("SELECT CONCAT(@@SERVERNAME, N'/', DB_NAME()) AS [Value]").SingleAsync(ct);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
        var markerPath = Path.Combine(Root, DatabaseMarker);
        if (File.Exists(markerPath))
        {
            if (!string.Equals((await File.ReadAllTextAsync(markerPath, ct)).Trim(), hash, StringComparison.Ordinal))
            {
                _logger.LogError("The uploads folder belongs to another database than the one the app is connected to: " +
                                 "orphan sweep skipped, no file moved. Check the connection string.");
                return new(0, 0, "skipped: the uploads folder belongs to another database");
            }
        }
        else
        {
            await File.WriteAllTextAsync(markerPath, hash, ct);
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
