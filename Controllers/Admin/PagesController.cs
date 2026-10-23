using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The texts of the public pages (one Pages row per route of Infrastructure/Site/SiteRoutes): titles, intro, body,
/// "coming soon" text and its button, SEO, publication, menu/footer visibility and order. A page's URL is code; the
/// admin cannot add or remove a page. The id is the page key, checked against the route table (anything else → 404).
/// The home page renders Content keys, not its row's texts: its form keeps only title, SEO and image, and it is always
/// published and in the menu (posted values for the other fields are ignored).
/// </summary>
public class PagesController(AppDbContext db, ISiteContentCache cache) : AdminControllerBase
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
        var row = await db.Pages.AsNoTracking().Include(p => p.Image).FirstOrDefaultAsync(p => p.Key == page.Key);
        return View(EditView, await VmAsync(page, ToInput(row), row is null));
    }

    /// <summary>The edit model plus the page's sections card (embedded below the form: its own POST forms).</summary>
    private async Task<PageEditVm> VmAsync(SitePage page, PageEditInput input, bool isNew) =>
        new(page, input, isNew, PageSectionRegistry.Of(page.Key).Count == 0 ? null
                : await PageSectionsController.LoadCardAsync(db, cache, page.Key, HttpContext.RequestAborted, embedded: true),
            await db.FooterColumns.AnyAsync(HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Edit(string? id, PageEditInput input)
    {
        if (SiteRoutes.Find(id) is not { } page) return NotFound();
        var row = await db.Pages.Include(p => p.Image).FirstOrDefaultAsync(p => p.Key == page.Key);
        input.Current = MediaRefs.Of(row?.Image);
        var isHome = page.Key == PageKeys.Home;
        var emptyAction = Optional(input.EmptyActionPage);
        if (!isHome && emptyAction is not null && (!SiteRoutes.IsKey(emptyAction) || emptyAction == page.Key))
            ModelState.AddModelError(nameof(PageEditInput.EmptyActionPage), L(MessageKeys.InvalidValueNoField));
        if (!ModelState.IsValid) return View(EditView, await VmAsync(page, input, row is null));
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return View(EditView, await VmAsync(page, input, row is null));

        if (row is null)
        {
            row = new Page { Key = page.Key, SortOrder = await NextOrderAsync(page) };
            db.Pages.Add(row);
        }

        row.TitleAr = input.TitleAr!.Trim();
        row.TitleEn = Optional(input.TitleEn);
        if (!isHome)
        {
            // Home: these fields have no public effect and are not on its form — the stored values stay.
            row.SummaryAr = input.SummaryAr?.Trim() ?? "";
            row.SummaryEn = Optional(input.SummaryEn);
            row.IntroAr = input.IntroAr?.Trim() ?? "";
            row.IntroEn = Optional(input.IntroEn);
            row.BodyAr = Lf(input.BodyAr) ?? "";
            row.BodyEn = Lf(input.BodyEn);
            row.EmptyTextAr = input.EmptyTextAr?.Trim() ?? "";
            row.EmptyTextEn = Optional(input.EmptyTextEn);
            row.EmptyActionPage = emptyAction;
        }
        row.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        row.MetaTitleEn = Optional(input.MetaTitleEn);
        row.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        row.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        row.ShowInMenu = isHome || input.ShowInMenu;
        row.ShowInFooter = isHome || input.ShowInFooter;
        row.IsPublished = isHome || input.IsPublished;
        row.UpdatedAt = DateTime.UtcNow;
        var leaving = ApplyImage(db, row.Image, uploaded, input, m => row.Image = m);

        await SaveWithImageAsync(db, uploaded, leaving); // the interceptor invalidates the site cache
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

    /// <summary>
    /// Publishes or unpublishes the page: an unpublished page (and every page under it) answers the branded 404 and
    /// leaves the menus, footer, cards, links and sitemap. The home page cannot be unpublished.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TogglePublished(string? id)
    {
        if (SiteRoutes.Find(id) is not { } page || page.Key == PageKeys.Home) return NotFound();
        var row = await db.Pages.FirstOrDefaultAsync(p => p.Key == page.Key);
        if (row is null) return NotFound();
        row.IsPublished = !row.IsPublished;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
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

    private static PageEditInput ToInput(Page? p) => p is null
        ? new PageEditInput { ShowInMenu = true, ShowInFooter = true, IsPublished = true }
        : new PageEditInput
        {
            TitleAr = p.TitleAr, TitleEn = p.TitleEn, SummaryAr = p.SummaryAr, SummaryEn = p.SummaryEn,
            IntroAr = p.IntroAr, IntroEn = p.IntroEn, BodyAr = p.BodyAr, BodyEn = p.BodyEn,
            EmptyTextAr = p.EmptyTextAr, EmptyTextEn = p.EmptyTextEn, MetaTitleAr = p.MetaTitleAr, MetaTitleEn = p.MetaTitleEn,
            MetaDescriptionAr = p.MetaDescriptionAr, MetaDescriptionEn = p.MetaDescriptionEn, ShowInMenu = p.ShowInMenu,
            ShowInFooter = p.ShowInFooter, IsPublished = p.IsPublished, EmptyActionPage = p.EmptyActionPage,
            ImageAltAr = p.Image?.AltAr, ImageAltEn = p.Image?.AltEn, Current = MediaRefs.Of(p.Image),
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

    /// <summary>The stored pages of <paramref name="page"/>'s parent in the order Move uses (SortOrder, then route order).</summary>
    public List<string> Siblings(SitePage page) => page.Parent is null
        ? []
        : SiteRoutes.Children(page.Parent).Where(c => Rows.ContainsKey(c.Key))
            .OrderBy(c => Rows[c.Key].SortOrder).Select(c => c.Key).ToList();
}

/// <param name="Sections">The page's «كتل الصفحة» card (Admin/PageSections), null for a page without sections.</param>
/// <param name="FooterCustomised">FooterColumns has rows: «تظهر في الفوتر» then affects only the automatic footer, and
/// the form says so (Hassan C5 F6b).</param>
public sealed record PageEditVm(SitePage Page, PageEditInput Input, bool IsNew, PageSectionsCardVm? Sections = null,
    bool FooterCustomised = false);
