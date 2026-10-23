using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «أرقام الأثر»: the figures of the hero (the first visible ones, up to hero_stats_limit) and the impact section.
/// Create, edit, show/hide, order, delete. No icon field: the design renders no stat icon (G25, dismissed);
/// <c>IconSvg</c> is never bound.
/// </summary>
public class StatsController(AppDbContext db) : AdminControllerBase
{
    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Stats/Index.cshtml",
            await db.Stats.AsNoTracking().OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(StatInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();
        var max = await db.Stats.MaxAsync(s => (int?)s.SortOrder) ?? 0;
        var item = new Stat { SortOrder = max + 1, IsActive = true };
        Map(input, item);
        db.Stats.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Update(StatUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await db.Stats.FindAsync(input.Id);
        if (item == null) return NotFound();
        Map(input, item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatUpdated);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.Stats.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        if (await MoveAsync(db.Stats, id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Stats.FindAsync(id);
        if (item == null) return NotFound();
        db.Stats.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return RedirectToAction(nameof(Index));
    }

    private static void Map(StatInput input, Stat item)
    {
        item.LabelAr = input.LabelAr!.Trim();
        item.LabelEn = Optional(input.LabelEn);
        item.ShortLabelAr = Optional(input.ShortLabelAr);
        item.ShortLabelEn = Optional(input.ShortLabelEn);
        item.Value = input.Value!.Value;
        item.Suffix = input.Suffix?.Trim() ?? "";
    }
}
