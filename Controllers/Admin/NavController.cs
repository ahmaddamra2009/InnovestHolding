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
/// «القائمة والفوتر»: the extra menu links (header switch, active, order) and the footer columns. The footer is
/// automatic (Infrastructure/Site/FooterBuilder) until the admin presses «تخصيص الأعمدة», which copies the automatic
/// footer into FooterColumns/FooterLinks; «إعادة إلى التلقائي» deletes those rows. Sub-items and «has dropdown» were
/// never rendered and are no longer edited (their rows stay in the database).
/// </summary>
public class NavController(AppDbContext db, ISiteContentCache cache) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? tab)
    {
        var vm = new NavAdminVm
        {
            Tab = tab == NavAdminVm.FooterTab ? NavAdminVm.FooterTab : NavAdminVm.MenuTab,
            Items = await db.NavItems.AsNoTracking().OrderBy(n => n.SortOrder).ThenBy(n => n.Id).ToListAsync(),
            Columns = await db.FooterColumns.AsNoTracking()
                .Include(c => c.Links.OrderBy(l => l.SortOrder).ThenBy(l => l.Id))
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync(),
            Pages = await db.Pages.AsNoTracking().Select(p => new { p.Key, p.TitleAr, p.IsPublished }).ToDictionaryAsync(p => p.Key, p => (p.TitleAr, p.IsPublished)),
        };
        if (vm.Columns.Count == 0)
        {
            // The automatic footer as it is now, to show the admin what «تخصيص» will start from.
            var s = await cache.GetAsync(HttpContext.RequestAborted);
            vm.AutomaticPlan = FooterBuilder.Plan(s);
            vm.QuickLinksTitle = s.Content.GetValueOrDefault(FooterBuilder.QuickLinksTitleKey)?.Ar ?? "";
        }
        return View("~/Views/Admin/Nav/Index.cshtml", vm);
    }

    private IActionResult Back(string tab = NavAdminVm.MenuTab, string? anchor = null) =>
        RedirectToAction(nameof(Index), null, new { tab = tab == NavAdminVm.FooterTab ? tab : null }, anchor);

    // ── Extra links (header) ──

    [HttpPost]
    public async Task<IActionResult> Create(NavItemCreateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await db.NavItems.MaxAsync(n => (int?)n.SortOrder) ?? 0;
        db.NavItems.Add(new NavItem
        {
            TitleAr = input.TitleAr!.Trim(),
            TitleEn = Optional(input.TitleEn),
            Href = input.Href!.Trim(),
            SortOrder = maxOrder + 1,
            ShowInHeader = true,
        });
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.NavAdded);
        return Back();
    }

    /// <summary>Names and link only; visibility, header placement and order are their own actions.</summary>
    [HttpPost]
    public async Task<IActionResult> Update(NavItemUpdateInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await db.NavItems.FindAsync(input.Id);
        if (item == null) return NotFound();

        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.Href = input.Href!.Trim();

        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.NavUpdated);
        return Back(anchor: $"row-{item.Id}");
    }

    /// <summary>Active = on the site at all (header if ShowInHeader, and the automatic footer).</summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.NavItems.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return Back(anchor: $"row-{id}");
    }

    /// <summary>Shown in the header and the drawer (the footer is not affected).</summary>
    [HttpPost]
    public async Task<IActionResult> ToggleHeader(int id)
    {
        var item = await db.NavItems.FindAsync(id);
        if (item == null) return NotFound();
        item.ShowInHeader = !item.ShowInHeader;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return Back(anchor: $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        if (await MoveAsync(db.NavItems, id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return Back(anchor: $"row-{id}");
    }

    /// <summary>Deletes the link (and, by cascade, its place in the customised footer columns and its old sub-items).</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.NavItems.Include(n => n.SubItems).FirstOrDefaultAsync(n => n.Id == id);
        if (item != null)
        {
            db.FooterLinks.RemoveRange(await db.FooterLinks.Where(l => l.NavItemId == id).ToListAsync());
            db.NavItems.Remove(item);
            await db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.NavDeleted);
        }
        return Back();
    }

    // ── Footer columns ──

    /// <summary>Copies the automatic footer into rows (so the customised footer starts identical). No-op once customised.</summary>
    [HttpPost]
    public async Task<IActionResult> CustomiseFooter()
    {
        if (!await db.FooterColumns.AnyAsync())
        {
            var s = await cache.GetAsync(HttpContext.RequestAborted);
            var quick = s.Content.GetValueOrDefault(FooterBuilder.QuickLinksTitleKey);
            var order = 0;
            foreach (var plan in FooterBuilder.Plan(s))
            {
                var page = plan.TitlePageKey is null ? null : s.Page(plan.TitlePageKey);
                var (ar, en) = page is not null
                    ? (page.TitleAr, page.TitleEn)
                    : (quick?.Ar ?? "", quick?.En);
                var column = new FooterColumn
                {
                    TitleAr = Truncate(string.IsNullOrWhiteSpace(ar) ? plan.TitlePageKey ?? "—" : ar.Trim(), 100),
                    TitleEn = string.IsNullOrWhiteSpace(en) ? null : Truncate(en.Trim(), 100),
                    SortOrder = ++order,
                };
                var linkOrder = 0;
                foreach (var link in plan.Links)
                    column.Links.Add(new FooterLink { PageKey = link.PageKey, NavItemId = link.NavItemId, SortOrder = ++linkOrder });
                db.FooterColumns.Add(column);
            }
            await db.SaveChangesAsync();
        }
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return Back(NavAdminVm.FooterTab);
    }

    /// <summary>Deletes every customised column (and its links): the footer is automatic again.</summary>
    [HttpPost]
    public async Task<IActionResult> ResetFooter()
    {
        db.FooterColumns.RemoveRange(await db.FooterColumns.ToListAsync());
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return Back(NavAdminVm.FooterTab);
    }

    [HttpPost]
    public async Task<IActionResult> AddColumn(FooterColumnInput input)
    {
        if (!ModelState.IsValid) return InvalidFooter();
        if (!await db.FooterColumns.AnyAsync()) return BadRequest();   // automatic: customise first
        if (await db.FooterColumns.CountAsync() >= FooterBuilder.MaxColumns) return LimitFooter();

        var max = await db.FooterColumns.MaxAsync(c => (int?)c.SortOrder) ?? 0;
        db.FooterColumns.Add(new FooterColumn { TitleAr = input.TitleAr!.Trim(), TitleEn = Optional(input.TitleEn), SortOrder = max + 1 });
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return Back(NavAdminVm.FooterTab);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateColumn(FooterColumnInput input)
    {
        if (!ModelState.IsValid) return InvalidFooter();
        var column = await db.FooterColumns.FindAsync(input.Id);
        if (column == null) return NotFound();
        column.TitleAr = input.TitleAr!.Trim();
        column.TitleEn = Optional(input.TitleEn);
        column.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return Back(NavAdminVm.FooterTab, $"col-{column.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteColumn(int id)
    {
        var column = await db.FooterColumns.FindAsync(id);
        if (column == null) return NotFound();
        db.FooterColumns.Remove(column);   // its links go by cascade
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return Back(NavAdminVm.FooterTab);
    }

    [HttpPost]
    public async Task<IActionResult> MoveColumn(int id, int direction)
    {
        if (await MoveAsync(db.FooterColumns, id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return Back(NavAdminVm.FooterTab, $"col-{id}");
    }

    /// <summary>Adds a page (a route key) or an extra link (an existing NavItem) at the end of a column.</summary>
    [HttpPost]
    public async Task<IActionResult> AddLink(FooterLinkInput input)
    {
        if (!ModelState.IsValid) return InvalidFooter();
        var column = await db.FooterColumns.FindAsync(input.ColumnId);
        if (column == null) return NotFound();

        string? pageKey = null;
        int? navId = null;
        var target = input.Target!.Trim();
        if (target.StartsWith("page:", StringComparison.Ordinal) && SiteRoutes.IsKey(target[5..])) pageKey = target[5..];
        else if (target.StartsWith("nav:", StringComparison.Ordinal) && int.TryParse(target[4..], out var nid)
                 && await db.NavItems.AnyAsync(n => n.Id == nid)) navId = nid;
        else
        {
            ModelState.AddModelError(nameof(input.Target), L(MessageKeys.InvalidValueNoField));
            return InvalidFooter();
        }

        if (await db.FooterLinks.CountAsync(l => l.FooterColumnId == column.Id) >= FooterBuilder.MaxLinks) return LimitFooter();
        var max = await db.FooterLinks.Where(l => l.FooterColumnId == column.Id).MaxAsync(l => (int?)l.SortOrder) ?? 0;
        db.FooterLinks.Add(new FooterLink { FooterColumnId = column.Id, PageKey = pageKey, NavItemId = navId, SortOrder = max + 1 });
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return Back(NavAdminVm.FooterTab, $"col-{column.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var link = await db.FooterLinks.FindAsync(id);
        if (link == null) return NotFound();
        db.FooterLinks.Remove(link);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return Back(NavAdminVm.FooterTab, $"col-{link.FooterColumnId}");
    }

    /// <summary>▲▼ within the link's own column.</summary>
    [HttpPost]
    public async Task<IActionResult> MoveLink(int id, int direction)
    {
        var columnId = await db.FooterLinks.Where(l => l.Id == id).Select(l => (int?)l.FooterColumnId).FirstOrDefaultAsync();
        if (columnId is null || await MoveAsync(db.FooterLinks.Where(l => l.FooterColumnId == columnId), id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return Back(NavAdminVm.FooterTab, $"col-{columnId}");
    }

    private IActionResult InvalidFooter()
    {
        TempData["Error"] = JoinErrors();
        return Back(NavAdminVm.FooterTab);
    }

    private IActionResult LimitFooter()
    {
        TempData["Error"] = L(MessageKeys.AdminLists.FooterLimit, FooterBuilder.MaxColumns, FooterBuilder.MaxLinks);
        return Back(NavAdminVm.FooterTab);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
