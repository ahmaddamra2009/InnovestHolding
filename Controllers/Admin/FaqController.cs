using InnovestHolding.Data;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class FaqController : AdminControllerBase
{
    private const string CreateView = "~/Views/Admin/Faq/Create.cshtml";
    private const string EditView = "~/Views/Admin/Faq/Edit.cshtml";
    private readonly AppDbContext _db;

    public FaqController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Faq/Index.cshtml",
            await _db.Faqs.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync());

    public IActionResult Create() => View(CreateView, new FaqInput());

    [HttpPost]
    public async Task<IActionResult> Create(FaqInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);

        var item = new Faq { SortOrder = (await _db.Faqs.MaxAsync(f => (int?)f.SortOrder) ?? 0) + 1 };
        Map(input, item);
        _db.Faqs.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة السؤال بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Faqs.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new FaqEditInput
        {
            Id = item.Id,
            Question = item.Question,
            Answer = item.Answer,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(FaqEditInput input)
    {
        if (!ModelState.IsValid)
            return View(EditView, input);

        var item = await _db.Faqs.FindAsync(input.Id);
        if (item == null) return NotFound();

        Map(input, item);
        item.SortOrder = input.SortOrder;

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

    private static void Map(FaqInput input, Faq item)
    {
        item.Question = input.Question!.Trim();
        item.Answer = input.Answer!.Trim();
        item.IsActive = input.IsActive;
    }
}
