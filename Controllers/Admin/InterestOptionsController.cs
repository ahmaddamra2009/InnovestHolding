using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>The registration form's interest list. Deleting an option keeps the registrations' Arabic snapshot.</summary>
public class InterestOptionsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public InterestOptionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/InterestOptions/Index.cshtml",
            await _db.InterestOptions.AsNoTracking().OrderBy(o => o.SortOrder).ThenBy(o => o.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(SimpleListItemInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await _db.InterestOptions.MaxAsync(o => (int?)o.SortOrder) ?? 0;
        _db.InterestOptions.Add(new InterestOption
        {
            NameAr = input.NameAr!.Trim(),
            NameEn = Optional(input.NameEn),
            SortOrder = maxOrder + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.OptionAdded);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(InterestOptionUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.InterestOptions.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.NameAr = input.NameAr!.Trim();
        item.NameEn = Optional(input.NameEn);
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.OptionUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.InterestOptions.FindAsync(id);
        if (item != null)
        {
            _db.InterestOptions.Remove(item); // FK SetNull: registrations keep their Interest text
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.OptionDeleted);
        }
        return RedirectToAction(nameof(Index));
    }
}
