using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers.Admin;

/// <summary>Every admin controller derives from this: signed-in users with the Admin role only.</summary>
[Authorize(Roles = "Admin")]
public abstract class AdminControllerBase : Controller
{
    /// <summary>All ModelState errors joined into one line, for the PRG list pages (TempData["Error"]).</summary>
    protected string JoinErrors() =>
        string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrEmpty(m)).Distinct());

    /// <summary>
    /// PRG on invalid input for the list pages (Nav, Partners, Platforms, Stats): the joined ModelState
    /// errors go to TempData["Error"] and the browser is sent back to the controller's Index.
    /// </summary>
    protected IActionResult InvalidRedirect()
    {
        TempData["Error"] = JoinErrors();
        return RedirectToAction("Index");
    }
}
