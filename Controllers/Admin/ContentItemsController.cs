using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The bilingual lists shown on the pages (values, sectors, services of a unit…), one group at a time. Only groups of
/// the registry (Infrastructure/Content/ContentItemGroups) and icons of the whitelist (Icons) are accepted.
/// </summary>
public class ContentItemsController(AppDbContext db) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? group)
    {
        var g = ContentItemGroups.Find(group) ?? ContentItemGroups.All[0];
        var items = await db.ContentItems.AsNoTracking().Where(i => i.Group == g.Key)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync();
        var counts = await db.ContentItems.AsNoTracking().GroupBy(i => i.Group)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View("~/Views/Admin/ContentItems/Index.cshtml", new ContentItemsVm(g, items, counts));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContentItemInput input)
    {
        var g = Validate(input);
        if (!ModelState.IsValid || g is null) return Back(g);

        var max = await db.ContentItems.Where(i => i.Group == g.Key).MaxAsync(i => (int?)i.SortOrder) ?? 0;
        db.ContentItems.Add(new ContentItem
        {
            Group = g.Key,
            TextAr = input.TextAr!.Trim(),
            TextEn = Optional(input.TextEn),
            DescriptionAr = g.UsesDescription ? Optional(input.DescriptionAr) : null,
            DescriptionEn = g.UsesDescription ? Optional(input.DescriptionEn) : null,
            IconKey = Optional(input.IconKey),
            SortOrder = max + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return Back(g);
    }

    [HttpPost]
    public async Task<IActionResult> Update(ContentItemUpdateInput input)
    {
        var g = Validate(input);
        if (!ModelState.IsValid || g is null) return Back(g);

        // The row must belong to the posted group (no moving rows between groups by tampering).
        var item = await db.ContentItems.FirstOrDefaultAsync(i => i.Id == input.Id && i.Group == g.Key);
        if (item is null) return NotFound();

        item.TextAr = input.TextAr!.Trim();
        item.TextEn = Optional(input.TextEn);
        item.DescriptionAr = g.UsesDescription ? Optional(input.DescriptionAr) : item.DescriptionAr;
        item.DescriptionEn = g.UsesDescription ? Optional(input.DescriptionEn) : item.DescriptionEn;
        item.IconKey = Optional(input.IconKey);
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return Back(g);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ContentItems.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));
        db.ContentItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return Back(ContentItemGroups.Find(item.Group));
    }

    private ContentItemGroup? Validate(ContentItemInput input)
    {
        var g = ContentItemGroups.Find(input.Group);
        if (g is null) ModelState.AddModelError(nameof(input.Group), L(MessageKeys.InvalidValueNoField));
        if (!string.IsNullOrWhiteSpace(input.IconKey) && !Icons.IsKnown(input.IconKey.Trim()))
            ModelState.AddModelError(nameof(input.IconKey), L(MessageKeys.InvalidValueNoField));
        return g;
    }

    /// <summary>PRG back to the group's list (errors, if any, go to TempData["Error"]).</summary>
    private IActionResult Back(ContentItemGroup? g)
    {
        if (!ModelState.IsValid) TempData["Error"] = JoinErrors();
        return RedirectToAction(nameof(Index), new { group = g?.Key });
    }
}

public sealed record ContentItemsVm(ContentItemGroup Group, IReadOnlyList<ContentItem> Items, IReadOnlyDictionary<string, int> Counts);
