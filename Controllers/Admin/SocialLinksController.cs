using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The four whitelisted footer networks (rows written by the migration). Only the URL, order and active flag are
/// editable — no create/delete: the icon and name of a network are code. An empty URL hides the link.
/// </summary>
public class SocialLinksController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public SocialLinksController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/SocialLinks/Index.cshtml",
            await _db.SocialLinks.AsNoTracking().OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Update(SocialLinkUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.SocialLinks.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.Url = Optional(input.Url);
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.SocialUpdated);
        return RedirectToAction(nameof(Index));
    }
}
