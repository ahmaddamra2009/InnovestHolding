using Microsoft.AspNetCore.Mvc;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The old «التسجيلات» screen listed quick and Forum registrations together. Since F11 every form has its own inbox
/// under «الوارد» (/Admin/Leads?type=…), so an old bookmark lands on all the forms' leads. Admin-only like every
/// admin screen.
/// </summary>
public class RegistrationsController : AdminControllerBase
{
    public IActionResult Index() => Redirect("/Admin/Leads");
}
