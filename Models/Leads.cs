using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

// The lead tables of the public forms (Join the Network, sponsorship enquiry, opportunity interest). Leads are personal
// data: admin-only, never shown on the public site. Every option text is a SNAPSHOT of the Arabic text at submission
// time (an option edit or delete never rewrites a lead). Culture, ConsentAt, IsRead and CreatedAt are written by the
// server, never bound from a request.

/// <summary>The admin's workflow state of a lead. Stored as a number; the labels come from the resx.</summary>
public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Closed = 2,
}

/// <summary>What the admin inbox reads and changes on every lead table.</summary>
public interface ILead
{
    int Id { get; }
    bool IsRead { get; set; }
    LeadStatus Status { get; set; }
    string? AdminNote { get; set; }
}

public static class LeadLimits
{
    /// <summary>The internal note's column length.</summary>
    public const int NoteMax = 2000;
}

/// <summary>A «Join the Network» application, as an investor or as a startup.</summary>
public class NetworkApplication : ILead
{
    public const string Investor = "investor";
    public const string Startup = "startup";

    public int Id { get; set; }

    /// <summary><see cref="Investor"/> or <see cref="Startup"/>.</summary>
    [Required, MaxLength(10)]
    public string Kind { get; set; } = Investor;

    /// <summary>The investor's name, or the founder's.</summary>
    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string? Phone { get; set; }

    [Required, MaxLength(100)]
    public string Country { get; set; } = "";

    /// <summary>The investor's organisation, or the startup's name.</summary>
    [MaxLength(200)]
    public string? Organization { get; set; }

    [MaxLength(100)]
    public string? InvestorType { get; set; }

    [MaxLength(100)]
    public string? TicketSize { get; set; }

    /// <summary>Investor: the sectors of interest, joined; startup: its sector.</summary>
    [MaxLength(500)]
    public string? Sectors { get; set; }

    [MaxLength(100)]
    public string? Stage { get; set; }

    [MaxLength(100)]
    public string? FundingSought { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(1000)]
    public string? Pitch { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    /// <summary>UTC time the visitor ticked the consent box (required to submit).</summary>
    public DateTime ConsentAt { get; set; }

    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; }

    /// <summary>Where the admin is with this lead (default New); changed only from the admin inbox.</summary>
    public LeadStatus Status { get; set; }

    /// <summary>The admin's internal note (never shown on the public site).</summary>
    [MaxLength(LeadLimits.NoteMax)]
    public string? AdminNote { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>A sponsorship enquiry for the Forum or the TV Program.</summary>
public class SponsorshipEnquiry : ILead
{
    public const string Forum = "forum";
    public const string Tv = "tv";

    public int Id { get; set; }

    /// <summary><see cref="Forum"/> or <see cref="Tv"/>.</summary>
    [Required, MaxLength(10)]
    public string Unit { get; set; } = Tv;

    /// <summary>The sponsorship package chosen, when packages exist (no foreign key yet: the packages table arrives later).</summary>
    public int? PackageId { get; set; }

    [MaxLength(150)]
    public string? PackageTitle { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = "";

    [Required, MaxLength(200)]
    public string ContactName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    public DateTime ConsentAt { get; set; }

    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; }

    /// <summary>Where the admin is with this lead (default New); changed only from the admin inbox.</summary>
    public LeadStatus Status { get; set; }

    /// <summary>The admin's internal note (never shown on the public site).</summary>
    [MaxLength(LeadLimits.NoteMax)]
    public string? AdminNote { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// «Express interest» in investment opportunities. <see cref="OpportunityId"/> is null for an interest in the network's
/// opportunities in general (the form of the opportunities page); it names one opportunity once opportunities are
/// published (no foreign key yet: the opportunities table arrives later).
/// </summary>
public class OpportunityInterest : ILead
{
    public int Id { get; set; }

    public int? OpportunityId { get; set; }

    [MaxLength(200)]
    public string? OpportunityTitle { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    public DateTime ConsentAt { get; set; }

    [MaxLength(5)]
    public string Culture { get; set; } = "ar";

    public bool IsRead { get; set; }

    /// <summary>Where the admin is with this lead (default New); changed only from the admin inbox.</summary>
    public LeadStatus Status { get; set; }

    /// <summary>The admin's internal note (never shown on the public site).</summary>
    [MaxLength(LeadLimits.NoteMax)]
    public string? AdminNote { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
