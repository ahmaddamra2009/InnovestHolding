using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
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

        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.DescriptionAr = input.DescriptionAr?.Trim() ?? "";
        item.DescriptionEn = Optional(input.DescriptionEn);
        item.ShortTitleAr = Optional(input.ShortTitleAr);
        item.ShortTitleEn = Optional(input.ShortTitleEn);
        item.LinkUrl = Optional(input.LinkUrl);
        // ShowInFooter is no longer edited here: the footer lists the pages of the site, not the services.

        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.PlatformUpdated);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Shows or hides the row on the public site (a separate action: a form that omits the flag never flips it).</summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await _db.Platforms.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index));
    }

}
