using InnovestHolding.Data;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class DashboardController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TotalEvents = await _db.Events.CountAsync(),
            TotalNews = await _db.News.CountAsync(),
            TotalPartners = await _db.Partners.CountAsync(),
            TotalFaqs = await _db.Faqs.CountAsync(),
            TotalPlatforms = await _db.Platforms.CountAsync(),
            TotalNavItems = await _db.NavItems.CountAsync(),
            TotalRegistrations = await _db.Registrations.CountAsync(),
            UnreadRegistrations = await _db.Registrations.CountAsync(r => !r.IsRead),
            TotalMessages = await _db.ContactMessages.CountAsync(),
            UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead)
        };

        return View("~/Views/Admin/Dashboard/Index.cshtml", model);
    }
}
