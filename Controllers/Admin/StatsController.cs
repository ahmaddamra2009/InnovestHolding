using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class StatsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public StatsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Stats/Index.cshtml",
            await _db.Stats.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Update(StatUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.Stats.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.LabelAr = input.LabelAr!.Trim();
        item.LabelEn = Optional(input.LabelEn);
        item.ShortLabelAr = Optional(input.ShortLabelAr);
        item.ShortLabelEn = Optional(input.ShortLabelEn);
        item.Value = input.Value!.Value;
        item.Suffix = input.Suffix?.Trim() ?? "";

        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatUpdated);
        return RedirectToAction(nameof(Index));
    }
}
