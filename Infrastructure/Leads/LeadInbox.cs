using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure.Leads;

/// <summary>One row of the admin applications inbox, whatever table it comes from.</summary>
public sealed class LeadRow
{
    public string Type { get; init; } = "";
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    /// <summary>The line that tells leads apart in the list (organisation, company, event, interest…).</summary>
    public string Summary { get; init; } = "";
    public string Culture { get; init; } = "";
    public bool IsRead { get; init; }
    public LeadStatus Status { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Unread rows per lead type (one query; the admin sidebar and dashboard read it).</summary>
public sealed class UnreadCounts
{
    public int Quick { get; init; }
    public int Forum { get; init; }
    public int Investor { get; init; }
    public int Startup { get; init; }
    public int Sponsorship { get; init; }
    public int Interest { get; init; }
    public int Messages { get; init; }

    /// <summary>Every lead of the applications inbox (contact messages have their own inbox).</summary>
    public int Leads => Quick + Forum + Investor + Startup + Sponsorship + Interest;

    public int Of(string type) => type switch
    {
        LeadInbox.Quick => Quick,
        LeadInbox.Forum => Forum,
        LeadInbox.Investor => Investor,
        LeadInbox.Startup => Startup,
        LeadInbox.Sponsorship => Sponsorship,
        LeadInbox.Interest => Interest,
        _ => 0,
    };
}

/// <summary>
/// The applications inbox: every lead of the public forms in one list (registrations — quick and Forum —, Join the
/// Network applications, sponsorship enquiries, opportunity interests), filtered by type and read state, newest first.
/// The rows stay in their own tables; nothing is copied or moved. Contact messages keep their own inbox («الرسائل»).
/// </summary>
public static class LeadInbox
{
    public const string Quick = Registration.Quick;
    public const string Forum = Registration.Forum;
    public const string Investor = NetworkApplication.Investor;
    public const string Startup = NetworkApplication.Startup;
    public const string Sponsorship = "sponsorship";
    public const string Interest = "interest";

    /// <summary>The types, in the order of the filter, with their Arabic admin labels.</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> Types =
    [
        (Forum, "تسجيل في المنتدى"),
        (Investor, "انضمام — مستثمر"),
        (Startup, "انضمام — شركة ناشئة"),
        (Sponsorship, "استفسار رعاية"),
        (Interest, "اهتمام بالفرص"),
        (Quick, "تسجيل سريع (الرئيسية)"),
    ];

    public static bool IsType(string? type) => type is not null && Types.Any(t => t.Key == type);

    public static string Label(string type) => Types.FirstOrDefault(t => t.Key == type).Label ?? type;

    /// <summary>All leads, or one type, as list rows (a UNION ALL in SQL; paging and counting stay in the database).</summary>
    public static IQueryable<LeadRow> Query(AppDbContext db, string? type)
    {
        IQueryable<LeadRow> Registrations(string kind) => db.Registrations.Where(r => r.Kind == kind).Select(r => new LeadRow
        {
            Type = kind, Id = r.Id, Name = r.FullName, Email = r.Email,
            Summary = r.EventTitle ?? r.Organization ?? r.Interest,
            Phone = r.Phone, Culture = r.Culture, IsRead = r.IsRead, Status = r.Status, Note = r.AdminNote, CreatedAt = r.CreatedAt,
        });
        IQueryable<LeadRow> Network(string kind) => db.NetworkApplications.Where(a => a.Kind == kind).Select(a => new LeadRow
        {
            Type = kind, Id = a.Id, Name = a.FullName, Email = a.Email,
            Summary = a.Organization ?? a.InvestorType ?? "",
            Phone = a.Phone, Culture = a.Culture, IsRead = a.IsRead, Status = a.Status, Note = a.AdminNote, CreatedAt = a.CreatedAt,
        });
        IQueryable<LeadRow> Sponsorships() => db.SponsorshipEnquiries.Select(s => new LeadRow
        {
            Type = Sponsorship, Id = s.Id, Name = s.ContactName, Email = s.Email,
            Summary = s.CompanyName,
            Phone = s.Phone, Culture = s.Culture, IsRead = s.IsRead, Status = s.Status, Note = s.AdminNote, CreatedAt = s.CreatedAt,
        });
        IQueryable<LeadRow> Interests() => db.OpportunityInterests.Select(i => new LeadRow
        {
            Type = Interest, Id = i.Id, Name = i.FullName, Email = i.Email,
            Summary = i.OpportunityTitle ?? "",
            Phone = i.Phone, Culture = i.Culture, IsRead = i.IsRead, Status = i.Status, Note = i.AdminNote, CreatedAt = i.CreatedAt,
        });

        return type switch
        {
            Quick or Forum => Registrations(type),
            Investor or Startup => Network(type),
            Sponsorship => Sponsorships(),
            Interest => Interests(),
            _ => Registrations(Forum).Concat(Registrations(Quick)).Concat(Network(Investor)).Concat(Network(Startup))
                .Concat(Sponsorships()).Concat(Interests()),
        };
    }

    /// <summary>Unread rows per type in one statement; the kind values travel as parameters (the model's constants).</summary>
    public static Task<UnreadCounts> UnreadAsync(AppDbContext db, CancellationToken ct = default) =>
        db.Database.SqlQueryRaw<UnreadCounts>(
            """
            SELECT
              (SELECT COUNT(*) FROM Registrations WHERE IsRead = 0 AND Kind = {0}) AS Quick,
              (SELECT COUNT(*) FROM Registrations WHERE IsRead = 0 AND Kind = {1}) AS Forum,
              (SELECT COUNT(*) FROM NetworkApplications WHERE IsRead = 0 AND Kind = {2}) AS Investor,
              (SELECT COUNT(*) FROM NetworkApplications WHERE IsRead = 0 AND Kind = {3}) AS Startup,
              (SELECT COUNT(*) FROM SponsorshipEnquiries WHERE IsRead = 0) AS Sponsorship,
              (SELECT COUNT(*) FROM OpportunityInterests WHERE IsRead = 0) AS Interest,
              (SELECT COUNT(*) FROM ContactMessages WHERE IsRead = 0) AS Messages
            """, Quick, Forum, Investor, Startup).SingleAsync(ct);
}