using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.ViewModels;
using InnovestHolding.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers;

public class AccountController : Controller
{
    /// <summary>
    /// Verified against when the username does not exist, so an unknown user costs the same BCrypt time
    /// as a wrong password (no timing oracle). Computed once from a random value.
    /// </summary>
    private static readonly Lazy<string> DummyHash =
        new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")));

    private readonly AppDbContext _db;
    private readonly ILoginThrottle _throttle;

    public AccountController(AppDbContext db, ILoginThrottle throttle)
    {
        _db = db;
        _throttle = throttle;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    // Pinned to ONE route (Stage 4 H1): /Account/Login/{id} and /Admin/Account/Login do not reach it.
    [HttpPost("/Account/Login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginInput input)
    {
        // NFKC + trim: the same canonical form for the lookup and for an unknown-name throttle key.
        var username = LoginKey.NormalizeName(input.Username);
        var ip = RateLimitPolicies.ClientIp(HttpContext);

        if (!ModelState.IsValid)
            return LoginView(username, UserMessages.LoginInvalid);

        // Resolve the account first (one indexed query, no BCrypt) so the lockout is counted against the
        // ACCOUNT, not the spelling: every variant the database's case/width-insensitive collation maps to
        // this user shares one key (Stage 4 H2). An unknown name gets a normalised key; same message either way.
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        var key = user is null ? LoginKey.ForUnknownName(username) : LoginKey.ForAccount(user.Id);

        // Locked: no BCrypt.
        if (_throttle.IsLocked(key, ip))
            return LoginView(username, UserMessages.LoginLocked);

        var valid = BCrypt.Net.BCrypt.Verify(input.Password, user?.PasswordHash ?? DummyHash.Value);

        if (user == null || !valid)
        {
            _throttle.RecordFailure(key, ip);
            return LoginView(username, UserMessages.LoginInvalid);
        }

        _throttle.ResetUser(key); // the IP counter is NOT reset (Stage 4 L2)

        await AdminSession.SignInAsync(HttpContext, user);
        return RedirectToAction("Index", "Dashboard", new { area = "" });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private ViewResult LoginView(string username, string error) =>
        View(nameof(Login), new LoginViewModel { Username = username, ErrorMessage = error });
}
