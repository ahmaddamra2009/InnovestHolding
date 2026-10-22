using InnovestHolding.Data;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class NavController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public NavController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Nav/Index.cshtml",
            await _db.NavItems.AsNoTracking()
                .Include(n => n.SubItems.OrderBy(s => s.SortOrder))
                .OrderBy(n => n.SortOrder)
                .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(NavItemCreateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await _db.NavItems.MaxAsync(n => (int?)n.SortOrder) ?? 0;
        _db.NavItems.Add(new NavItem
        {
            Title = input.Title!.Trim(),
            Href = input.Href!.Trim(),
            SortOrder = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة العنصر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(NavItemUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.NavItems.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.Title = input.Title!.Trim();
        item.Href = input.Href!.Trim();
        item.HasDropdown = input.HasDropdown;
        item.IsActive = input.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث العنصر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.NavItems.Include(n => n.SubItems).FirstOrDefaultAsync(n => n.Id == id);
        if (item != null)
        {
            _db.NavItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم حذف العنصر بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }

    // ── Sub Items ──
    [HttpPost]
    public async Task<IActionResult> AddSubItem(NavSubItemCreateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var parent = await _db.NavItems.FindAsync(input.NavItemId);
        if (parent == null) return NotFound();

        parent.HasDropdown = true;
        var maxOrder = await _db.NavSubItems
            .Where(s => s.NavItemId == input.NavItemId)
            .MaxAsync(s => (int?)s.SortOrder) ?? 0;

        _db.NavSubItems.Add(new NavSubItem
        {
            NavItemId = input.NavItemId,
            Title = input.Title!.Trim(),
            Href = input.Href!.Trim(),
            SortOrder = maxOrder + 1
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة العنصر الفرعي بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSubItem(int id)
    {
        var item = await _db.NavSubItems.FindAsync(id);
        if (item != null)
        {
            _db.NavSubItems.Remove(item);
            await _db.SaveChangesAsync();

            var remaining = await _db.NavSubItems.CountAsync(s => s.NavItemId == item.NavItemId);
            if (remaining == 0)
            {
                var parent = await _db.NavItems.FindAsync(item.NavItemId);
                if (parent != null) parent.HasDropdown = false;
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "تم حذف العنصر الفرعي بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }
}
