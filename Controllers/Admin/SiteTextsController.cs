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

    /// <summary>The key, the texts, and the field's group (its id and its tab name, e.g. «صفحة 404 وصفحة الخطأ»: QA C5-8).</summary>
    private static bool Matches(SiteTextField f, string q) =>
        new[] { f.Text.ResxKey, f.DefaultAr, f.DefaultEn, f.ValueAr, f.ValueEn, f.Text.Group,
                PublicTexts.Groups.FirstOrDefault(g => g.Id == f.Text.Group).Title }
            .Any(s => s?.Contains(q, StringComparison.OrdinalIgnoreCase) == true);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');

    /// <summary>
    /// Saves every posted text. The per-field and per-group reset buttons are submit buttons of the SAME form
    /// (<paramref name="reset"/> = a resx key, <paramref name="resetGroup"/> = a group id): the reset text(s) are treated
    /// as empty (row deleted) and every other posted edit is saved with them, so a reset never loses unsaved edits
    /// elsewhere on the page (no JS needed). An invalid edit elsewhere re-renders the page with every edit kept.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Index(SiteTextsInput input, string? tab, string? q, string? reset, string? resetGroup)
    {
        tab = TabOf(tab);
        q = Search(q);
        var resetKeys = PublicTexts.All
            .Where(t => t.ResxKey == reset || (resetGroup is not null && t.Group == resetGroup))
            .Select(t => t.ResxKey).ToHashSet(StringComparer.Ordinal);
        // Only registry keys are considered (no over-posting); keys that were not posted are left unchanged.
        var posted = input.Texts
            .Select(p => (Text: PublicTexts.Find(p.Key), Value: p.Value ?? new BilingualValueInput()))
            .Where(p => p.Text is not null && !resetKeys.Contains(p.Text.ResxKey))
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
            foreach (var key in resetKeys) values[key] = new BilingualValueInput(); // the reset asked for is shown, saved with the fix
            return View(ViewPath, Build(tab, q, stored, values));
        }

        posted.AddRange(resetKeys.Select(k => (Text: PublicTexts.Find(k)!, Ar: (string?)null, En: (string?)null)));
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
        TempData["Success"] = L(resetGroup is not null && resetKeys.Count > 0 ? MessageKeys.Admin.SiteTextGroupReset
            : resetKeys.Count > 0 ? MessageKeys.Admin.SiteTextReset : MessageKeys.Admin.SiteTextsSaved);
        var url = Url.Action(nameof(Index), new { tab, q });
        return Redirect(reset is not null && resetKeys.Count > 0 ? url + "#t-" + Anchor(reset) : url!);
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
        if (PublicTexts.HasFormatSpec(value))
            ModelState.AddModelError(field, L(MessageKeys.Admin.SiteTextFormatSpec, label, language));
        else if (!PublicTexts.PlaceholdersMatch(value, defaultText))
        {
            var expected = PublicTexts.Placeholders(defaultText);
            ModelState.AddModelError(field, L(MessageKeys.Admin.SiteTextPlaceholders, label, language,
                expected.Count == 0 ? "—" : string.Join(" ", expected)));
        }
    }

    /// <summary>The element id of a field (resx keys are letters, digits and "_").</summary>
    public static string Anchor(string key) => new(key.Where(char.IsAsciiLetterOrDigit).ToArray());
}
