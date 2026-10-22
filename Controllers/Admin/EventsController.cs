using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class EventsController : AdminControllerBase
{
    private const string CreateView = "~/Views/Admin/Events/Create.cshtml";
    private const string EditView = "~/Views/Admin/Events/Edit.cshtml";
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Events/Index.cshtml",
            await _db.Events.AsNoTracking().OrderBy(e => e.SortOrder).ToListAsync());

    public IActionResult Create() => View(CreateView, new EventInput());

    [HttpPost]
    public async Task<IActionResult> Create(EventInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);

        var item = new Event { SortOrder = (await _db.Events.MaxAsync(e => (int?)e.SortOrder) ?? 0) + 1 };
        Map(input, item);
        _db.Events.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.EventAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new EventEditInput
        {
            Id = item.Id,
            TitleAr = item.TitleAr,
            TitleEn = item.TitleEn,
            DescriptionAr = item.DescriptionAr,
            DescriptionEn = item.DescriptionEn,
            EventDay = item.EventDay,
            EventMonthAr = item.EventMonthAr,
            EventMonthEn = item.EventMonthEn,
            LocationAr = item.LocationAr,
            LocationEn = item.LocationEn,
            TimeAr = item.TimeAr,
            TimeEn = item.TimeEn,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EventEditInput input)
    {
        if (!ModelState.IsValid)
            return View(EditView, input);

        var item = await _db.Events.FindAsync(input.Id);
        if (item == null) return NotFound();

        Map(input, item);
        item.SortOrder = input.SortOrder;

        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.EventUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Events.FindAsync(id);
        if (item != null)
        {
            _db.Events.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.EventDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(EventInput input, Event item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.DescriptionAr = input.DescriptionAr?.Trim() ?? "";
        item.DescriptionEn = Optional(input.DescriptionEn);
        item.EventDay = input.EventDay!.Value;
        item.EventMonthAr = input.EventMonthAr!.Trim();
        item.EventMonthEn = Optional(input.EventMonthEn);
        item.LocationAr = input.LocationAr?.Trim() ?? "";
        item.LocationEn = Optional(input.LocationEn);
        item.TimeAr = input.TimeAr?.Trim() ?? "";
        item.TimeEn = Optional(input.TimeEn);
        item.IsActive = input.IsActive;
    }
}
