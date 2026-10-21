using InnovestHolding.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

[Authorize]
public class RegistrationsController : Controller
{
    private readonly AppDbContext _db;

    public RegistrationsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Registrations/Index.cshtml",
            await _db.Registrations.OrderByDescending(r => r.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var item = await _db.Registrations.FindAsync(id);
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
        var item = await _db.Registrations.FindAsync(id);
        if (item != null)
        {
            _db.Registrations.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم حذف التسجيل بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }
}
