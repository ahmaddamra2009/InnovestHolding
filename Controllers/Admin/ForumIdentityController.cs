using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «هوية المنتدى»: the forum pages' identity (Infrastructure/Content/ForumIdentity) — logos, title-band image, share
/// image, colours and fonts. Images follow the Brand screen (SiteContents key → MediaFiles FileKey, slot rules, then the
/// shared MediaStore pipeline); colours and fonts are SiteContents keys. Nothing is written when any value is refused.
/// Messages are resx keys (MessageKeys.AdminForum); slot labels and hints stay with the slots (ForumIdentity), like Brand.
/// </summary>
public class ForumIdentityController : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/ForumIdentity/Index.cshtml";
    private readonly AppDbContext _db;

    public ForumIdentityController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(ViewPath, await PageAsync());

    private async Task<ForumIdentityPageVm> PageAsync(string? errorKey = null, string? error = null,
        ForumColorsInput? typed = null, IReadOnlyDictionary<string, string>? colorErrors = null, string? fontError = null,
        string? colorsError = null, IReadOnlyList<ContrastFailure>? failures = null)
    {
        var keys = ForumIdentity.Images.Select(s => s.Key).Concat(ForumIdentity.SettingKeys).ToList();
        var values = await _db.SiteContents.AsNoTracking().Where(c => keys.Contains(c.SectionKey))
            .ToDictionaryAsync(c => c.SectionKey, c => new TextPair(c.ContentAr, c.ContentEn));
        var fileKeys = ForumIdentity.Images.Select(s => values.GetValueOrDefault(s.Key)?.Ar).OfType<string>().Where(v => v.Length > 0).ToList();
        var files = fileKeys.Count == 0
            ? new Dictionary<string, MediaFile>()
            : await _db.MediaFiles.AsNoTracking().Where(m => fileKeys.Contains(m.FileKey))
                .ToDictionaryAsync(m => m.FileKey, StringComparer.Ordinal);

        var slots = ForumIdentity.Images.Select(slot =>
        {
            var m = values.GetValueOrDefault(slot.Key)?.Ar is { Length: > 0 } fk ? files.GetValueOrDefault(fk) : null;
            var input = new BrandImageInput { Key = slot.Key, Current = MediaRefs.Of(m), ImageAltAr = m?.AltAr, ImageAltEn = m?.AltEn };
            return new BrandSlotVm(slot, input, m?.Bytes, m is null ? null : BrandImages.Advice(slot, m.Width, m.Height, m.Bytes),
                slot.Key == errorKey ? error : null);
        }).ToList();

        var stored = ForumIdentity.Colors.ToDictionary(c => c.Key, c => ForumIdentity.StoredColor(values, c.Key), StringComparer.Ordinal);
        var effective = ForumIdentity.Effective(stored);
        var colors = ForumIdentity.Colors.Select(c => new ForumColorRowVm(c,
            typed is null ? stored[c.Key] : typed.Colors.GetValueOrDefault(c.Key)?.Trim(),
            effective[c.Key], colorErrors?.GetValueOrDefault(c.Key))).ToList();
        var contrast = ForumIdentity.Pairs.Select(p => new ForumContrastRowVm(p, effective[p.Foreground], effective[p.Background],
            ForumIdentity.Contrast(effective[p.Foreground], effective[p.Background]))).ToList();

        return new ForumIdentityPageVm
        {
            Slots = slots,
            Colors = colors,
            Contrast = contrast,
            FontAr = typed is null ? ForumIdentity.StoredFont(values, ForumIdentity.FontArKey)?.Key : typed.FontAr,
            FontEn = typed is null ? ForumIdentity.StoredFont(values, ForumIdentity.FontEnKey)?.Key : typed.FontEn,
            FontError = fontError,
            ColorsError = colorsError,
            Failures = failures ?? [],
        };
    }

    /// <summary>Upload, replace or remove one forum image, or update its alt texts. A refused file re-renders the screen
    /// with the reason under that slot (status 200) and writes nothing.</summary>
    [HttpPost]
    public async Task<IActionResult> Image(BrandImageInput input)
    {
        var slot = ForumIdentity.FindImage(input.Key);
        if (slot is null || !ModelState.IsValid)
        {
            TempData["Error"] = slot is null ? L(MessageKeys.InvalidValueNoField) : JoinErrors();
            return RedirectToAction(nameof(Index));
        }
        var (result, refusal) = await SaveSlotImageAsync(_db, slot, input);
        if (result == SlotSave.Refused) return View(ViewPath, await PageAsync(slot.Key, refusal));
        TempData["Success"] = result switch
        {
            SlotSave.Uploaded => L(MessageKeys.AdminForum.ImageSaved, slot.Label),
            SlotSave.Removed => L(MessageKeys.AdminForum.ImageRemoved, slot.Label),
            _ => L(MessageKeys.AdminForum.Saved),
        };
        return Redirect(Url.Action(nameof(Index))! + "#forum-" + slot.Key);
    }

    /// <summary>
    /// The colours and fonts, saved together. Each colour is #RRGGBB or empty (= the default); each font a key of the
    /// fixed list or empty. Then every contrast pair is measured with the colours as they will render: one pair below
    /// its minimum refuses the whole save (status 200, the failing pairs listed) and nothing is written.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Colors(ForumColorsInput input)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var c in ForumIdentity.Colors)
        {
            var raw = input.Colors.GetValueOrDefault(c.Key)?.Trim() ?? "";
            if (raw.Length == 0) values[c.Key] = null;
            else if (raw.Length == 7 && BrandImages.HexColor().IsMatch(raw)) values[c.Key] = raw.ToLowerInvariant();
            else errors[c.Key] = L(MessageKeys.AdminForum.ColorInvalid);
        }
        var fontAr = (input.FontAr ?? "").Trim();
        var fontEn = (input.FontEn ?? "").Trim();
        string? fontError = null;
        if ((fontAr.Length > 0 && ForumIdentity.FindFont(fontAr) is null) || (fontEn.Length > 0 && ForumIdentity.FindFont(fontEn) is null))
            fontError = L(MessageKeys.AdminForum.FontInvalid);

        if (!ModelState.IsValid || errors.Count > 0 || fontError is not null)
            return View(ViewPath, await PageAsync(typed: input, colorErrors: errors, fontError: fontError ?? (ModelState.IsValid ? null : JoinErrors()),
                colorsError: L(MessageKeys.AdminForum.FieldsRefused)));

        var failures = ForumIdentity.ContrastFailures(values);
        if (failures.Count > 0)
            return View(ViewPath, await PageAsync(typed: input, failures: failures,
                colorsError: L(MessageKeys.AdminForum.ContrastRefused)));

        var settings = new Dictionary<string, string?>(values, StringComparer.Ordinal)
        {
            [ForumIdentity.FontArKey] = fontAr.Length == 0 ? null : fontAr,
            [ForumIdentity.FontEnKey] = fontEn.Length == 0 ? null : fontEn,
        };
        var keys = settings.Keys.ToList();
        var rows = await _db.SiteContents.Where(c => keys.Contains(c.SectionKey)).ToDictionaryAsync(c => c.SectionKey, StringComparer.Ordinal);
        foreach (var (key, value) in settings)
        {
            var row = rows.GetValueOrDefault(key);
            if (value is null)
            {
                // Empty = the default: no row.
                if (row is not null) _db.SiteContents.Remove(row);
                continue;
            }
            if (row is null) _db.SiteContents.Add(new SiteContent { SectionKey = key, ContentAr = value, UpdatedAt = DateTime.UtcNow });
            else if (row.ContentAr != value)
            {
                row.ContentAr = value;
                row.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.AdminForum.ColorsSaved);
        return Redirect(Url.Action(nameof(Index))! + "#forum-colors");
    }

    /// <summary>«استعادة الافتراضي»: removes every forum colour and font (the pages go back to the default forum palette).
    /// Images are kept: each one has its own «حذف» on its form.</summary>
    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        var keys = ForumIdentity.SettingKeys.ToList();
        var rows = await _db.SiteContents.Where(c => keys.Contains(c.SectionKey)).ToListAsync();
        _db.SiteContents.RemoveRange(rows);
        await _db.SaveChangesAsync();
        TempData["Success"] = rows.Count == 0
            ? L(MessageKeys.AdminForum.ResetNothing)
            : L(MessageKeys.AdminForum.ResetDone, rows.Count);
        return Redirect(Url.Action(nameof(Index))! + "#forum-colors");
    }
}
