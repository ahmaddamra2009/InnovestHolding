using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class MessagesController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db) => _db = db;

    /// <summary>Newest first, <see cref="AdminControllerBase.PageSize"/> per page (index IX_ContactMessages_CreatedAt).</summary>
    public async Task<IActionResult> Index(int? page)
    {
        var total = await _db.ContactMessages.CountAsync();
        var unread = await _db.ContactMessages.CountAsync(m => !m.IsRead);
        var current = PagedListViewModel<ContactMessage>.Clamp(page, total, PageSize);
        var items = await _db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Skip((current - 1) * PageSize).Take(PageSize)
            .ToListAsync();
        return View("~/Views/Admin/Messages/Index.cshtml", new PagedListViewModel<ContactMessage>
        {
            Items = items, Page = current, PageSize = PageSize, TotalCount = total, UnreadCount = unread
        });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, int? page)
    {
        var item = await _db.ContactMessages.FindAsync(id);
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
        var item = await _db.ContactMessages.FindAsync(id);
        if (item != null)
        {
            _db.ContactMessages.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.MessageDeleted);
        }
        return RedirectToAction(nameof(Index), new { page = page > 1 ? page : null });
    }
}
