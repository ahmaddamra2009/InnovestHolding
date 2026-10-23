using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «النسخ السابقة» (F10): the past editions of the forum shown on /forum/editions, each with up to
/// <see cref="ForumEdition.MaxPhotos"/> photos. A photo is an ordinary MediaFile with one owner (its
/// ForumEditionPhotos row): uploaded one per POST through the shared pipeline (MediaStore, ImageUse.General), its alt
/// text in MediaFile.AltAr/AltEn. Every photo action names its edition and the photo must belong to it (404 otherwise).
/// </summary>
public class ForumEditionsController(AppDbContext db) : AdminControllerBase
{
    private const string IndexView = "~/Views/Admin/ForumEditions/Index.cshtml";
    private const string EditView = "~/Views/Admin/ForumEditions/Edit.cshtml";

    public async Task<IActionResult> Index()
    {
        var rows = await db.ForumEditions.AsNoTracking()
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Select(e => new
            {
                e.Id, e.Year, e.EditionLabelAr, e.TitleAr, e.TitleEn, e.TextAr, e.TextEn, e.IsActive,
                Photos = e.Photos.Count,
                Cover = e.Photos.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Select(p => p.Image).FirstOrDefault(),
            })
            .ToListAsync();
        return View(IndexView, rows.Select(r => new ForumEditionRowVm(r.Id, r.Year, r.EditionLabelAr, r.TitleAr,
            // The EN chip = the public pages' rule (one definition: ForumEditionQueries.IsEnglishComplete).
            ForumEditionQueries.IsEnglishComplete(r.TitleEn, r.TextAr, r.TextEn), r.IsActive, r.Photos, MediaRefs.Of(r.Cover))).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ForumEditionInput input)
    {
        if (!ModelState.IsValid) return InvalidRedirect();
        var max = await db.ForumEditions.MaxAsync(e => (int?)e.SortOrder) ?? 0;
        var item = new ForumEdition { SortOrder = max + 1, CreatedAt = DateTime.UtcNow };
        Map(input, item);
        db.ForumEditions.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.AdminForumEditions.EditionAdded);
        return RedirectToAction(nameof(Edit), new { id = item.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ForumEditions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (item is null) return NotFound();
        return View(EditView, await WithPhotosAsync(new ForumEditionEditInput
        {
            Id = item.Id,
            Year = item.Year,
            EditionLabelAr = item.EditionLabelAr,
            EditionLabelEn = item.EditionLabelEn,
            TitleAr = item.TitleAr,
            TitleEn = item.TitleEn,
            PlaceAr = item.PlaceAr,
            PlaceEn = item.PlaceEn,
            TextAr = item.TextAr,
            TextEn = item.TextEn,
            IsActive = item.IsActive,
        }));
    }

    /// <summary>The edit form's photos (display only), in their order.</summary>
    private async Task<ForumEditionEditInput> WithPhotosAsync(ForumEditionEditInput input)
    {
        input.Photos = (await db.ForumEditionPhotos.AsNoTracking().Where(p => p.ForumEditionId == input.Id)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Include(p => p.Image).ToListAsync())
            .Select(p => new ForumEditionPhotoVm(p.Id, MediaRefs.Of(p.Image), p.Image?.AltAr ?? "", p.Image?.AltEn))
            .ToList();
        return input;
    }

    [HttpPost]
    public async Task<IActionResult> Update(ForumEditionEditInput input)
    {
        var item = await db.ForumEditions.FirstOrDefaultAsync(e => e.Id == input.Id);
        if (item is null) return NotFound();
        if (!ModelState.IsValid) return View(EditView, await WithPhotosAsync(input));
        Map(input, item);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.AdminForumEditions.EditionUpdated);
        return RedirectToAction(nameof(Edit), new { id = item.Id });
    }

    /// <summary>Shows or hides the edition on the public site (a separate action: a form that omits the flag never flips it).</summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.ForumEditions.FindAsync(id);
        if (item is null) return NotFound();
        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.StatusToggled);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    /// <summary>▲▼ in the editions list (the public order).</summary>
    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        var moved = await MoveAsync(db.ForumEditions, id, direction);
        if (moved is null) return NotFound();
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return RedirectToAction(nameof(Index), null, null, $"row-{id}");
    }

    /// <summary>Deletes the edition, its photo rows and their images (rows in one SaveChanges, files after the commit).</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ForumEditions.Include(e => e.Photos).ThenInclude(p => p.Image).FirstOrDefaultAsync(e => e.Id == id);
        if (item is not null)
        {
            var images = item.Photos.Select(p => p.Image).Where(m => m is not null).ToArray();
            db.ForumEditionPhotos.RemoveRange(item.Photos);
            db.MediaFiles.RemoveRange(images!);
            db.ForumEditions.Remove(item);
            await SaveWithImageAsync(db, null, images);
            TempData["Success"] = L(MessageKeys.AdminForumEditions.EditionDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    // ── Photos ──

    /// <summary>
    /// Adds ONE photo at the end of the edition. Refused (nothing written) when no file is chosen, when the edition already
    /// holds <see cref="ForumEdition.MaxPhotos"/>, or by the image pipeline. The cap is checked again under a lock on the
    /// edition row, so two uploads at once cannot pass it.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddPhoto(ForumEditionPhotoInput input)
    {
        if (!await db.ForumEditions.AnyAsync(e => e.Id == input.EditionId)) return NotFound();
        if (!input.HasUpload) ModelState.AddModelError(nameof(input.Image), L(MessageKeys.AdminForumEditions.PhotoRequired));
        else if (await db.ForumEditionPhotos.CountAsync(p => p.ForumEditionId == input.EditionId) >= ForumEdition.MaxPhotos)
            ModelState.AddModelError(nameof(input.Image), L(MessageKeys.AdminForumEditions.PhotoLimit, ForumEdition.MaxPhotos));
        if (!ModelState.IsValid) return BackToEdit(input.EditionId);

        var uploaded = await UploadImageAsync(input, ImageUse.General);
        if (!ModelState.IsValid || uploaded is null) return BackToEdit(input.EditionId);

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            // Locks the edition row until the commit: a concurrent upload to the same edition waits here.
            var now = DateTime.UtcNow;
            await db.ForumEditions.Where(e => e.Id == input.EditionId).ExecuteUpdateAsync(s => s.SetProperty(e => e.UpdatedAt, now));
            var count = await db.ForumEditionPhotos.CountAsync(p => p.ForumEditionId == input.EditionId);
            if (count >= ForumEdition.MaxPhotos)
            {
                Media.Discard(uploaded);
                ModelState.AddModelError(nameof(input.Image), L(MessageKeys.AdminForumEditions.PhotoLimit, ForumEdition.MaxPhotos));
                return BackToEdit(input.EditionId);
            }
            var max = await db.ForumEditionPhotos.Where(p => p.ForumEditionId == input.EditionId).MaxAsync(p => (int?)p.SortOrder) ?? 0;
            db.MediaFiles.Add(uploaded);
            db.ForumEditionPhotos.Add(new ForumEditionPhoto { ForumEditionId = input.EditionId, Image = uploaded, SortOrder = max + 1, CreatedAt = now });
            await SaveWithImageAsync(db, uploaded);
            await tx.CommitAsync();
        }
        catch
        {
            Media.Discard(uploaded);
            throw;
        }
        TempData["Success"] = L(MessageKeys.AdminForumEditions.PhotoAdded);
        return BackToEdit(input.EditionId);
    }

    /// <summary>The alt texts of one photo of the edition.</summary>
    [HttpPost]
    public async Task<IActionResult> UpdatePhoto(ForumEditionPhotoAltInput input)
    {
        var photo = await db.ForumEditionPhotos.Include(p => p.Image)
            .FirstOrDefaultAsync(p => p.Id == input.PhotoId && p.ForumEditionId == input.EditionId);
        if (photo?.Image is null) return NotFound();
        if (!ModelState.IsValid) return BackToEdit(input.EditionId);
        photo.Image.AltAr = input.ImageAltAr?.Trim() ?? "";
        photo.Image.AltEn = Optional(input.ImageAltEn);
        await TouchAsync(input.EditionId);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.AdminForumEditions.PhotoUpdated);
        return BackToEdit(input.EditionId, $"photo-{photo.Id}");
    }

    /// <summary>▲▼ among the edition's photos (the first is the cover).</summary>
    [HttpPost]
    public async Task<IActionResult> MovePhoto(int id, int direction, int editionId)
    {
        var moved = await MoveAsync(db.ForumEditionPhotos.Where(p => p.ForumEditionId == editionId), id, direction);
        if (moved is null) return NotFound();
        if (moved.Moved)
        {
            await TouchAsync(editionId);
            await db.SaveChangesAsync();
        }
        TempData["Success"] = L(MessageKeys.Admin.OrderChanged);
        return BackToEdit(editionId, $"photo-{id}");
    }

    /// <summary>Deletes one photo of the edition: its row and image (row and MediaFile together, the file after the commit).</summary>
    [HttpPost]
    public async Task<IActionResult> DeletePhoto(int id, int editionId)
    {
        var photo = await db.ForumEditionPhotos.Include(p => p.Image).FirstOrDefaultAsync(p => p.Id == id && p.ForumEditionId == editionId);
        if (photo is null) return NotFound();
        var image = photo.Image;
        db.ForumEditionPhotos.Remove(photo);
        if (image is not null) db.MediaFiles.Remove(image);
        await TouchAsync(editionId);
        await SaveWithImageAsync(db, null, image);
        TempData["Success"] = L(MessageKeys.AdminForumEditions.PhotoDeleted);
        return BackToEdit(editionId);
    }

    /// <summary>The edition's UpdatedAt (the editions page's lastmod) follows its photos.</summary>
    private async Task TouchAsync(int editionId)
    {
        var edition = await db.ForumEditions.FindAsync(editionId);
        if (edition is not null) edition.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>PRG back to the edit page (errors, if any, go to TempData["Error"]).</summary>
    private IActionResult BackToEdit(int editionId, string? fragment = "photos")
    {
        if (!ModelState.IsValid) TempData["Error"] = JoinErrors();
        return RedirectToAction(nameof(Edit), null, new { id = editionId }, fragment);
    }

    private static void Map(ForumEditionInput input, ForumEdition item)
    {
        item.Year = (short)input.Year!.Value;
        item.EditionLabelAr = input.EditionLabelAr?.Trim() ?? "";
        item.EditionLabelEn = Optional(input.EditionLabelEn);
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.PlaceAr = input.PlaceAr?.Trim() ?? "";
        item.PlaceEn = Optional(input.PlaceEn);
        item.TextAr = Lf(input.TextAr) ?? "";
        item.TextEn = Lf(input.TextEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
