using System.Security.Claims;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class SettingsController : AdminControllerBase
{
    private const string ViewPath = "~/Views/Admin/Settings/Index.cshtml";
    private readonly AppDbContext _db;
    private readonly ILoginThrottle _throttle;

    public SettingsController(AppDbContext db, ILoginThrottle throttle)
    {
        _db = db;
        _throttle = throttle;
    }

    public IActionResult Index() => View(ViewPath, new ChangePasswordInput());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid)
            return View(ViewPath, new ChangePasswordInput()); // password values are never echoed back

        var id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return Challenge();

        // The current-password check is a password guess like a login: same throttle, same account key.
        var key = LoginKey.ForAccount(user.Id);
        var ip = RateLimitPolicies.ClientIp(HttpContext);
        if (_throttle.IsLocked(key, ip))
        {
            ModelState.AddModelError(nameof(ChangePasswordInput.CurrentPassword), L(MessageKeys.LoginLocked));
            return View(ViewPath, new ChangePasswordInput());
        }

        if (!BCrypt.Net.BCrypt.Verify(input.CurrentPassword, user.PasswordHash))
        {
            _throttle.RecordFailure(key, ip);
            ModelState.AddModelError(nameof(ChangePasswordInput.CurrentPassword), L(MessageKeys.CurrentPasswordWrong));
            return View(ViewPath, new ChangePasswordInput());
        }
        _throttle.ResetUser(key);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
        await _db.SaveChangesAsync();

        // Every other session carries the old password fingerprint and is rejected on its next request;
        // this one is re-issued with the new fingerprint so the admin stays signed in here.
        await AdminSession.SignInAsync(HttpContext, user);

        TempData["Success"] = L(MessageKeys.Admin.PasswordChanged);
        return RedirectToAction(nameof(Index));
    }
}
