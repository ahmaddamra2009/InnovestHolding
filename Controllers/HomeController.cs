using InnovestHolding.Data;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            Content = await _db.SiteContents
                .ToDictionaryAsync(c => c.SectionKey, c => c.ContentAr),

            NavItems = await _db.NavItems
                .Include(n => n.SubItems.OrderBy(s => s.SortOrder))
                .Where(n => n.IsActive)
                .OrderBy(n => n.SortOrder)
                .ToListAsync(),

            Events = await _db.Events
                .Where(e => e.IsActive)
                .OrderBy(e => e.SortOrder)
                .ToListAsync(),

            News = await _db.News
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(),

            Platforms = await _db.Platforms
                .OrderBy(p => p.SortOrder)
                .ToListAsync(),

            Partners = await _db.Partners
                .OrderBy(p => p.SortOrder)
                .ToListAsync(),

            Faqs = await _db.Faqs
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(),

            Stats = await _db.Stats
                .OrderBy(s => s.SortOrder)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Register(string fullName, string email, string phone, string interest)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            return Json(new { success = false, message = "الاسم والبريد الإلكتروني مطلوبان" });

        _db.Registrations.Add(new Models.Registration
        {
            FullName = fullName,
            Email = email,
            Phone = phone ?? "",
            Interest = interest ?? ""
        });

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = "تم التسجيل بنجاح! سنتواصل معك قريباً" });
    }

    [HttpPost]
    public async Task<IActionResult> Contact(string fullName, string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            return Json(new { success = false, message = "جميع الحقول مطلوبة" });

        _db.ContactMessages.Add(new Models.ContactMessage
        {
            FullName = fullName,
            Email = email,
            Subject = subject ?? "",
            Message = message
        });

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = "تم إرسال رسالتك بنجاح! سنرد عليك في أقرب وقت" });
    }
}
