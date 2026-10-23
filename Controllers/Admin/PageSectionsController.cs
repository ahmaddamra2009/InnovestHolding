using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using C = InnovestHolding.Infrastructure.Content.ContentItemGroups;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// Show/hide and order the sections of a public page (PageSections; Infrastructure/Site/PageSectionRegistry), plus the
/// home/Forum counts. The id is a page key that has sections (anything else → 404); a section key outside the page's
/// registry → 404; hiding a section that cannot be hidden, or moving a fixed one → 400. Every action is a plain POST
/// form (global antiforgery) that redirects back (PRG); the site cache is invalidated by the save interceptor.
/// </summary>
public class PageSectionsController(AppDbContext db, ISiteContentCache cache) : AdminControllerBase
{
    private const string IndexView = "~/Views/Admin/PageSections/Index.cshtml";

    [HttpGet]
    public async Task<IActionResult> Index(string? id)
    {
        var key = string.IsNullOrEmpty(id) ? PageKeys.Home : id;
        if (PageSectionRegistry.Of(key).Count == 0) return NotFound();
        return View(IndexView, await LoadCardAsync(db, cache, key, HttpContext.RequestAborted));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(string? id, string? section)
    {
        if (Def(id, section) is not { } def) return NotFound();
        if (!def.CanHide) return BadRequest();
        var rows = await RowsAsync(id!);
        var row = rows.Single(r => r.SectionKey == def.Key);
        if (row.IsVisible && rows.Count(r => r.IsVisible) <= 1)
        {
            TempData["Error"] = L(MessageKeys.AdminSections.LastVisible);
            return Back(id!, def.Key);
        }
        row.IsVisible = !row.IsVisible;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(row.IsVisible ? MessageKeys.AdminSections.Shown : MessageKeys.AdminSections.Hidden, def.Label);
        return Back(id!, def.Key);
    }

    [HttpPost]
    public async Task<IActionResult> Move(string? id, string? section, int direction)
    {
        if (Def(id, section) is not { } def || direction is not (-1 or 1)) return NotFound();
        if (!def.Movable) return BadRequest();
        var rows = await RowsAsync(id!);
        var movable = PageSectionRegistry.Of(id!).Where(d => d.Movable).Select(d => d.Key).ToList();
        var rowId = rows.Single(r => r.SectionKey == def.Key).Id;
        var result = await MoveAsync(db.PageSections.Where(r => r.PageKey == id && movable.Contains(r.SectionKey)), rowId, direction);
        if (result is null) return NotFound();
        TempData["Success"] = L(MessageKeys.AdminSections.Moved, def.Label, result.Position, result.Count);
        return Back(id!, def.Key);
    }

    /// <summary>Sets one of the counts of <see cref="PageSectionRegistry.Limits"/> (an invalid value changes nothing).</summary>
    [HttpPost]
    public async Task<IActionResult> SetLimit(string? key, string? value)
    {
        if (PageSectionRegistry.FindLimit(key) is not { } limit) return NotFound();
        if (PageSectionRegistry.ParseLimit(limit, value) is not { } n)
        {
            TempData["Error"] = L(MessageKeys.AdminSections.LimitInvalid, limit.Label, limit.Max);
            return Back(limit.Page, limit.Section);
        }
        var row = await db.SiteContents.FirstOrDefaultAsync(s => s.SectionKey == limit.Key);
        if (row is null)
        {
            row = new SiteContent { SectionKey = limit.Key };
            db.SiteContents.Add(row);
        }
        row.ContentAr = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        row.ContentEn = null;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ContentSaved);
        return Back(limit.Page, limit.Section);
    }

    private static SectionDef? Def(string? page, string? section) =>
        page is null ? null : PageSectionRegistry.Find(page, section);

    private IActionResult Back(string page, string section) =>
        Redirect($"/Admin/PageSections/Index/{page}#section-{section}");

    /// <summary>The page's rows, tracked; a registry section without a row (not possible after AddAdminControl) gets one.</summary>
    private async Task<List<PageSection>> RowsAsync(string page)
    {
        var rows = await db.PageSections.Where(r => r.PageKey == page).ToListAsync();
        var missing = PageSectionRegistry.Of(page).Where(d => !rows.Exists(r => r.SectionKey == d.Key)).ToList();
        if (missing.Count == 0) return rows;
        var next = rows.Count == 0 ? 0 : rows.Max(r => r.SortOrder);
        foreach (var d in missing)
        {
            var row = new PageSection { PageKey = page, SectionKey = d.Key, IsVisible = true, SortOrder = next += 10 };
            db.PageSections.Add(row);
            rows.Add(row);
        }
        await db.SaveChangesAsync();
        return rows;
    }

    /// <summary>
    /// The card of one page (also embedded by Pages/Edit, «كتل الصفحة»): its sections in render order with the admin's
    /// switch, how much content each has on the live site and the counts edited here.
    /// </summary>
    public static async Task<PageSectionsCardVm> LoadCardAsync(AppDbContext db, ISiteContentCache cache, string pageKey, CancellationToken ct)
    {
        var snapshot = await cache.GetAsync(ct);
        var content = await db.SiteContents.AsNoTracking()
            .Where(s => PageSectionRegistry.Limits.Select(l => l.Key).Contains(s.SectionKey))
            .ToDictionaryAsync(s => s.SectionKey, s => s.ContentAr, ct);
        // The live switches (not the cached ones: the screen shows exactly what is stored).
        var stored = await db.PageSections.AsNoTracking().Where(r => r.PageKey == pageKey)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new SectionRow(r.SectionKey, r.IsVisible, r.SortOrder)).ToListAsync(ct);
        var live = snapshot with
        {
            SectionData = new Dictionary<string, IReadOnlyList<SectionRow>>(StringComparer.Ordinal) { [pageKey] = stored },
        };
        // Fixed sections first (they never move: e.g. the hero figures), then the movable ones in render order.
        var layout = PageSectionRegistry.Layout(live, pageKey).OrderBy(x => x.Def.Movable).ToList();
        var movable = layout.Where(x => x.Def.Movable).Select(x => x.Def.Key).ToList();
        var rows = layout.Select(x =>
        {
            var limit = PageSectionRegistry.Limits.FirstOrDefault(l => l.Page == pageKey && l.Section == x.Def.Key);
            return new PageSectionRowVm(x.Def, x.Visible, ContentCount(snapshot, pageKey, x.Def.Key),
                x.Def.Movable ? movable.IndexOf(x.Def.Key) + 1 : 0, movable.Count,
                limit, limit is null ? null : PageSectionRegistry.ParseLimit(limit, content.GetValueOrDefault(limit.Key)) ?? limit.Default);
        }).ToList();
        var title = snapshot.Page(pageKey)?.TitleAr ?? pageKey;
        return new PageSectionsCardVm(pageKey, title, SiteRoutes.PathFor(SiteRoutes.Get(pageKey), false), rows);
    }

    /// <summary>
    /// How many items a section has on the live site (null = not a list: always has content). 0 = the section is
    /// hidden automatically whatever its switch.
    /// </summary>
    internal static int? ContentCount(SiteSnapshot s, string page, string section)
    {
        var v = new SiteViews(s, false);
        int Items(string group) => s.ItemsOf(group).Count;
        return (page, section) switch
        {
            (_, "hero_stats" or "stats") => s.Stats.Count,
            (PageKeys.Home, "ecosystem") or (PageKeys.Ecosystem, "units") => v.Cards(PageKeys.Ecosystem, false).Count,
            (_, "units") => v.Cards(page, false).Count,
            (PageKeys.Home, "platforms") or (PageKeys.Ecosystem, "services") => s.Platforms.Count,
            (PageKeys.Home or PageKeys.Forum, "events") => s.Events.Count,
            (PageKeys.Home, "news") => s.News.Count,
            (PageKeys.Home, "partners") => s.Partners.Count(p => p.ShowOnHome),
            (_, "faq") => s.Faqs.Count,
            (PageKeys.About, "values") => Items(C.Values),
            (PageKeys.About, "why") => Items(C.WhyPoints),
            (_, "sectors") => Items(C.Sectors),
            (PageKeys.About, "partners_in_success") => Items(C.PartnersInSuccess),
            (PageKeys.About, "founder") => string.IsNullOrWhiteSpace(s.Content.GetValueOrDefault("founder_message")?.Ar) ? 0 : null,
            (PageKeys.Ecosystem, "pillars") => Items(C.EcosystemPillars),
            (PageKeys.Ecosystem, "programs") => Items(C.Programs),
            (PageKeys.Ecosystem, "government") => Items(C.GovernmentServices),
            (PageKeys.Ecosystem, "universities") => Items(C.UniversityServices),
            (PageKeys.Forum, "components") => Items(C.ForumComponents),
            (_, "investor_services") => Items(C.InvestorServices),
            (PageKeys.Network, "steps") => Items(C.NetworkSteps),
            (PageKeys.NetworkStartups, "startup_services") => Items(C.StartupServices),
            (PageKeys.ForumPartners, "partner_types") => Items(C.PartnerTypes),
            (PageKeys.ForumPartners, "packages") => s.Ecosystem.PackagesOf(SponsorshipUnits.Forum).Count,
            (PageKeys.TvSponsorship, "packages") => s.Ecosystem.PackagesOf(SponsorshipUnits.Tv).Count,
            _ => null,
        };
    }
}
