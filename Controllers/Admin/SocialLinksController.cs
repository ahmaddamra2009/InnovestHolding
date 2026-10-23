using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// The social links (footer and contact block). A known network (<see cref="SocialLink.Networks"/>: brand name and glyph
/// from code) appears once; «شبكة أخرى» (<see cref="SocialLink.Custom"/>) carries the admin's plain-text label and an icon
/// KEY of the code whitelist — never admin SVG. An empty URL hides the link. Known rows are hidden, not deleted.
/// </summary>
public class SocialLinksController(AppDbContext db) : AdminControllerBase
{
    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/SocialLinks/Index.cshtml",
            await db.SocialLinks.AsNoTracking().OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(SocialLinkCreateInput input)
    {
        var network = input.Network?.Trim() ?? "";
        var custom = network == SocialLink.Custom;
        if (!custom && !SocialLink.Networks.Contains(network))
            ModelState.AddModelError(nameof(input.Network), L(MessageKeys.InvalidValueNoField));
        else if (!custom && await db.SocialLinks.AnyAsync(s => s.Network == network))
            ModelState.AddModelError(nameof(input.Network), L(MessageKeys.AdminLists.SocialExists));
        if (custom) CheckCustom(input.Label, input.IconKey);
        if (!ModelState.IsValid) return InvalidRedirect();

        var max = await db.SocialLinks.MaxAsync(s => (int?)s.SortOrder) ?? 0;
        var item = new SocialLink
        {
            Network = network,
            Url = Optional(input.Url),
            Label = custom ? input.Label!.Trim() : null,
            IconKey = custom ? input.IconKey!.Trim() : null,
            SortOrder = max + 1,
            IsActive = true,
        };
        db.SocialLinks.Add(item);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            TempData["Error"] = L(MessageKeys.AdminLists.SocialExists);   // two saves racing for the same network
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Update(SocialLinkUpdateInput input)
    {
        var item = await db.SocialLinks.FindAsync(input.Id);
        if (item == null) return NotFound();
        var custom = item.Network == SocialLink.Custom;
        if (custom) CheckCustom(input.Label, input.IconKey);
        if (!ModelState.IsValid) return InvalidRedirect();

        item.Url = Optional(input.Url);
        if (custom)
        {
            item.Label = input.Label!.Trim();
            item.IconKey = input.IconKey!.Trim();
        }
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.SocialUpdated);
        return RedirectToAction(nameof(Index), null, null, $"row-{item.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.SocialLinks.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        if (await MoveAsync(db.SocialLinks, id, direction) is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    /// <summary>Only a custom network is deleted; a known one is hidden (or its URL emptied) instead.</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.SocialLinks.FindAsync(id);
        if (item == null) return NotFound();
        if (item.Network != SocialLink.Custom) return BadRequest();
        db.SocialLinks.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        return RedirectToAction(nameof(Index));
    }

    private void CheckCustom(string? label, string? iconKey)
    {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(iconKey))
            ModelState.AddModelError(nameof(SocialLinkCreateInput.Label), L(MessageKeys.AdminLists.CustomSocialIncomplete));
        else if (!Icons.IsKnown(iconKey.Trim()))
            ModelState.AddModelError(nameof(SocialLinkCreateInput.IconKey), L(MessageKeys.AdminLists.IconInvalid));
    }
}
