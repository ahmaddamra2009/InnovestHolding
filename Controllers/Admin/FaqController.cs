using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class FaqController : Controller
{
    private readonly AppDbContext _db;

    public FaqController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Faq/Index.cshtml",
            await _db.Faqs.OrderBy(f => f.SortOrder).ToListAsync());

    public IActionResult Create() =>
        View("~/Views/Admin/Faq/Create.cshtml", new Faq());

    [HttpPost]
    public async Task<IActionResult> Create(Faq model)
    {
        model.SortOrder = (await _db.Faqs.MaxAsync(f => (int?)f.SortOrder) ?? 0) + 1;
        _db.Faqs.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة السؤال بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Faqs.FindAsync(id);
        if (item == null) return NotFound();
        return View("~/Views/Admin/Faq/Edit.cshtml", item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Faq model)
    {
        var item = await _db.Faqs.FindAsync(model.Id);
        if (item == null) return NotFound();

        item.Question = model.Question;
        item.Answer = model.Answer;
        item.SortOrder = model.SortOrder;
        item.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث السؤال بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Faqs.FindAsync(id);
        if (item != null)
        {
            _db.Faqs.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم حذف السؤال بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }
}
