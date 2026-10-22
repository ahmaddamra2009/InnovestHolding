using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure.Forms;

/// <summary>A choice of a public form (an active ContentItem, an active event, a package…) in both languages.</summary>
public sealed record FormChoice(int Id, string TextAr, string? TextEn);

/// <summary>
/// The choices the lead forms offer, read from the database (two small queries; the lead pages are not on the cached
/// snapshot because its item rows carry no id). The POST validates against the same lists, so a choice the page could
/// not show (inactive, another group, unknown id) is refused.
/// </summary>
public sealed record LeadFormChoices(
    IReadOnlyList<FormChoice> Events,
    IReadOnlyList<FormChoice> InvestorTypes,
    IReadOnlyList<FormChoice> TicketSizes,
    IReadOnlyList<FormChoice> Sectors,
    IReadOnlyList<FormChoice> Stages,
    IReadOnlyList<FormChoice> FundingRanges)
{
    private static readonly string[] Groups =
    [
        ContentItemGroups.InvestorTypes, ContentItemGroups.TicketSizes, ContentItemGroups.Sectors,
        ContentItemGroups.StartupStages, ContentItemGroups.FundingRanges,
    ];

    public static async Task<LeadFormChoices> LoadAsync(AppDbContext db, CancellationToken ct)
    {
        var items = await db.ContentItems.AsNoTracking()
            .Where(i => i.IsActive && Groups.Contains(i.Group))
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new { i.Group, Choice = new FormChoice(i.Id, i.TextAr, i.TextEn) })
            .ToListAsync(ct);
        var events = await EventsAsync(db, ct);

        IReadOnlyList<FormChoice> Of(string group) => items.Where(i => i.Group == group).Select(i => i.Choice).ToList();
        return new LeadFormChoices(events, Of(ContentItemGroups.InvestorTypes), Of(ContentItemGroups.TicketSizes),
            Of(ContentItemGroups.Sectors), Of(ContentItemGroups.StartupStages), Of(ContentItemGroups.FundingRanges));
    }

    /// <summary>The active events (the Forum registration form needs only these: one query).</summary>
    public static async Task<IReadOnlyList<FormChoice>> EventsAsync(AppDbContext db, CancellationToken ct) =>
        await db.Events.AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Select(e => new FormChoice(e.Id, e.TitleAr, e.TitleEn))
            .ToListAsync(ct);

    /// <summary>The choice with this id in <paramref name="list"/>, or null (also for a null id).</summary>
    public static FormChoice? Find(IReadOnlyList<FormChoice> list, int? id) => id is { } v ? list.FirstOrDefault(c => c.Id == v) : null;
}

/// <summary>
/// The records a lead can refer to that a LATER phase publishes: investment opportunities and sponsorship packages. Until
/// that phase replaces the registration, <see cref="NoLeadReferences"/> knows none, so a posted opportunity or package id
/// is refused and the forms show no package choice.
/// </summary>
public interface ILeadReferences
{
    /// <summary>A published opportunity that is open for interest, or null.</summary>
    Task<FormChoice?> OpenOpportunityAsync(int id, CancellationToken ct);

    /// <summary>The active sponsorship packages of a unit (<c>forum</c> / <c>tv</c>), in display order.</summary>
    Task<IReadOnlyList<FormChoice>> PackagesAsync(string unit, CancellationToken ct);
}

public sealed class NoLeadReferences : ILeadReferences
{
    public Task<FormChoice?> OpenOpportunityAsync(int id, CancellationToken ct) => Task.FromResult<FormChoice?>(null);

    public Task<IReadOnlyList<FormChoice>> PackagesAsync(string unit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<FormChoice>>([]);
}

public static class LeadText
{
    /// <summary>
    /// A snapshot of an option text cut to its column (an admin may write a longer option than the lead column holds;
    /// the snapshot is informational, the submission must not fail on it).
    /// </summary>
    public static string? Snap(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim();
        return t.Length <= max ? t : t[..max];
    }

    /// <summary>Trimmed text, or null when empty.</summary>
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
