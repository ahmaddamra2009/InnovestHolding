using System.Diagnostics;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using InnovestHolding.ViewModels.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    // Pinned to ONE route (Stage 4 H1): /Home/Register/{id} and /Admin/Home/Register do not reach it.
    [HttpPost("/Home/Register")]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Register(RegistrationInput input)
    {
        // Honeypot filled: answer exactly like a success and write nothing (do not tip off the bot).
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = UserMessages.RegistrationSuccess });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        _db.Registrations.Add(new Registration
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = input.Phone?.Trim() ?? "",
            Interest = input.Interest!.Trim()
        });

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = UserMessages.RegistrationSuccess });
    }

    [HttpPost("/Home/Contact")]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Contact(ContactInput input)
    {
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = UserMessages.ContactSuccess });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        _db.ContactMessages.Add(new ContactMessage
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Subject = input.Subject!.Trim(),
            Message = input.Message!.Trim()
        });

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = UserMessages.ContactSuccess });
    }

    /// <summary>
    /// Target of UseExceptionHandler. The handler re-executes the ORIGINAL method, so a failed POST
    /// arrives here as a POST without a valid token — hence the only [IgnoreAntiforgeryToken] in the app.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private string FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? string.Format(UserMessages.InvalidValue, "");
}
