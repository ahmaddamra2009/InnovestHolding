using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class NewsController : Controller
{
    private readonly AppDbContext _db;

    public NewsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/News/Index.cshtml",
            await _db.News.OrderByDescending(n => n.CreatedAt).ToListAsync());

    public IActionResult Create() =>
        View("~/Views/Admin/News/Create.cshtml", new News());

    [HttpPost]
    public async Task<IActionResult> Create(News model)
    {
        _db.News.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة الخبر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.News.FindAsync(id);
        if (item == null) return NotFound();
        return View("~/Views/Admin/News/Edit.cshtml", item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(News model)
    {
        var item = await _db.News.FindAsync(model.Id);
        if (item == null) return NotFound();

        item.Title = model.Title;
        item.Summary = model.Summary;
        item.Category = model.Category;
        item.NewsDate = model.NewsDate;
        item.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث الخبر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.News.FindAsync(id);
        if (item != null)
        {
            _db.News.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم حذف الخبر بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }
}
