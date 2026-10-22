using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The texts of the public pages (one Pages row per route of Infrastructure/Site/SiteRoutes): titles, intro, body,
/// "coming soon" text, SEO, menu visibility and order. A page's URL is code; the admin cannot add or remove a page.
/// The id is the page key, checked against the route table (anything else → 404).
/// </summary>
public class PagesController(AppDbContext db) : AdminControllerBase
{
    private const string EditView = "~/Views/Admin/Pages/Edit.cshtml";

    public async Task<IActionResult> Index()
    {
        var rows = await db.Pages.AsNoTracking().ToDictionaryAsync(p => p.Key, StringComparer.Ordinal);
        return View("~/Views/Admin/Pages/Index.cshtml", new PagesIndexVm(rows));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (SiteRoutes.Find(id) is not { } page) return NotFound();
        var row = await db.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Key == page.Key);
        return View(EditView, new PageEditVm(page, ToInput(row), row is null));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string? id, PageEditInput input)
    {
        if (SiteRoutes.Find(id) is not { } page) return NotFound();
        var row = await db.Pages.FirstOrDefaultAsync(p => p.Key == page.Key);
        if (!ModelState.IsValid) return View(EditView, new PageEditVm(page, input, row is null));

        if (row is null)
        {
            row = new Page { Key = page.Key, SortOrder = await NextOrderAsync(page) };
            db.Pages.Add(row);
        }

        row.TitleAr = input.TitleAr!.Trim();
        row.TitleEn = Optional(input.TitleEn);
        row.SummaryAr = input.SummaryAr?.Trim() ?? "";
        row.SummaryEn = Optional(input.SummaryEn);
        row.IntroAr = input.IntroAr?.Trim() ?? "";
        row.IntroEn = Optional(input.IntroEn);
        row.BodyAr = Lf(input.BodyAr) ?? "";
        row.BodyEn = Lf(input.BodyEn);
        row.EmptyTextAr = input.EmptyTextAr?.Trim() ?? "";
        row.EmptyTextEn = Optional(input.EmptyTextEn);
        row.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        row.MetaTitleEn = Optional(input.MetaTitleEn);
        row.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        row.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        row.ShowInMenu = page.Key == PageKeys.Home || input.ShowInMenu;
        row.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(); // the interceptor invalidates the site cache
        TempData["Success"] = L(MessageKeys.Admin.PageSaved);
        return RedirectToAction(nameof(Edit), new { id = page.Key });
    }

    /// <summary>Shows or hides the page in the header, the drawer and the footer (the page stays reachable by URL).</summary>
    [HttpPost]
    public async Task<IActionResult> ToggleMenu(string? id)
    {
        if (SiteRoutes.Find(id) is not { } page || page.Key == PageKeys.Home) return NotFound();
        var row = await db.Pages.FirstOrDefaultAsync(p => p.Key == page.Key);
        if (row is null) return NotFound();
        row.ShowInMenu = !row.ShowInMenu;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.PageMenuToggled);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Moves the page one place up (-1) or down (+1) among the pages of the same parent.</summary>
    [HttpPost]
    public async Task<IActionResult> Move(string? id, int direction)
    {
        if (SiteRoutes.Find(id) is not { } page || page.Parent is null || direction is not (-1 or 1)) return NotFound();
        var siblingKeys = SiteRoutes.Children(page.Parent).Select(p => p.Key).ToList();
        var siblings = await db.Pages.Where(p => siblingKeys.Contains(p.Key)).ToListAsync();
        var ordered = siblings.OrderBy(p => p.SortOrder).ThenBy(p => siblingKeys.IndexOf(p.Key)).ToList();
        var index = ordered.FindIndex(p => p.Key == page.Key);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return RedirectToAction(nameof(Index));

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i + 1;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> NextOrderAsync(SitePage page)
    {
        var keys = SiteRoutes.Children(page.Parent ?? "").Select(p => p.Key).ToList();
        return (await db.Pages.Where(p => keys.Contains(p.Key)).MaxAsync(p => (int?)p.SortOrder) ?? 0) + 1;
    }

    private static string? Lf(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\r\n", "\n").Replace('\r', '\n');

    private static PageEditInput ToInput(Page? p) => p is null
        ? new PageEditInput { ShowInMenu = true }
        : new PageEditInput
        {
            TitleAr = p.TitleAr, TitleEn = p.TitleEn, SummaryAr = p.SummaryAr, SummaryEn = p.SummaryEn,
            IntroAr = p.IntroAr, IntroEn = p.IntroEn, BodyAr = p.BodyAr, BodyEn = p.BodyEn,
            EmptyTextAr = p.EmptyTextAr, EmptyTextEn = p.EmptyTextEn, MetaTitleAr = p.MetaTitleAr, MetaTitleEn = p.MetaTitleEn,
            MetaDescriptionAr = p.MetaDescriptionAr, MetaDescriptionEn = p.MetaDescriptionEn, ShowInMenu = p.ShowInMenu,
        };
}

/// <summary>The page tree (route table order) with the stored rows.</summary>
public sealed record PagesIndexVm(IReadOnlyDictionary<string, Page> Rows)
{
    /// <summary>Depth-first: each page followed by its sub-pages, siblings in their stored order.</summary>
    public IEnumerable<(SitePage Page, int Depth)> Tree()
    {
        IEnumerable<(SitePage, int)> Walk(SitePage page, int depth)
        {
            yield return (page, depth);
            foreach (var child in SiteRoutes.Children(page.Key).OrderBy(c => Rows.GetValueOrDefault(c.Key)?.SortOrder ?? 0))
                foreach (var x in Walk(child, depth + 1)) yield return x;
        }
        return Walk(SiteRoutes.Get(PageKeys.Home), 0);
    }
}

public sealed record PageEditVm(SitePage Page, PageEditInput Input, bool IsNew);
