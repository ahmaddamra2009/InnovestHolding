using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Controllers;

/// <summary>
/// The public form endpoints (POST only, JSON replies for fetch()). Off the page URLs so /en/contact can be a page:
/// /forms/{name} (Arabic) and /en/forms/{name} (English). /Home/Register and /Home/Contact are kept for one release so a
/// page cached before the deploy still posts. Exact attribute routes only (no {id}, no /Admin/… variant); every URL of
/// an action shares its rate-limit bucket (the partition is the ACTION, RateLimitPolicies.ActionKey). Antiforgery is
/// the global filter. The lead forms (Forum registration, Join, sponsorship, opportunity interest) are in
/// FormsController.Leads.cs.
/// </summary>
public partial class FormsController(
    AppDbContext db,
    ISiteContentCache cache,
    IStringLocalizer<SharedResource> l) : Controller
{
    [HttpPost("/forms/register")]
    [HttpPost("/en/forms/register")]
    [HttpPost("/Home/Register")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Register(RegistrationInput input)
    {
        // Honeypot filled: answer exactly like a success and write nothing (do not tip off the bot).
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = l[MessageKeys.RegistrationSuccess].Value });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        // The interest must be one of the ACTIVE options; when none is active the field must be absent.
        var options = (await cache.GetAsync(HttpContext.RequestAborted)).Interests;
        InterestRow? option = null;
        if (options.Count > 0)
        {
            option = options.FirstOrDefault(o => o.Id == input.InterestOptionId);
            if (option is null)
                return BadRequest(new { success = false, message = l[MessageKeys.InterestInvalid].Value });
        }
        else if (input.InterestOptionId is not null)
        {
            return BadRequest(new { success = false, message = l[MessageKeys.InterestInvalid].Value });
        }

        db.Registrations.Add(new Registration
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = input.Phone?.Trim() ?? "",
            InterestOptionId = option?.Id,
            Interest = option?.NameAr ?? "",
            Culture = Cultures.CurrentName,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Json(new { success = true, message = l[MessageKeys.RegistrationSuccess].Value });
    }

    [HttpPost("/forms/contact")]
    [HttpPost("/en/forms/contact")]
    [HttpPost("/Home/Contact")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Contact(ContactInput input)
    {
        if (!string.IsNullOrEmpty(input.Website))
            return Json(new { success = true, message = l[MessageKeys.ContactSuccess].Value });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstError() });

        db.ContactMessages.Add(new ContactMessage
        {
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Subject = input.Subject!.Trim(),
            Message = input.Message!.Trim(),
            Culture = Cultures.CurrentName,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Json(new { success = true, message = l[MessageKeys.ContactSuccess].Value });
    }

    private string FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? l[MessageKeys.InvalidValueNoField].Value;
}
