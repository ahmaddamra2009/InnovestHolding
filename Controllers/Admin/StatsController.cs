using InnovestHolding.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class StatsController : Controller
{
    private readonly AppDbContext _db;

    public StatsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Stats/Index.cshtml",
            await _db.Stats.OrderBy(s => s.SortOrder).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Update(int id, string label, int value, string suffix)
    {
        var item = await _db.Stats.FindAsync(id);
        if (item == null) return NotFound();

        item.Label = label;
        item.Value = value;
        item.Suffix = suffix;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث الإحصائية بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
