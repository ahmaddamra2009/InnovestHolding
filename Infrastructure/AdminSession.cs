using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// The admin cookie session. The principal carries a fingerprint of the account's CURRENT password hash;
/// every authenticated request re-reads the hash and rejects the cookie when it no longer matches, so a
/// password change ends every other session (Stage 4 L3) — without a schema change.
/// </summary>
public static class AdminSession
{
    public const string PasswordStampClaim = "innovest:pwd";

    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, "Admin"),
            new(PasswordStampClaim, PasswordStamp(user.PasswordHash))
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static Task SignInAsync(HttpContext context, AppUser user) =>
        context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = true });

    /// <summary>SHA-256 of the BCrypt hash string, base64. The cookie is encrypted; this is a hash of a hash.</summary>
    public static string PasswordStamp(string passwordHash) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));

    /// <summary><c>CookieAuthenticationEvents.OnValidatePrincipal</c>: one indexed lookup per admin request.</summary>
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var stamp = principal?.FindFirst(PasswordStampClaim)?.Value;
        var idText = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        string? hash = null;
        if (stamp is not null && int.TryParse(idText, out var id))
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            hash = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.PasswordHash).FirstOrDefaultAsync();
        }

        if (hash is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(PasswordStamp(hash)), Encoding.ASCII.GetBytes(stamp!)))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
