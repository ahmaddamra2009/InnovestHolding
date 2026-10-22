using InnovestHolding.Data;
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
        TempData["Success"] = "تمت إضافة الفعالية بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new EventEditInput
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            EventDay = item.EventDay,
            EventMonth = item.EventMonth,
            Location = item.Location,
            Time = item.Time,
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
        TempData["Success"] = "تم تحديث الفعالية بنجاح";
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
            TempData["Success"] = "تم حذف الفعالية بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(EventInput input, Event item)
    {
        item.Title = input.Title!.Trim();
        item.Description = input.Description?.Trim() ?? "";
        item.EventDay = input.EventDay!.Value;
        item.EventMonth = input.EventMonth!.Trim();
        item.Location = input.Location?.Trim() ?? "";
        item.Time = input.Time?.Trim() ?? "";
        item.IsActive = input.IsActive;
    }
}
