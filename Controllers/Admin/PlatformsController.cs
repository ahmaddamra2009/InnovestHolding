using InnovestHolding.Data;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class PlatformsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public PlatformsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Platforms/Index.cshtml",
            await _db.Platforms.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Update(PlatformUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.Platforms.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.Title = input.Title!.Trim();
        item.Description = input.Description?.Trim() ?? "";

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث المنصة بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
