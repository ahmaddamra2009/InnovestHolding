using InnovestHolding.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Messages/Index.cshtml",
            await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var item = await _db.ContactMessages.FindAsync(id);
        if (item != null)
        {
            item.IsRead = !item.IsRead;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.ContactMessages.FindAsync(id);
        if (item != null)
        {
            _db.ContactMessages.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم حذف الرسالة بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }
}
