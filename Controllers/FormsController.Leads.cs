using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Forms;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;

namespace InnovestHolding.Controllers;

// The lead forms. Same rules as Register/Contact: exact attribute routes (Arabic + /en, one rate-limit bucket per action),
// the global antiforgery filter, a 32 KB body cap, the honeypot answered like a success with nothing written, a 400 JSON
// with every field error in the page language and in the form's field order (LeadFieldOrder), option ids checked against the ACTIVE choices of their own list, option
// texts stored as Arabic snapshots, and the server writing Culture, ConsentAt and CreatedAt. Nothing is logged.
public partial class FormsController
{
    private IStringLocalizer<SharedResource> Loc => HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    private JsonResult Success(string key) => Json(new { success = true, message = Loc[key].Value });

    /// <summary>
    /// The form's global window is full (<see cref="LeadVolumeGuard"/>): a JSON 429 that names another channel, the
    /// contact email the admin manages (SiteContents <c>contact_email</c>); the generic 429 text when none is set.
    /// </summary>
    private async Task<ObjectResult> FullAsync()
    {
        var snapshot = await HttpContext.RequestServices.GetRequiredService<ISiteContentCache>().GetAsync(HttpContext.RequestAborted);
        var email = snapshot.Content.GetValueOrDefault(ContactEmailKey)?.Ar?.Trim();
        var message = string.IsNullOrEmpty(email) ? Loc[MessageKeys.TooManyRequests].Value : Loc[MessageKeys.LeadFormFull, email].Value;
        return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message });
    }

    /// <summary>The SiteContents key of the public contact email (language-neutral; shown in the footer and contact page).</summary>
    private const string ContactEmailKey = "contact_email";

    private bool Admit(string form) => HttpContext.RequestServices.GetRequiredService<LeadVolumeGuard>().TryAcquire(form);

    /// <summary>400 with one message; <paramref name="field"/> names the input to focus (the form field's name).</summary>
    private BadRequestObjectResult Fail(string message, string? field) => BadRequest(new { success = false, message, field });

    /// <summary>
    /// 400 with every model-state error, one per field, in the order the form shows its fields (<paramref name="order"/>,
    /// <see cref="LeadFieldOrder"/>): <c>errors</c> = [{ field, message }]; <c>message</c> / <c>field</c> repeat the first
    /// (the page focuses it). A key the order does not know (none today) goes last.
    /// </summary>
    private BadRequestObjectResult Invalid(IReadOnlyList<string> order)
    {
        int Rank(string key)
        {
            var name = key.Split('[', '.')[0];
            for (var i = 0; i < order.Count; i++)
                if (string.Equals(order[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return int.MaxValue;
        }

        var errors = ModelState
            .Select(e => (Field: e.Key, Message: e.Value?.Errors.Select(x => x.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m))))
            .Where(e => e.Message is not null)
            .OrderBy(e => Rank(e.Field)) // stable: equal ranks keep the model-state order
            .Select(e => new { field = e.Field, message = e.Message! })
            .ToList();
        if (errors.Count == 0) return Fail(FirstError(), null);
        return BadRequest(new { success = false, message = errors[0].message, field = errors[0].field, errors });
    }

    private BadRequestObjectResult BadChoice(string fieldKey, string field) =>
        Fail(Loc[MessageKeys.OptionInvalid, Loc[fieldKey].Value].Value, field);

    /// <summary>Records a required-field error the attributes cannot express (it depends on another field).</summary>
    private void Missing(string fieldKey, string field) =>
        ModelState.AddModelError(field, Loc[MessageKeys.Required, Loc[fieldKey].Value].Value);

    [HttpPost("/forms/forum-registration")]
    [HttpPost("/en/forms/forum-registration")]
    [PublishedFormPage(PageKeys.ForumRegistration, "form")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> ForumRegistration(ForumRegistrationInput input, [FromServices] AppDbContext data,
        [FromServices] ISiteContentCache content)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.ForumRegistrationSuccess);
        if (!ModelState.IsValid) return Invalid(LeadFieldOrder.ForumRegistration);

        var ct = HttpContext.RequestAborted;
        InterestRow? option = null;
        if (input.InterestOptionId is { } optionId)
        {
            option = (await content.GetAsync(ct)).Interests.FirstOrDefault(o => o.Id == optionId);
            if (option is null) return BadChoice(F.Interest, nameof(input.InterestOptionId));
        }
        FormChoice? evt = null;
        if (input.EventId is not null)
        {
            evt = LeadFormChoices.Find(await LeadFormChoices.EventsAsync(data, ct), input.EventId);
            if (evt is null) return BadChoice(F.Event, nameof(input.EventId));
        }

        var now = DateTime.UtcNow;
        data.Registrations.Add(new Registration
        {
            Kind = Registration.Forum,
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = input.Phone?.Trim() ?? "",
            InterestOptionId = option?.Id,
            Interest = LeadText.Snap(option?.NameAr, 100) ?? "",
            EventId = evt?.Id,
            EventTitle = LeadText.Snap(evt?.TextAr, 200),
            Organization = LeadText.Optional(input.Organization),
            JobTitle = LeadText.Optional(input.JobTitle),
            Country = LeadText.Optional(input.Country),
            ConsentAt = now,
            Culture = Cultures.CurrentName,
            CreatedAt = now,
        });
        if (!Admit(nameof(ForumRegistration))) return await FullAsync();
        await data.SaveChangesAsync(ct);
        return Success(MessageKeys.ForumRegistrationSuccess);
    }

    [HttpPost("/forms/join")]
    [HttpPost("/en/forms/join")]
    [PublishedFormPage(PageKeys.NetworkJoin, "form")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Join(JoinInput input, [FromServices] AppDbContext data)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.JoinSuccess);

        var investor = input.Kind == NetworkApplication.Investor;
        var choices = await LeadFormChoices.LoadAsync(data, HttpContext.RequestAborted);
        // The fields of the OTHER type are ignored (the page shows only one set; without JavaScript both are posted).
        // A choice list is required only while it has active choices (the page does not offer an empty list).
        // Recorded with the attribute errors, so the reply lists them all in field order.
        if (investor && input.InvestorTypeId is null && choices.InvestorTypes.Count > 0)
            Missing(F.InvestorType, nameof(input.InvestorTypeId));
        if (input.Kind == NetworkApplication.Startup)
        {
            if (string.IsNullOrWhiteSpace(input.StartupName)) Missing(F.StartupName, nameof(input.StartupName));
            if (input.SectorId is null && choices.Sectors.Count > 0) Missing(F.Sector, nameof(input.SectorId));
            if (input.StageId is null && choices.Stages.Count > 0) Missing(F.Stage, nameof(input.StageId));
        }
        if (!ModelState.IsValid) return Invalid(LeadFieldOrder.Join);

        var now = DateTime.UtcNow;
        var row = new NetworkApplication
        {
            Kind = investor ? NetworkApplication.Investor : NetworkApplication.Startup,
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = LeadText.Optional(input.Phone),
            Country = input.Country!.Trim(),
            ConsentAt = now,
            Culture = Cultures.CurrentName,
            CreatedAt = now,
        };

        if (investor)
        {
            var type = LeadFormChoices.Find(choices.InvestorTypes, input.InvestorTypeId);
            if (input.InvestorTypeId is not null && type is null) return BadChoice(F.InvestorType, nameof(input.InvestorTypeId));
            var ticket = LeadFormChoices.Find(choices.TicketSizes, input.TicketSizeId);
            if (input.TicketSizeId is not null && ticket is null) return BadChoice(F.TicketSize, nameof(input.TicketSizeId));
            var sectors = new List<FormChoice>();
            foreach (var id in (input.SectorIds ?? []).Distinct())
            {
                if (LeadFormChoices.Find(choices.Sectors, id) is not { } sector) return BadChoice(F.Sectors, nameof(input.SectorIds));
                sectors.Add(sector);
            }
            row.Organization = LeadText.Optional(input.Organization);
            row.InvestorType = LeadText.Snap(type?.TextAr, 100);
            row.TicketSize = LeadText.Snap(ticket?.TextAr, 100);
            row.Sectors = LeadText.Snap(string.Join("، ", sectors.Select(s => s.TextAr.Trim())), 500);
            row.LinkedInUrl = LeadText.Optional(input.LinkedInUrl);
            row.Message = LeadText.Optional(input.Message);
        }
        else
        {
            var sector = LeadFormChoices.Find(choices.Sectors, input.SectorId);
            if (input.SectorId is not null && sector is null) return BadChoice(F.Sector, nameof(input.SectorId));
            var stage = LeadFormChoices.Find(choices.Stages, input.StageId);
            if (input.StageId is not null && stage is null) return BadChoice(F.Stage, nameof(input.StageId));
            var funding = LeadFormChoices.Find(choices.FundingRanges, input.FundingId);
            if (input.FundingId is not null && funding is null) return BadChoice(F.FundingSought, nameof(input.FundingId));
            row.Organization = input.StartupName!.Trim();
            row.Sectors = LeadText.Snap(sector?.TextAr, 500);
            row.Stage = LeadText.Snap(stage?.TextAr, 100);
            row.FundingSought = LeadText.Snap(funding?.TextAr, 100);
            row.WebsiteUrl = LeadText.Optional(input.WebsiteUrl);
            row.Pitch = LeadText.Optional(input.Pitch);
        }

        if (!Admit(nameof(Join))) return await FullAsync();
        data.NetworkApplications.Add(row);
        await data.SaveChangesAsync(HttpContext.RequestAborted);
        return Success(MessageKeys.JoinSuccess);
    }

    [HttpPost("/forms/sponsorship")]
    [HttpPost("/en/forms/sponsorship")]
    [PublishedFormPage(PageKeys.TvSponsorship, "form")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Sponsorship(SponsorshipInput input, [FromServices] AppDbContext data,
        [FromServices] ILeadReferences references)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.SponsorshipSuccess);
        if (!ModelState.IsValid) return Invalid(LeadFieldOrder.Sponsorship);

        FormChoice? package = null;
        if (input.PackageId is not null)
        {
            package = LeadFormChoices.Find(await references.PackagesAsync(input.Unit!, HttpContext.RequestAborted), input.PackageId);
            if (package is null) return BadChoice(F.Package, nameof(input.PackageId));
        }

        if (!Admit(nameof(Sponsorship))) return await FullAsync();
        var now = DateTime.UtcNow;
        data.SponsorshipEnquiries.Add(new SponsorshipEnquiry
        {
            Unit = input.Unit!,
            PackageId = package?.Id,
            PackageTitle = LeadText.Snap(package?.TextAr, 150),
            CompanyName = input.CompanyName!.Trim(),
            ContactName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = LeadText.Optional(input.Phone),
            Message = LeadText.Optional(input.Message),
            ConsentAt = now,
            Culture = Cultures.CurrentName,
            CreatedAt = now,
        });
        await data.SaveChangesAsync(HttpContext.RequestAborted);
        return Success(MessageKeys.SponsorshipSuccess);
    }

    [HttpPost("/forms/opportunity-interest")]
    [HttpPost("/en/forms/opportunity-interest")]
    [PublishedFormPage(PageKeys.NetworkOpportunities, "interest_form")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> OpportunityInterest(OpportunityInterestInput input, [FromServices] AppDbContext data,
        [FromServices] ILeadReferences references)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.InterestSuccess);
        if (!ModelState.IsValid) return Invalid(LeadFieldOrder.OpportunityInterest);

        FormChoice? opportunity = null;
        if (input.OpportunityId is { } id)
        {
            opportunity = await references.OpenOpportunityAsync(id, HttpContext.RequestAborted);
            if (opportunity is null) return BadChoice(F.Opportunity, nameof(input.OpportunityId));
        }

        if (!Admit(nameof(OpportunityInterest))) return await FullAsync();
        var now = DateTime.UtcNow;
        data.OpportunityInterests.Add(new OpportunityInterest
        {
            OpportunityId = opportunity?.Id,
            OpportunityTitle = LeadText.Snap(opportunity?.TextAr, 200),
            FullName = input.FullName!.Trim(),
            Email = input.Email!.Trim(),
            Phone = LeadText.Optional(input.Phone),
            Message = LeadText.Optional(input.Message),
            ConsentAt = now,
            Culture = Cultures.CurrentName,
            CreatedAt = now,
        });
        await data.SaveChangesAsync(HttpContext.RequestAborted);
        return Success(MessageKeys.InterestSuccess);
    }
}
