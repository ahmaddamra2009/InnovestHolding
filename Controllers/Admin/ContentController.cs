using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class ContentController : Controller
{
    private readonly AppDbContext _db;

    public ContentController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Edit()
    {
        var content = await _db.SiteContents.ToDictionaryAsync(c => c.SectionKey, c => c.ContentAr);
        return View("~/Views/Admin/Content/Edit.cshtml", content);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Dictionary<string, string> content)
    {
        foreach (var kvp in content)
        {
            var item = await _db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == kvp.Key);
            if (item != null)
            {
                item.ContentAr = kvp.Value;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ التعديلات بنجاح";
        return RedirectToAction(nameof(Edit));
    }
}
