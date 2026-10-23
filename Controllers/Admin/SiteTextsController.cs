using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// Admin › رسائل وأزرار الموقع: the overrides of the public interface texts (<see cref="PublicTexts"/>). An override is a
/// SiteContents row <c>ui.&lt;resx key&gt;</c>; an empty language keeps the resx text of that language, and a text with no
/// override has no row (Reset deletes it). Every save goes through SaveChanges, so the content snapshot is dropped
/// (ContentCacheInvalidationInterceptor) and the next public request reads the new text.
/// </summary>
public class SiteTextsController(AppDbContext db) : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/SiteTexts/Index.cshtml";
    public const string AllTab = "all";

    private static string TabOf(string? tab) => PublicTexts.Groups.Any(g => g.Id == tab) ? tab! : AllTab;

    private static string? Search(string? q) => string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];

    private Task<Dictionary<string, SiteContent>> RowsAsync(bool tracked) =>
        (tracked ? db.SiteContents : db.SiteContents.AsNoTracking())
            .Where(c => c.SectionKey.StartsWith(PublicTexts.Prefix))
            .ToDictionaryAsync(c => c.SectionKey, StringComparer.Ordinal);

    public async Task<IActionResult> Index(string? tab, string? q)
    {
        var rows = await RowsAsync(false);
        return View(ViewPath, Build(TabOf(tab), Search(q), rows, null));
    }

    private static SiteTextsViewModel Build(string tab, string? q, IReadOnlyDictionary<string, SiteContent> rows,
        IReadOnlyDictionary<string, BilingualValueInput>? posted)
    {
        bool Custom(PublicText t) => rows.TryGetValue(t.RowKey, out var r) && (!string.IsNullOrWhiteSpace(r.ContentAr) || !string.IsNullOrWhiteSpace(r.ContentEn));

        var fields = PublicTexts.All
            .Where(t => tab == AllTab || t.Group == tab)
            .Select(t =>
            {
                rows.TryGetValue(t.RowKey, out var row);
                var input = posted?.GetValueOrDefault(t.ResxKey);
                return new SiteTextField(t, PublicTexts.Default(t, false), PublicTexts.Default(t, true),
                    input is not null ? input.Ar : row?.ContentAr, input is not null ? input.En : row?.ContentEn, Custom(t));
            })
            .Where(f => q is null || Matches(f, q))
            .ToList();
        var counts = PublicTexts.Groups.ToDictionary(g => g.Id, g => PublicTexts.All.Count(t => t.Group == g.Id && Custom(t)));
        return new SiteTextsViewModel(tab, q, fields, counts);
    }

    private static bool Matches(SiteTextField f, string q) =>
        new[] { f.Text.ResxKey, f.DefaultAr, f.DefaultEn, f.ValueAr, f.ValueEn }
            .Any(s => s?.Contains(q, StringComparison.OrdinalIgnoreCase) == true);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');

    [HttpPost]
    public async Task<IActionResult> Index(SiteTextsInput input, string? tab, string? q)
    {
        tab = TabOf(tab);
        q = Search(q);
        // Only registry keys are considered (no over-posting); keys that were not posted are left unchanged.
        var posted = input.Texts
            .Select(p => (Text: PublicTexts.Find(p.Key), Value: p.Value ?? new BilingualValueInput()))
            .Where(p => p.Text is not null)
            .Select(p => (Text: p.Text!, Ar: Clean(p.Value.Ar), En: Clean(p.Value.En)))
            .ToList();

        foreach (var (text, ar, en) in posted)
        {
            Validate(text, ar, false);
            Validate(text, en, true);
        }

        if (!ModelState.IsValid)
        {
            var stored = await RowsAsync(false);
            var values = input.Texts.ToDictionary(p => p.Key, p => p.Value ?? new BilingualValueInput(), StringComparer.Ordinal);
            return View(ViewPath, Build(tab, q, stored, values));
        }

        var rows = await RowsAsync(true);
        var now = DateTime.UtcNow;
        foreach (var (text, ar, en) in posted)
        {
            rows.TryGetValue(text.RowKey, out var row);
            if (ar is null && en is null)
            {
                if (row is not null) db.SiteContents.Remove(row); // both languages back to the default: no row
                continue;
            }
            if (row is null)
            {
                db.SiteContents.Add(new SiteContent { SectionKey = text.RowKey, ContentAr = ar ?? "", ContentEn = en, UpdatedAt = now });
            }
            else if ((Clean(row.ContentAr) ?? "") != (ar ?? "") || Clean(row.ContentEn) != en)
            {
                row.ContentAr = ar ?? "";
                row.ContentEn = en;
                row.UpdatedAt = now;
            }
        }
        await db.SaveChangesAsync(); // the interceptor drops the content snapshot
        TempData["Success"] = L(MessageKeys.Admin.SiteTextsSaved);
        return RedirectToAction(nameof(Index), new { tab, q });
    }

    private void Validate(PublicText text, string? value, bool english)
    {
        if (value is null) return;
        var field = $"texts[{text.ResxKey}].{(english ? "En" : "Ar")}";
        var defaultText = PublicTexts.Default(text, english);
        var label = PublicTexts.Default(text, false);
        var language = english ? "EN" : "ع";
        if (value.Length > PublicTexts.MaxLength)
            ModelState.AddModelError(field, L(MessageKeys.MaxLength, $"{label} ({language})", PublicTexts.MaxLength));
        if (PublicTexts.HasMarkupOrLink(value, defaultText))
            ModelState.AddModelError(field, L(MessageKeys.Admin.SiteTextMarkup, label, language));
        if (!PublicTexts.PlaceholdersMatch(value, defaultText))
        {
            var expected = PublicTexts.Placeholders(defaultText);
            ModelState.AddModelError(field, L(MessageKeys.Admin.SiteTextPlaceholders, label, language,
                expected.Count == 0 ? "—" : string.Join(" ", expected)));
        }
    }

    /// <summary>One text back to its resx default (both languages): the row is deleted.</summary>
    [HttpPost]
    public async Task<IActionResult> Reset(string? key, string? tab, string? q)
    {
        if (PublicTexts.Find(key) is { } text
            && await db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == text.RowKey) is { } row)
        {
            db.SiteContents.Remove(row);
            await db.SaveChangesAsync();
        }
        TempData["Success"] = L(MessageKeys.Admin.SiteTextReset);
        return Redirect(Url.Action(nameof(Index), new { tab = TabOf(tab), q = Search(q) }) + (key is null ? "" : "#t-" + Anchor(key)));
    }

    /// <summary>Every text of one group back to its default.</summary>
    [HttpPost]
    public async Task<IActionResult> ResetGroup(string? group)
    {
        var keys = PublicTexts.All.Where(t => t.Group == group).Select(t => t.RowKey).ToList();
        if (keys.Count > 0)
        {
            var rows = await db.SiteContents.Where(c => keys.Contains(c.SectionKey)).ToListAsync();
            if (rows.Count > 0)
            {
                db.SiteContents.RemoveRange(rows);
                await db.SaveChangesAsync();
            }
        }
        TempData["Success"] = L(MessageKeys.Admin.SiteTextGroupReset);
        return RedirectToAction(nameof(Index), new { tab = TabOf(group) });
    }

    /// <summary>The element id of a field (resx keys are letters, digits and "_").</summary>
    public static string Anchor(string key) => new(key.Where(char.IsAsciiLetterOrDigit).ToArray());
}
