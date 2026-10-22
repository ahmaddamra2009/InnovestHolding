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

    [GeneratedRegex("^[0-9a-f]{32}$")]
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
    public async Task<MediaSaveResult> SaveAsync(IFormFile upload, string? altAr, string? altEn, CancellationToken ct)
    {
        if (upload.Length <= 0 || upload.Length > MaxBytes) return new(null, ImageProblem.Size);

        var byExtension = ImageInspector.FromExtension(upload.FileName);
        var byType = ImageInspector.FromContentType(upload.ContentType);
        if (byExtension is null || byType is null) return new(null, ImageProblem.Type);

        byte[] data;
        await using (var stream = upload.OpenReadStream())
        using (var buffer = new MemoryStream((int)upload.Length))
        {
            await stream.CopyToAsync(buffer, ct);
            if (buffer.Length > MaxBytes) return new(null, ImageProblem.Size);
            data = buffer.ToArray();
        }

        var sniffed = ImageInspector.Sniff(data);
        if (sniffed is null) return new(null, ImageProblem.Type);
        if (sniffed != byExtension || sniffed != byType) return new(null, ImageProblem.Mismatch);

        var problem = ImageInspector.Inspect(data, out var info);
        if (problem != ImageProblem.None) return new(null, problem);

        var clean = ImageInspector.Sanitize(data, info!.Format);
        // The rebuilt file must pass the same checks with the same result.
        if (ImageInspector.Inspect(clean, out var again) != ImageProblem.None || again != info)
            return new(null, ImageProblem.Corrupt);

        var key = Guid.NewGuid().ToString("N");
        var path = PathOf(key, info.Format);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.WriteAsync(clean, ct);
            await file.FlushAsync(ct);
        }
        File.Move(temp, path, overwrite: false);

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

    /// <summary>Deletes the files of rows that were removed (call after the commit). A failure is logged; the
    /// startup sweep removes the orphan later.</summary>
    public void DeleteFiles(IEnumerable<MediaFile?> files)
    {
        foreach (var file in files)
        {
            if (file is null || ImageInspector.FromName(file.Format) is not { } format) continue;
            try
            {
                File.Delete(PathOf(file.FileKey, format));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete media file {Key}; the startup sweep will remove it.", file.FileKey);
            }
        }
    }

    /// <summary>A file written for a save that then failed.</summary>
    public void Discard(MediaFile? file) => DeleteFiles([file]);

    /// <summary>
    /// Startup janitor: deletes files older than 24 h whose key has no row (a delete that failed, a save that died
    /// between the file and the row) and *.tmp files older than 1 h. It never deletes a file that has a row, and it
    /// ignores anything that is not named like a media file.
    /// </summary>
    public async Task<int> SweepAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!Directory.Exists(Root)) return 0;
        var known = (await db.MediaFiles.AsNoTracking().Select(m => m.FileKey).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var now = _time.GetUtcNow().UtcDateTime;
        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(path);
            var age = now - File.GetLastWriteTimeUtc(path);
            var isTemp = name.EndsWith(".tmp", StringComparison.Ordinal);
            var key = name.Split('.')[0];
            if (!KeyPattern().IsMatch(key)) continue;
            var orphan = isTemp ? age > TimeSpan.FromHours(1) : !known.Contains(key) && age > TimeSpan.FromHours(24);
            if (!orphan) continue;
            try
            {
                File.Delete(path);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete orphan media file {Name}.", name);
            }
        }
        return deleted;
    }
}
