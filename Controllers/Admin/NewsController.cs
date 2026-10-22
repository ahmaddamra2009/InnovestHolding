using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
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
            await _db.News.AsNoTracking().OrderBy(n => n.SortOrder).ThenBy(n => n.Id).ToListAsync());

    public IActionResult Create() => View(CreateView, new NewsInput());

    [HttpPost]
    public async Task<IActionResult> Create(NewsInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);

        // Newest first: a new item goes before every existing one.
        var item = new News { SortOrder = (await _db.News.MinAsync(n => (int?)n.SortOrder) ?? 1) - 1 };
        Map(input, item);
        _db.News.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.NewsAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.News.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new NewsEditInput
        {
            Id = item.Id,
            TitleAr = item.TitleAr,
            TitleEn = item.TitleEn,
            SummaryAr = item.SummaryAr,
            SummaryEn = item.SummaryEn,
            CategoryAr = item.CategoryAr,
            CategoryEn = item.CategoryEn,
            NewsDateAr = item.NewsDateAr,
            NewsDateEn = item.NewsDateEn,
            LinkUrl = item.LinkUrl,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder
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
        item.SortOrder = input.SortOrder;

        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.NewsUpdated);
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
            TempData["Success"] = L(MessageKeys.Admin.NewsDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(NewsInput input, News item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.SummaryAr = input.SummaryAr?.Trim() ?? "";
        item.SummaryEn = Optional(input.SummaryEn);
        item.CategoryAr = input.CategoryAr?.Trim() ?? "";
        item.CategoryEn = Optional(input.CategoryEn);
        item.NewsDateAr = input.NewsDateAr?.Trim() ?? "";
        item.NewsDateEn = Optional(input.NewsDateEn);
        item.LinkUrl = Optional(input.LinkUrl);
        item.IsActive = input.IsActive;
    }
}
