using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class EventsController : Controller
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Events/Index.cshtml",
            await _db.Events.OrderBy(e => e.SortOrder).ToListAsync());

    public IActionResult Create() =>
        View("~/Views/Admin/Events/Create.cshtml", new Event());

    [HttpPost]
    public async Task<IActionResult> Create(Event model)
    {
        model.SortOrder = (await _db.Events.MaxAsync(e => (int?)e.SortOrder) ?? 0) + 1;
        _db.Events.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة الفعالية بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Events.FindAsync(id);
        if (item == null) return NotFound();
        return View("~/Views/Admin/Events/Edit.cshtml", item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Event model)
    {
        var item = await _db.Events.FindAsync(model.Id);
        if (item == null) return NotFound();

        item.Title = model.Title;
        item.Description = model.Description;
        item.EventDay = model.EventDay;
        item.EventMonth = model.EventMonth;
        item.Location = model.Location;
        item.Time = model.Time;
        item.IsActive = model.IsActive;
        item.SortOrder = model.SortOrder;

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
}
