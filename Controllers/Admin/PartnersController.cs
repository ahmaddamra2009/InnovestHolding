using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class PartnersController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public PartnersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Partners/Index.cshtml",
            await _db.Partners.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(PartnerCreateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await _db.Partners.MaxAsync(p => (int?)p.SortOrder) ?? 0;
        _db.Partners.Add(new Partner { NameAr = input.NameAr!.Trim(), NameEn = Optional(input.NameEn), SortOrder = maxOrder + 1 });
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.PartnerAdded);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Edits an existing partner's names (the English name can be added after creation).</summary>
    [HttpPost]
    public async Task<IActionResult> Update(PartnerUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.Partners.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.NameAr = input.NameAr!.Trim();
        item.NameEn = Optional(input.NameEn);
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.PartnerUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Partners.FindAsync(id);
        if (item != null)
        {
            _db.Partners.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.PartnerDeleted);
        }
        return RedirectToAction(nameof(Index));
    }
}
