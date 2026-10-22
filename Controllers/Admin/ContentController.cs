using InnovestHolding.Data;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class ContentController : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/Content/Edit.cshtml";
    private readonly AppDbContext _db;

    public ContentController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Edit()
    {
        var content = await _db.SiteContents.AsNoTracking()
            .ToDictionaryAsync(c => c.SectionKey, c => (string?)c.ContentAr);
        return View(ViewPath, new ContentEditInput { Content = content });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ContentEditInput input)
    {
        if (!ModelState.IsValid)
            return View(ViewPath, input);

        // One query for every row; keys that do not already exist are ignored (no over-posting).
        var rows = await _db.SiteContents.ToDictionaryAsync(c => c.SectionKey);
        var now = DateTime.UtcNow;
        foreach (var (key, value) in input.Content)
        {
            if (!rows.TryGetValue(key, out var item)) continue;
            item.ContentAr = value ?? "";
            item.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ التعديلات بنجاح";
        return RedirectToAction(nameof(Edit));
    }
}
