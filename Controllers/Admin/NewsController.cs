using InnovestHolding.Data;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class NewsController : AdminControllerBase
{
    private const string CreateView = "~/Views/Admin/News/Create.cshtml";
    private const string EditView = "~/Views/Admin/News/Edit.cshtml";
    private readonly AppDbContext _db;

    public NewsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/News/Index.cshtml",
            await _db.News.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync());

    public IActionResult Create() => View(CreateView, new NewsInput());

    [HttpPost]
    public async Task<IActionResult> Create(NewsInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);

        var item = new News();
        Map(input, item);
        _db.News.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة الخبر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.News.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new NewsEditInput
        {
            Id = item.Id,
            Title = item.Title,
            Summary = item.Summary,
            Category = item.Category,
            NewsDate = item.NewsDate,
            IsActive = item.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(NewsEditInput input)
    {
        if (!ModelState.IsValid)
            return View(EditView, input);

        var item = await _db.News.FindAsync(input.Id);
        if (item == null) return NotFound();

        Map(input, item);

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

    private static void Map(NewsInput input, News item)
    {
        item.Title = input.Title!.Trim();
        item.Summary = input.Summary?.Trim() ?? "";
        item.Category = input.Category?.Trim() ?? "";
        item.NewsDate = input.NewsDate?.Trim() ?? "";
        item.IsActive = input.IsActive;
    }
}
