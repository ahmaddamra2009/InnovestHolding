using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>The bullets of the registration section.</summary>
public class RegisterBenefitsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public RegisterBenefitsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/RegisterBenefits/Index.cshtml",
            await _db.RegisterBenefits.AsNoTracking().OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(RegisterBenefitInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await _db.RegisterBenefits.MaxAsync(b => (int?)b.SortOrder) ?? 0;
        _db.RegisterBenefits.Add(new RegisterBenefit
        {
            TextAr = input.TextAr!.Trim(),
            TextEn = Optional(input.TextEn),
            SortOrder = maxOrder + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.BenefitAdded);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(RegisterBenefitUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.RegisterBenefits.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.TextAr = input.TextAr!.Trim();
        item.TextEn = Optional(input.TextEn);
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.BenefitUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.RegisterBenefits.FindAsync(id);
        if (item != null)
        {
            _db.RegisterBenefits.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.BenefitDeleted);
        }
        return RedirectToAction(nameof(Index));
    }
}
