using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The headings of the Forum Partners page (tiers or categories). Managed on the Partners screen; deleting a group
/// keeps its partners (FK SET NULL: they are listed after the groups).
/// </summary>
public class PartnerGroupsController(AppDbContext db) : AdminControllerBase
{
    public IActionResult Index() => RedirectToAction("Index", "Partners");

    [HttpPost]
    public async Task<IActionResult> Create(SimpleListItemInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();
        var maxOrder = await db.PartnerGroups.MaxAsync(g => (int?)g.SortOrder) ?? 0;
        db.PartnerGroups.Add(new PartnerGroup
        {
            NameAr = input.NameAr!.Trim(), NameEn = Optional(input.NameEn), SortOrder = maxOrder + 1, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.GroupAdded);
        return RedirectToAction("Index", "Partners");
    }

    [HttpPost]
    public async Task<IActionResult> Update(InterestOptionUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();
        var item = await db.PartnerGroups.FindAsync(input.Id);
        if (item == null) return NotFound();
        item.NameAr = input.NameAr!.Trim();
        item.NameEn = Optional(input.NameEn);
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.GroupUpdated);
        return RedirectToAction("Index", "Partners");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.PartnerGroups.FindAsync(id);
        if (item != null)
        {
            db.PartnerGroups.Remove(item); // FK SET NULL: its partners stay, without a group
            await db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.GroupDeleted);
        }
        return RedirectToAction("Index", "Partners");
    }
}
