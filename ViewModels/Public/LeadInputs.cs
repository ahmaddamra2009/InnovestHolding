using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Public;

// The POST bodies of the lead forms (/forms/{name} and /en/forms/{name}). Lengths mirror the columns; option fields are
// ids that the controller checks against the ACTIVE choices of their own list. Kind-specific required fields of the
// Join form are checked by the controller (they depend on Kind). Nothing here binds to an entity.

public abstract class LeadInputBase
{
    [Display(Name = F.FullName)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? FullName { get; set; }

    [Display(Name = F.Email)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    [EmailAddress(ErrorMessage = K.Email)]
    public string? Email { get; set; }

    [Display(Name = F.Phone)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    [RegularExpression(@"^[0-9+\-\s().]*$", ErrorMessage = K.Phone)]
    public string? Phone { get; set; }

    /// <summary>The consent checkbox (value "true"); the server stores the time it was given.</summary>
    [Display(Name = F.Consent)]
    [MustBeTrue]
    public bool Consent { get; set; }

    /// <summary>Honeypot — hidden from people; must stay empty.</summary>
    public string? Website { get; set; }
}

/// <summary>POST /forms/forum-registration.</summary>
public sealed class ForumRegistrationInput : LeadInputBase
{
    [Display(Name = F.Organization)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? Organization { get; set; }

    [Display(Name = F.JobTitle)]
    [StringLength(150, ErrorMessage = K.MaxLength)]
    public string? JobTitle { get; set; }

    [Display(Name = F.Country)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? Country { get; set; }

    /// <summary>Optional; when given, an ACTIVE interest option.</summary>
    [Display(Name = F.Interest)]
    public int? InterestOptionId { get; set; }

    /// <summary>Optional (any event); when given, an ACTIVE event.</summary>
    [Display(Name = F.Event)]
    public int? EventId { get; set; }
}

/// <summary>POST /forms/join — an investor or a startup («Kind»).</summary>
public sealed class JoinInput : LeadInputBase
{
    /// <summary>A link may not carry a quote, an angle bracket or whitespace (it is shown to the admin, and may be a link one day).</summary>
    public const string SafeUrlChars = @"^[^""<>\s]*$";

    [Display(Name = F.ApplicantType)]
    [Required(ErrorMessage = K.Required)]
    [RegularExpression("^(investor|startup)$", ErrorMessage = K.InvalidValue)]
    public string? Kind { get; set; }

    [Display(Name = F.Country)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? Country { get; set; }

    /// <summary>Investor: organisation (optional). Startup: the startup's name (required).</summary>
    [Display(Name = F.Organization)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? Organization { get; set; }

    [Display(Name = F.StartupName)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? StartupName { get; set; }

    // Investor
    [Display(Name = F.InvestorType)]
    public int? InvestorTypeId { get; set; }

    [Display(Name = F.TicketSize)]
    public int? TicketSizeId { get; set; }

    [Display(Name = F.Sectors)]
    [MaxLength(30, ErrorMessage = K.InvalidValue)]
    public List<int>? SectorIds { get; set; }

    [Display(Name = F.LinkedInUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    [RegularExpression(SafeUrlChars, ErrorMessage = K.HttpsUrl)]
    public string? LinkedInUrl { get; set; }

    [Display(Name = F.Message)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? Message { get; set; }

    // Startup
    [Display(Name = F.Sector)]
    public int? SectorId { get; set; }

    [Display(Name = F.Stage)]
    public int? StageId { get; set; }

    [Display(Name = F.FundingSought)]
    public int? FundingId { get; set; }

    [Display(Name = F.WebsiteUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    [RegularExpression(SafeUrlChars, ErrorMessage = K.HttpsUrl)]
    public string? WebsiteUrl { get; set; }

    [Display(Name = F.Pitch)]
    [StringLength(1000, ErrorMessage = K.MaxLength)]
    public string? Pitch { get; set; }
}

/// <summary>POST /forms/sponsorship. FullName is the contact person.</summary>
public sealed class SponsorshipInput : LeadInputBase
{
    [Display(Name = F.CompanyName)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? CompanyName { get; set; }

    [Display(Name = F.Unit)]
    [Required(ErrorMessage = K.Required)]
    [RegularExpression("^(forum|tv)$", ErrorMessage = K.InvalidValue)]
    public string? Unit { get; set; }

    /// <summary>Optional; when given, an active package of the chosen unit.</summary>
    [Display(Name = F.Package)]
    public int? PackageId { get; set; }

    [Display(Name = F.Message)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? Message { get; set; }
}

/// <summary>POST /forms/opportunity-interest. No OpportunityId = interest in the network's opportunities in general.</summary>
public sealed class OpportunityInterestInput : LeadInputBase
{
    [Display(Name = F.Opportunity)]
    public int? OpportunityId { get; set; }

    [Display(Name = F.Message)]
    [StringLength(2000, ErrorMessage = K.MaxLength)]
    public string? Message { get; set; }
}
