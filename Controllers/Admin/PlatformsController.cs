using InnovestHolding.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class PlatformsController : Controller
{
    private readonly AppDbContext _db;

    public PlatformsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Platforms/Index.cshtml",
            await _db.Platforms.OrderBy(p => p.SortOrder).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Update(int id, string title, string description)
    {
        var item = await _db.Platforms.FindAsync(id);
        if (item == null) return NotFound();

        item.Title = title;
        item.Description = description;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث المنصة بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
