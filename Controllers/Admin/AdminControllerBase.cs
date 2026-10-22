using InnovestHolding.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// Every admin controller derives from this: signed-in users with the Admin role only. Admin requests are always
/// Arabic (their URLs never start with /en), so the messages below come out of the neutral (Arabic) resx.
/// </summary>
[Authorize(Roles = "Admin")]
[RequestSizeLimit(InputLimits.AdminBodyBytes)]
public abstract class AdminControllerBase : Controller
{
    public const int PageSize = 50;

    /// <summary>A resx message for the current request culture.</summary>
    protected string L(string key, params object[] args) =>
        HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>()[key, args].Value;

    /// <summary>All ModelState errors joined into one line, for the PRG list pages (TempData["Error"]).</summary>
    protected string JoinErrors() =>
        string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrEmpty(m)).Distinct());

    /// <summary>
    /// PRG on invalid input for the list pages: the joined ModelState errors go to TempData["Error"] and the browser
    /// is sent back to the controller's Index.
    /// </summary>
    protected IActionResult InvalidRedirect()
    {
        TempData["Error"] = JoinErrors();
        return RedirectToAction("Index");
    }

    /// <summary>Trimmed text, or null when empty/whitespace (optional English columns store NULL, not "").</summary>
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
