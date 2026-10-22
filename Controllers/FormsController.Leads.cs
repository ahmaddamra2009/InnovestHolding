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
// with the first message in the page language, option ids checked against the ACTIVE choices of their own list, option
// texts stored as Arabic snapshots, and the server writing Culture, ConsentAt and CreatedAt. Nothing is logged.
public partial class FormsController
{
    private IStringLocalizer<SharedResource> Loc => HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    private JsonResult Success(string key) => Json(new { success = true, message = Loc[key].Value });

    /// <summary>400 with one message; <paramref name="field"/> names the input to focus (the form field's name).</summary>
    private BadRequestObjectResult Fail(string message, string? field) => BadRequest(new { success = false, message, field });

    /// <summary>The first model-state error and the field it belongs to.</summary>
    private BadRequestObjectResult Invalid()
    {
        var entry = ModelState.Where(e => e.Value is { Errors.Count: > 0 }).Select(e => (e.Key, e.Value!.Errors[0].ErrorMessage))
            .FirstOrDefault(e => !string.IsNullOrEmpty(e.ErrorMessage));
        return Fail(string.IsNullOrEmpty(entry.ErrorMessage) ? FirstError() : entry.ErrorMessage, entry.Key);
    }

    private BadRequestObjectResult BadChoice(string fieldKey, string field) =>
        Fail(Loc[MessageKeys.OptionInvalid, Loc[fieldKey].Value].Value, field);

    private BadRequestObjectResult Missing(string fieldKey, string field) =>
        Fail(Loc[MessageKeys.Required, Loc[fieldKey].Value].Value, field);

    [HttpPost("/forms/forum-registration")]
    [HttpPost("/en/forms/forum-registration")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> ForumRegistration(ForumRegistrationInput input, [FromServices] AppDbContext data,
        [FromServices] ISiteContentCache content)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.ForumRegistrationSuccess);
        if (!ModelState.IsValid) return Invalid();

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
            evt = LeadFormChoices.Find((await LeadFormChoices.LoadAsync(data, ct)).Events, input.EventId);
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
        await data.SaveChangesAsync(ct);
        return Success(MessageKeys.ForumRegistrationSuccess);
    }

    [HttpPost("/forms/join")]
    [HttpPost("/en/forms/join")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Join(JoinInput input, [FromServices] AppDbContext data)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.JoinSuccess);
        if (!ModelState.IsValid) return Invalid();

        var investor = input.Kind == NetworkApplication.Investor;
        var choices = await LeadFormChoices.LoadAsync(data, HttpContext.RequestAborted);
        // The fields of the OTHER type are ignored (the page shows only one set; without JavaScript both are posted).
        // A choice list is required only while it has active choices (the page does not offer an empty list).
        if (investor && input.InvestorTypeId is null && choices.InvestorTypes.Count > 0)
            return Missing(F.InvestorType, nameof(input.InvestorTypeId));
        if (!investor)
        {
            if (string.IsNullOrWhiteSpace(input.StartupName)) return Missing(F.StartupName, nameof(input.StartupName));
            if (input.SectorId is null && choices.Sectors.Count > 0) return Missing(F.Sector, nameof(input.SectorId));
            if (input.StageId is null && choices.Stages.Count > 0) return Missing(F.Stage, nameof(input.StageId));
        }

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

        data.NetworkApplications.Add(row);
        await data.SaveChangesAsync(HttpContext.RequestAborted);
        return Success(MessageKeys.JoinSuccess);
    }

    [HttpPost("/forms/sponsorship")]
    [HttpPost("/en/forms/sponsorship")]
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> Sponsorship(SponsorshipInput input, [FromServices] AppDbContext data,
        [FromServices] ILeadReferences references)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.SponsorshipSuccess);
        if (!ModelState.IsValid) return Invalid();

        FormChoice? package = null;
        if (input.PackageId is not null)
        {
            package = LeadFormChoices.Find(await references.PackagesAsync(input.Unit!, HttpContext.RequestAborted), input.PackageId);
            if (package is null) return BadChoice(F.Package, nameof(input.PackageId));
        }

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
    [JsonFormEndpoint]
    [EnableRateLimiting(RateLimitPolicies.PublicForm)]
    [RequestSizeLimit(InputLimits.PublicBodyBytes)]
    public async Task<IActionResult> OpportunityInterest(OpportunityInterestInput input, [FromServices] AppDbContext data,
        [FromServices] ILeadReferences references)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Success(MessageKeys.InterestSuccess);
        if (!ModelState.IsValid) return Invalid();

        FormChoice? opportunity = null;
        if (input.OpportunityId is { } id)
        {
            opportunity = await references.OpenOpportunityAsync(id, HttpContext.RequestAborted);
            if (opportunity is null) return BadChoice(F.Opportunity, nameof(input.OpportunityId));
        }

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
