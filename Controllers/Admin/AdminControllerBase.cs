using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// Every admin controller derives from this: signed-in users with the Admin role only. Admin requests are always
/// Arabic (their URLs never start with /en), so the messages below come out of the neutral (Arabic) resx.
/// </summary>
[Authorize(Roles = "Admin")]
[RequestSizeLimit(InputLimits.AdminBodyBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = InputLimits.AdminBodyBytes)]
public abstract class AdminControllerBase : Controller
{
    public const int PageSize = 50;

    /// <summary>A resx message for the current request culture.</summary>
    protected string L(string key, params object[] args) =>
        HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>()[key, args].Value;

    /// <summary>All ModelState errors joined into one line, for the PRG list pages (TempData["Error"]).</summary>
    protected string JoinErrors() =>
        string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrEmpty(m)).Distinct());

    /// <summary>
    /// PRG on invalid input for the list pages: the joined ModelState errors go to TempData["Error"] and the browser
    /// is sent back to the controller's Index.
    /// </summary>
    protected IActionResult InvalidRedirect()
    {
        TempData["Error"] = JoinErrors();
        return RedirectToAction("Index");
    }

    /// <summary>Trimmed text, or null when empty/whitespace (optional English columns store NULL, not "").</summary>
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Plain-text body with LF line ends; null when empty.</summary>
    protected static string? Lf(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\r\n", "\n").Replace('\r', '\n');

    // ── Images (Infrastructure/Media/MediaStore) ──

    protected MediaStore Media => HttpContext.RequestServices.GetRequiredService<MediaStore>();

    private static string ProblemKey(ImageProblem p) => p switch
    {
        ImageProblem.Size => MessageKeys.ImageSize,
        ImageProblem.Mismatch => MessageKeys.ImageMismatch,
        ImageProblem.Corrupt => MessageKeys.ImageCorrupt,
        ImageProblem.Dimensions => MessageKeys.ImageDimensions,
        ImageProblem.Animated => MessageKeys.ImageAnimated,
        _ => MessageKeys.ImageType,
    };

    /// <summary>
    /// Validates and writes the chosen file (if any). On refusal nothing is written and the message goes to ModelState
    /// under "Image". Call only once the rest of the form is valid, so a refused form never leaves a file behind.
    /// </summary>
    protected async Task<MediaFile?> UploadImageAsync(ImageFieldsInput input)
    {
        if (!input.HasUpload) return null;
        try
        {
            var result = await Media.SaveAsync(input.Image!, input.ImageAltAr, input.ImageAltEn, HttpContext.RequestAborted);
            if (result.File is null) ModelState.AddModelError(nameof(ImageFieldsInput.Image), L(ProblemKey(result.Problem)));
            return result.File;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminControllerBase>>()
                .LogError(ex, "An image could not be written to the uploads folder.");
            ModelState.AddModelError(nameof(ImageFieldsInput.Image), L(MessageKeys.ImageStorage));
            return null;
        }
    }

    /// <summary>
    /// Puts the form's image decision on its owner (in the same SaveChanges): a new upload replaces the current image,
    /// "remove" clears it, otherwise the current image's alt texts are updated. Returns the row that leaves (its file
    /// is deleted by <see cref="SaveWithImageAsync"/> after the commit).
    /// </summary>
    protected static MediaFile? ApplyImage(AppDbContext db, MediaFile? current, MediaFile? uploaded, ImageFieldsInput input, Action<MediaFile?> set)
    {
        if (uploaded is not null)
        {
            db.MediaFiles.Add(uploaded);
            set(uploaded);
            if (current is not null) db.MediaFiles.Remove(current);
            return current;
        }
        if (input.RemoveImage && current is not null)
        {
            set(null);
            db.MediaFiles.Remove(current);
            return current;
        }
        if (current is not null)
        {
            current.AltAr = input.ImageAltAr?.Trim() ?? "";
            current.AltEn = Optional(input.ImageAltEn);
        }
        return null;
    }

    /// <summary>
    /// Saves; on failure the file written for this request is removed, on success the files of the rows that left are.
    /// </summary>
    protected async Task SaveWithImageAsync(AppDbContext db, MediaFile? uploaded, params MediaFile?[] leaving)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            Media.Discard(uploaded);
            throw;
        }
        Media.DeleteFiles(leaving);
    }

    // ── Slugs (detail pages) ──

    /// <summary>
    /// The slug to store, or null with a ModelState error. An empty field keeps <paramref name="current"/> (edit) or
    /// is made from the English title (create; "" when that leaves nothing — the caller then uses {type}-{id}). A
    /// typed slug must match the pattern and be free: not the current slug of another item, not an old slug of
    /// another item (old URLs keep redirecting, so they are never reused).
    /// </summary>
    protected async Task<string?> CheckSlugAsync(AppDbContext db, string entityType, string? requested, string? titleEn,
        int? selfId, string? current, Func<string, Task<bool>> usedByOther)
    {
        var typed = (requested ?? "").Trim().ToLowerInvariant();
        if (typed.Length == 0)
        {
            if (current is not null) return current;
            var baseSlug = Slugs.FromTitle(titleEn);
            if (baseSlug.Length == 0) return "";
            // Made from the title: a taken one gets -2, -3… (never an error the admin did not cause).
            for (var n = 1; n < 100; n++)
            {
                var candidate = n == 1 ? baseSlug : (baseSlug.Length > Slugs.MaxLength - 4 ? baseSlug[..(Slugs.MaxLength - 4)] : baseSlug) + "-" + n;
                if (await IsFreeAsync(candidate)) return candidate;
            }
            return "";
        }
        if (!Slugs.IsValid(typed))
        {
            ModelState.AddModelError("Slug", L(MessageKeys.SlugInvalid));
            return null;
        }
        if (typed != current && !await IsFreeAsync(typed))
        {
            ModelState.AddModelError("Slug", L(MessageKeys.SlugTaken));
            return null;
        }
        return typed;

        async Task<bool> IsFreeAsync(string slug) =>
            !await usedByOther(slug)
            && !await db.SlugRedirects.AnyAsync(r => r.EntityType == entityType && r.OldSlug == slug && r.EntityId != selfId);
    }

    /// <summary>
    /// Records a slug change: the old slug redirects to the item; going back to one of the item's own old slugs removes
    /// that redirect (the URL is the item's current address again).
    /// </summary>
    protected static async Task RecordSlugChangeAsync(AppDbContext db, string entityType, int id, string oldSlug, string newSlug)
    {
        if (oldSlug == newSlug) return;
        var own = await db.SlugRedirects.Where(r => r.EntityType == entityType && r.EntityId == id && r.OldSlug == newSlug).ToListAsync();
        db.SlugRedirects.RemoveRange(own);
        if (!await db.SlugRedirects.AnyAsync(r => r.EntityType == entityType && r.OldSlug == oldSlug))
            db.SlugRedirects.Add(new SlugRedirect { EntityType = entityType, OldSlug = oldSlug, EntityId = id, CreatedAt = DateTime.UtcNow });
    }

    /// <summary>The item's redirects leave with it (its old URLs then answer 404).</summary>
    protected static async Task RemoveSlugRedirectsAsync(AppDbContext db, string entityType, int id) =>
        db.SlugRedirects.RemoveRange(await db.SlugRedirects.Where(r => r.EntityType == entityType && r.EntityId == id).ToListAsync());
}
