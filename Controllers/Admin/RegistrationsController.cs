using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class RegistrationsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public RegistrationsController(AppDbContext db) => _db = db;

    /// <summary>Newest first, <see cref="AdminControllerBase.PageSize"/> per page (index IX_Registrations_CreatedAt).</summary>
    public async Task<IActionResult> Index(int? page)
    {
        var total = await _db.Registrations.CountAsync();
        var unread = await _db.Registrations.CountAsync(r => !r.IsRead);
        var current = PagedListViewModel<Registration>.Clamp(page, total, PageSize);
        var items = await _db.Registrations.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((current - 1) * PageSize).Take(PageSize)
            .ToListAsync();
        return View("~/Views/Admin/Registrations/Index.cshtml", new PagedListViewModel<Registration>
        {
            Items = items, Page = current, PageSize = PageSize, TotalCount = total, UnreadCount = unread
        });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, int? page)
    {
        var item = await _db.Registrations.FindAsync(id);
        if (item != null)
        {
            item.IsRead = !item.IsRead;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { page = page > 1 ? page : null });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int? page)
    {
        var item = await _db.Registrations.FindAsync(id);
        if (item != null)
        {
            _db.Registrations.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.RegistrationDeleted);
        }
        return RedirectToAction(nameof(Index), new { page = page > 1 ? page : null });
    }
}
