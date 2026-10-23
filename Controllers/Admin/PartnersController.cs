using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>Partners and sponsors: name, logo (upload), website, group heading, home-strip switch.</summary>
public class PartnersController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public PartnersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData[GroupsKey] = await _db.PartnerGroups.AsNoTracking().OrderBy(g => g.SortOrder).ThenBy(g => g.Id).ToListAsync();
        return View("~/Views/Admin/Partners/Index.cshtml",
            await _db.Partners.AsNoTracking().Include(p => p.Logo).OrderBy(p => p.SortOrder).ToListAsync());
    }

    public const string GroupsKey = "PartnerGroups";

    /// <summary>A group id from the form: an existing group, or none (anything else is refused).</summary>
    private async Task<bool> GroupIsValidAsync(int? id) => id is null || await _db.PartnerGroups.AnyAsync(g => g.Id == id);

    [HttpPost]
    public async Task<IActionResult> Create(PartnerCreateInput input)
    {
        if (!await GroupIsValidAsync(input.PartnerGroupId)) ModelState.AddModelError(nameof(input.PartnerGroupId), L(MessageKeys.InvalidValueNoField));
        if (!ModelState.IsValid) return InvalidRedirect();
        var uploaded = await UploadImageAsync(input, ImageUse.Logo);
        if (!ModelState.IsValid) return InvalidRedirect();

        var maxOrder = await _db.Partners.MaxAsync(p => (int?)p.SortOrder) ?? 0;
        var item = new Partner { SortOrder = maxOrder + 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        Map(input, item);
        ApplyImage(_db, null, uploaded, input, m => item.Logo = m);
        _db.Partners.Add(item);
        await SaveWithImageAsync(_db, uploaded);
        TempData["Success"] = L(MessageKeys.Admin.PartnerAdded);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Edits an existing partner (names, logo, website, group, home strip).</summary>
    [HttpPost]
    public async Task<IActionResult> Update(PartnerUpdateInput input)
    {
        if (!await GroupIsValidAsync(input.PartnerGroupId)) ModelState.AddModelError(nameof(input.PartnerGroupId), L(MessageKeys.InvalidValueNoField));
        if (!ModelState.IsValid) return InvalidRedirect();

        var item = await _db.Partners.Include(p => p.Logo).FirstOrDefaultAsync(p => p.Id == input.Id);
        if (item == null) return NotFound();
        var uploaded = await UploadImageAsync(input, ImageUse.Logo);
        if (!ModelState.IsValid) return InvalidRedirect();

        Map(input, item);
        var leaving = ApplyImage(_db, item.Logo, uploaded, input, m => item.Logo = m);
        await SaveWithImageAsync(_db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.PartnerUpdated);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Shows or hides the row on the public site (a separate action: a form that omits the flag never flips it).</summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await _db.Partners.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Partners.Include(p => p.Logo).FirstOrDefaultAsync(p => p.Id == id);
        if (item != null)
        {
            var logo = item.Logo;
            _db.Partners.Remove(item);
            if (logo is not null) _db.MediaFiles.Remove(logo);
            await SaveWithImageAsync(_db, null, logo);
            TempData["Success"] = L(MessageKeys.Admin.PartnerDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(PartnerCreateInput input, Partner item)
    {
        item.NameAr = input.NameAr!.Trim();
        item.NameEn = Optional(input.NameEn);
        item.Url = Optional(input.Url);
        item.PartnerGroupId = input.PartnerGroupId;
        item.ShowOnHome = input.ShowOnHome;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
