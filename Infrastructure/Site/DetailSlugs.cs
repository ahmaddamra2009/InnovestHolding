using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// The current slug of the published item a requested slug points to: the slug itself when it is current, the item's
/// new slug when it is an old one (SlugRedirects), else null. The detail actions and the URL normaliser both use it, so
/// an old slug written with capitals or a trailing slash reaches the item in ONE 301.
/// </summary>
public static class DetailSlugs
{
    public static async Task<string?> CurrentAsync(AppDbContext db, DetailRoute route, string slug, CancellationToken ct)
    {
        var current = await Published(db, route, slug, null).FirstOrDefaultAsync(ct);
        if (current is not null) return current;
        var id = await db.SlugRedirects.AsNoTracking()
            .Where(r => r.EntityType == route.EntityType && r.OldSlug == slug)
            .Select(r => (int?)r.EntityId).FirstOrDefaultAsync(ct);
        return id is null ? null : await Published(db, route, null, id).FirstOrDefaultAsync(ct);
    }

    private static IQueryable<string> Published(AppDbContext db, DetailRoute route, string? slug, int? id) => route.EntityType switch
    {
        SlugTypes.News => db.News.AsNoTracking().Where(n => n.IsActive && (slug == null || n.Slug == slug) && (id == null || n.Id == id)).Select(n => n.Slug),
        SlugTypes.Event => db.Events.AsNoTracking().Where(e => e.IsActive && (slug == null || e.Slug == slug) && (id == null || e.Id == id)).Select(e => e.Slug),
        _ => throw new ArgumentOutOfRangeException(nameof(route)),
    };
}
