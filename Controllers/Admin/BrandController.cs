using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «الهوية البصرية»: the favicon, the apple-touch icon, the default share image and the theme colour (G26–G28). Each
/// image is a SiteContents key holding the FileKey of a MediaFiles row (the SiteImages pattern of the Content editor,
/// no schema change), uploaded through the shared pipeline (MediaStore: whitelist, signature, structure, metadata
/// strip) after the slot's own rules (BrandImages.Check). A replacement stores a new random key, so its /media URL
/// changes; removing it brings back the bundled file.
/// </summary>
public class BrandController : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/Brand/Index.cshtml";
    private readonly AppDbContext _db;

    public BrandController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(ViewPath, await PageAsync());

    private async Task<BrandPageVm> PageAsync(string? errorKey = null, string? error = null,
        string? typedColor = null, string? colorError = null)
    {
        var keys = BrandImages.All.Select(s => s.Key).Append(BrandImages.ThemeColorKey).ToList();
        var values = await _db.SiteContents.AsNoTracking().Where(c => keys.Contains(c.SectionKey))
            .ToDictionaryAsync(c => c.SectionKey, c => c.ContentAr);
        var fileKeys = values.Values.Where(v => !string.IsNullOrEmpty(v)).ToList();
        var files = fileKeys.Count == 0
            ? new Dictionary<string, MediaFile>()
            : await _db.MediaFiles.AsNoTracking().Where(m => fileKeys.Contains(m.FileKey))
                .ToDictionaryAsync(m => m.FileKey, StringComparer.Ordinal);

        var slots = BrandImages.All.Select(slot =>
        {
            var m = values.GetValueOrDefault(slot.Key) is { Length: > 0 } fk ? files.GetValueOrDefault(fk) : null;
            var input = new BrandImageInput { Key = slot.Key, Current = MediaRefs.Of(m), ImageAltAr = m?.AltAr, ImageAltEn = m?.AltEn };
            return new BrandSlotVm(slot, input, m?.Bytes, m is null ? null : BrandImages.Advice(slot, m.Width, m.Height, m.Bytes),
                slot.Key == errorKey ? error : null);
        }).ToList();

        return new BrandPageVm
        {
            Slots = slots,
            ThemeColor = BrandImages.ThemeColor(values.GetValueOrDefault(BrandImages.ThemeColorKey)),
            ThemeColorTyped = typedColor,
            ThemeColorError = colorError,
        };
    }

    private async Task<MediaFile?> CurrentAsync(string key)
    {
        var fileKey = await _db.SiteContents.AsNoTracking().Where(c => c.SectionKey == key).Select(c => c.ContentAr).FirstOrDefaultAsync();
        return string.IsNullOrEmpty(fileKey) ? null : await _db.MediaFiles.FirstOrDefaultAsync(m => m.FileKey == fileKey);
    }

    /// <summary>
    /// Upload, replace or remove one brand image. A refused file re-renders the screen with the reason under that slot
    /// (status 200) and writes nothing.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Image(BrandImageInput input)
    {
        var slot = BrandImages.Find(input.Key);
        if (slot is null || !ModelState.IsValid)
        {
            TempData["Error"] = slot is null ? L(MessageKeys.InvalidValueNoField) : JoinErrors();
            return RedirectToAction(nameof(Index));
        }
        if (!slot.HasAlt) input.ImageAltAr = input.ImageAltEn = null;

        if (input.HasUpload)
        {
            var refusal = await CheckSlotAsync(slot, input.Image!);
            if (refusal is not null) return View(ViewPath, await PageAsync(slot.Key, refusal));
        }

        var current = await CurrentAsync(slot.Key);
        var uploaded = await UploadImageAsync(input, slot.Use);
        if (!ModelState.IsValid) return View(ViewPath, await PageAsync(slot.Key, JoinErrors()));

        var row = await _db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == slot.Key);
        if (row is null)
        {
            row = new SiteContent { SectionKey = slot.Key };
            _db.SiteContents.Add(row);
        }
        var leaving = ApplyImage(_db, current, uploaded, input, m => row.ContentAr = m?.FileKey ?? "");
        row.UpdatedAt = DateTime.UtcNow;
        // Nothing uploaded: no row (the bundled file is used).
        if (string.IsNullOrEmpty(row.ContentAr))
        {
            if (_db.Entry(row).State == EntityState.Added) _db.Entry(row).State = EntityState.Detached;
            else _db.SiteContents.Remove(row);
        }
        await SaveWithImageAsync(_db, uploaded, leaving);
        TempData["Success"] = uploaded is not null
            ? $"تم حفظ «{slot.Label}». قد يحتاج المتصفح وقتاً لإظهار الأيقونة الجديدة."
            : input.RemoveImage && current is not null
                ? $"عاد «{slot.Label}» إلى الملف الافتراضي المرفق."
                : L(MessageKeys.Admin.PhotoSaved);
        return Redirect(Url.Action(nameof(Index))! + "#brand-" + slot.Key);
    }

    /// <summary>
    /// The slot's rules on the uploaded bytes, read up to the slot's cap + 1 (an oversized file is refused on its length
    /// without being read). Null = acceptable; the shared pipeline then checks and stores it.
    /// </summary>
    private async Task<string?> CheckSlotAsync(BrandImage slot, IFormFile file)
    {
        if (file.Length > slot.MaxBytes)
            return BrandImages.Check(slot, new byte[slot.MaxBytes + 1], file.FileName, file.ContentType);
        await using var stream = file.OpenReadStream();
        var buffer = new byte[file.Length];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, HttpContext.RequestAborted);
        return BrandImages.Check(slot, buffer.AsSpan(0, read), file.FileName, file.ContentType);
    }

    /// <summary>The mobile browser bar colour: #RRGGBB, or empty for the default.</summary>
    [HttpPost]
    public async Task<IActionResult> ThemeColor(BrandThemeInput input)
    {
        var value = (input.ThemeColor ?? "").Trim();
        if (!ModelState.IsValid || (value.Length > 0 && !BrandImages.HexColor().IsMatch(value)))
            return View(ViewPath, await PageAsync(typedColor: value,
                colorError: "اللون غير صالح: اكتبه بالصيغة #RRGGBB (مثل #111145)، أو اتركه فارغاً للون الافتراضي."));

        // Empty = the default colour. The row is kept (a migration may have written it with the default); none is
        // created just to hold the default.
        var stored = value.Length == 0 ? BrandImages.DefaultThemeColor : value.ToLowerInvariant();
        var row = await _db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == BrandImages.ThemeColorKey);
        if (row is null && stored != BrandImages.DefaultThemeColor)
        {
            row = new SiteContent { SectionKey = BrandImages.ThemeColorKey };
            _db.SiteContents.Add(row);
        }
        if (row is not null && row.ContentAr != stored)
        {
            row.ContentAr = stored;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ لون المتصفح.";
        return Redirect(Url.Action(nameof(Index))! + "#brand-theme");
    }
}
