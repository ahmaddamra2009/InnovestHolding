using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>The SiteContents editor, generated from the key registry (ContentKeys): one Ar/En pair per key.</summary>
public class ContentController : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/Content/Edit.cshtml";
    private readonly AppDbContext _db;

    public ContentController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Edit()
    {
        var rows = await _db.SiteContents.AsNoTracking()
            .ToDictionaryAsync(c => c.SectionKey, c => new BilingualValueInput { Ar = c.ContentAr, En = c.ContentEn });
        var input = new ContentEditInput();
        foreach (var key in ContentKeys.All)
            input.Content[key.Key] = rows.GetValueOrDefault(key.Key) ?? new BilingualValueInput();
        await SetPageChoicesAsync();
        return View(ViewPath, input);
    }

    public const string FounderPhotoKey = "FounderPhoto";

    private async Task<MediaFile?> FounderPhotoRowAsync(bool tracked)
    {
        var key = await _db.SiteContents.AsNoTracking().Where(c => c.SectionKey == ContentKeys.FounderPhoto)
            .Select(c => c.ContentAr).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(key)) return null;
        var query = tracked ? _db.MediaFiles : _db.MediaFiles.AsNoTracking();
        return await query.FirstOrDefaultAsync(m => m.FileKey == key);
    }

    private async Task SetFounderPhotoAsync()
    {
        var m = await FounderPhotoRowAsync(false);
        ViewData[FounderPhotoKey] = new FounderPhotoInput { Current = MediaRefs.Of(m), ImageAltAr = m?.AltAr, ImageAltEn = m?.AltEn };
    }

    /// <summary>
    /// The founder block's photo (its own small form: a file cannot be part of the big text form without making every
    /// text save a multipart upload). The key founder_photo holds the image's FileKey; a replaced or removed image is
    /// deleted with its file.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> FounderPhoto(FounderPhotoInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirectToEdit();
        var current = await FounderPhotoRowAsync(true);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return InvalidRedirectToEdit();

        var row = await _db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == ContentKeys.FounderPhoto);
        if (row is null)
        {
            row = new SiteContent { SectionKey = ContentKeys.FounderPhoto };
            _db.SiteContents.Add(row);
        }
        var leaving = ApplyImage(_db, current, uploaded, input, m => row.ContentAr = m?.FileKey ?? "");
        row.UpdatedAt = DateTime.UtcNow;
        await SaveWithImageAsync(_db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.PhotoSaved);
        return RedirectToAction(nameof(Edit));
    }

    private IActionResult InvalidRedirectToEdit()
    {
        TempData["Error"] = JoinErrors();
        return RedirectToAction(nameof(Edit));
    }

    /// <summary>The pages a page-key setting can point to (route table order), labelled with their Arabic titles.</summary>
    private async Task SetPageChoicesAsync()
    {
        var titles = await _db.Pages.AsNoTracking().ToDictionaryAsync(p => p.Key, p => p.TitleAr);
        ViewData[PageChoicesKey] = SiteRoutes.All
            .Select(p => new KeyValuePair<string, string>(p.Key, $"{titles.GetValueOrDefault(p.Key) ?? p.Key} ({p.Path})"))
            .ToList();
        await SetFounderPhotoAsync();
    }

    public const string PageChoicesKey = "PageChoices";

    [HttpPost]
    public async Task<IActionResult> Edit(ContentEditInput input)
    {
        // Only registry keys are considered (anything else posted is ignored — no over-posting); keys that were not
        // posted are left unchanged.
        var posted = input.Content
            .Select(p => (Key: ContentKeys.Find(p.Key), Value: p.Value ?? new BilingualValueInput()))
            .Where(p => p.Key is not null)
            .Select(p => (Key: p.Key!, p.Value))
            .ToList();

        foreach (var (key, value) in posted)
            Validate(key, value);

        if (!ModelState.IsValid)
        {
            // Re-render with the admin's input kept, on top of the stored values for keys that were not posted.
            var stored = await _db.SiteContents.AsNoTracking()
                .ToDictionaryAsync(c => c.SectionKey, c => new BilingualValueInput { Ar = c.ContentAr, En = c.ContentEn });
            var model = new ContentEditInput();
            foreach (var key in ContentKeys.All)
                model.Content[key.Key] = stored.GetValueOrDefault(key.Key) ?? new BilingualValueInput();
            foreach (var (key, value) in posted)
                model.Content[key.Key] = value;
            await SetPageChoicesAsync();
            return View(ViewPath, model);
        }

        // One query for every row; a registry key missing from the database is created.
        var rows = await _db.SiteContents.ToDictionaryAsync(c => c.SectionKey);
        var now = DateTime.UtcNow;
        foreach (var (key, value) in posted)
        {
            if (!rows.TryGetValue(key.Key, out var row))
            {
                row = new SiteContent { SectionKey = key.Key };
                _db.SiteContents.Add(row);
            }

            var ar = Normalize(key, value.Ar) ?? "";
            var en = key.Neutral ? null : Normalize(key, value.En); // empty English → NULL → the page falls back
            // Compared in the SAME normal form: a browser posts textarea line breaks as CRLF and the stored value may
            // have either (rows written by the migration on Windows carry CRLF); only a real change of the text is
            // written and stamps UpdatedAt.
            var isNew = row.Id == 0;
            if (isNew || (Normalize(key, row.ContentAr) ?? "") != ar || (key.Neutral ? null : Normalize(key, row.ContentEn)) != en)
            {
                row.ContentAr = ar;
                row.ContentEn = en;
                row.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(); // the interceptor invalidates the home-page cache
        TempData["Success"] = L(MessageKeys.Admin.ContentSaved);
        return RedirectToAction(nameof(Edit));
    }

    private static string? Normalize(ContentKey key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().Replace("\r\n", "\n").Replace('\r', '\n'); // one line-break form: LF
        return key.Rule == ContentRule.BaseUrl && SiteUrl.TryNormalize(v, out var normalized) ? normalized : v;
    }

    private void Validate(ContentKey key, BilingualValueInput value)
    {
        var label = key.Label;
        void Check(string? text, string field, string fieldLabel)
        {
            if ((text?.Length ?? 0) > key.MaxLength)
                ModelState.AddModelError(field, L(MessageKeys.MaxLength, fieldLabel, key.MaxLength));
            if (key.Format == ContentFormat.Highlight && !ContentText.TryParseHighlight(text, out _, out _, out _))
                ModelState.AddModelError(field, L(MessageKeys.Highlight, fieldLabel));
        }

        var arField = $"content[{key.Key}].Ar";
        if (!key.AllowEmpty && !key.Optional && string.IsNullOrWhiteSpace(value.Ar))
            ModelState.AddModelError(arField, L(MessageKeys.Required, label));
        Check(value.Ar, arField, label);
        if (!key.Neutral)
            Check(value.En, $"content[{key.Key}].En", label + " (EN)");

        if (string.IsNullOrWhiteSpace(value.Ar)) return;
        var ok = key.Rule switch
        {
            ContentRule.HttpsUrl => SiteUrl.IsHttpsUrl(value.Ar),
            ContentRule.BaseUrl => SiteUrl.TryNormalize(value.Ar, out _),
            ContentRule.SitePath => SiteUrl.IsSitePath(value.Ar),
            ContentRule.Count => ContentKeys.ParseCount(value.Ar.Trim(), 0) > 0,
            ContentRule.PageKey => SiteRoutes.IsKey(value.Ar.Trim()),
            ContentRule.Digits => value.Ar.Trim() is { Length: >= 8 and <= 15 } d && d.All(char.IsAsciiDigit),
            ContentRule.UtcOffset => EventDates.TryParseOffset(value.Ar.Trim(), out _),
            _ => true
        };
        if (!ok)
        {
            var message = key.Rule switch
            {
                ContentRule.BaseUrl => L(MessageKeys.BaseUrl),
                ContentRule.SitePath => L(MessageKeys.SitePath, label),
                ContentRule.Count => L(MessageKeys.Range, label, 1, ContentKeys.MaxCount),
                ContentRule.PageKey or ContentRule.Digits => L(MessageKeys.InvalidValue, label),
                ContentRule.UtcOffset => L(MessageKeys.UtcOffset),
                _ => L(MessageKeys.HttpsUrl, label)
            };
            ModelState.AddModelError(arField, message);
        }
    }
}
