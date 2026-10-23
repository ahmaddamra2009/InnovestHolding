using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The «خدماتنا» cards: create, edit (icon from the code whitelist), show/hide, order, delete. <c>IconSvg</c> (the
/// migration-owned legacy icon) and <c>ShortTitle*</c>/<c>ShowInFooter</c> (never rendered) are not bound.
/// </summary>
public class PlatformsController(AppDbContext db) : AdminControllerBase
{
    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Platforms/Index.cshtml",
            await db.Platforms.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(PlatformInput input)
    {
        CheckIcon(input.IconKey);
        if (!ModelState.IsValid) return InvalidRedirect();

        var max = await db.Platforms.MaxAsync(p => (int?)p.SortOrder) ?? 0;
        var item = new Platform { SortOrder = max + 1, IsActive = true };
        Map(input, item);
        db.Platforms.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Update(PlatformUpdateInput input)
    {
        CheckIcon(input.IconKey);
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await db.Platforms.FindAsync(input.Id);
        if (item == null) return NotFound();
        Map(input, item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.PlatformUpdated);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    /// <summary>Shows or hides the row on the public site (a separate action: a form that omits the flag never flips it).</summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.Platforms.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        if (await MoveAsync(db.Platforms, id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Platforms.FindAsync(id);
        if (item == null) return NotFound();
        db.Platforms.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return RedirectToAction(nameof(Index));
    }

    private void CheckIcon(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key) && !Icons.IsKnown(key.Trim()))
            ModelState.AddModelError(nameof(PlatformInput.IconKey), L(MessageKeys.AdminLists.IconInvalid));
    }

    private static void Map(PlatformInput input, Platform item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.DescriptionAr = input.DescriptionAr?.Trim() ?? "";
        item.DescriptionEn = Optional(input.DescriptionEn);
        item.LinkUrl = Optional(input.LinkUrl);
        item.IconKey = Optional(input.IconKey);
    }
}
