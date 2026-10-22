using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class NewsController : AdminControllerBase
{
    private const string CreateView = "~/Views/Admin/News/Create.cshtml";
    private const string EditView = "~/Views/Admin/News/Edit.cshtml";
    private readonly AppDbContext _db;

    public NewsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/News/Index.cshtml",
            await _db.News.AsNoTracking().OrderBy(n => n.SortOrder).ThenBy(n => n.Id).ToListAsync());

    public IActionResult Create() => View(CreateView, new NewsInput());

    [HttpPost]
    public async Task<IActionResult> Create(NewsInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);
        var slug = await CheckSlugAsync(_db, SlugTypes.News, input.Slug, input.TitleEn, null, null,
            s => _db.News.AnyAsync(n => n.Slug == s));
        if (slug is null) return View(CreateView, input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return View(CreateView, input);

        await using var tx = await _db.Database.BeginTransactionAsync();
        // Newest first: a new item goes before every existing one. No slug from the English title → news-{id} below.
        var item = new News
        {
            SortOrder = (await _db.News.MinAsync(n => (int?)n.SortOrder) ?? 1) - 1,
            Slug = slug.Length > 0 ? slug : "tmp-" + Guid.NewGuid().ToString("N"),
        };
        Map(input, item);
        ApplyImage(_db, null, uploaded, input, m => item.Image = m);
        _db.News.Add(item);
        try
        {
            await SaveWithImageAsync(_db, uploaded);
            if (slug.Length == 0)
            {
                item.Slug = "news-" + item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await SaveWithImageAsync(_db, uploaded); // the row rolls back on failure: its file goes too
            }
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another save took the same slug between the check and this insert (the unique index decided).
            ModelState.AddModelError("Slug", L(MessageKeys.SlugTaken));
            return View(CreateView, input);
        }
        TempData["Success"] = L(MessageKeys.Admin.NewsAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.News.AsNoTracking().Include(n => n.Image).FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();
        return View(EditView, new NewsEditInput
        {
            Id = item.Id,
            Slug = item.Slug,
            TitleAr = item.TitleAr,
            TitleEn = item.TitleEn,
            SummaryAr = item.SummaryAr,
            SummaryEn = item.SummaryEn,
            BodyAr = item.BodyAr,
            BodyEn = item.BodyEn,
            CategoryAr = item.CategoryAr,
            CategoryEn = item.CategoryEn,
            NewsDateAr = item.NewsDateAr,
            NewsDateEn = item.NewsDateEn,
            PublishedOn = item.PublishedOn,
            LinkUrl = item.LinkUrl,
            MetaTitleAr = item.MetaTitleAr,
            MetaTitleEn = item.MetaTitleEn,
            MetaDescriptionAr = item.MetaDescriptionAr,
            MetaDescriptionEn = item.MetaDescriptionEn,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder,
            ImageAltAr = item.Image?.AltAr,
            ImageAltEn = item.Image?.AltEn,
            Current = MediaRefs.Of(item.Image),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(NewsEditInput input)
    {
        var item = await _db.News.Include(n => n.Image).FirstOrDefaultAsync(n => n.Id == input.Id);
        if (item == null) return NotFound();
        input.Current = MediaRefs.Of(item.Image);
        if (!ModelState.IsValid)
            return View(EditView, input);
        var slug = await CheckSlugAsync(_db, SlugTypes.News, input.Slug, input.TitleEn, item.Id, item.Slug,
            s => _db.News.AnyAsync(n => n.Slug == s && n.Id != item.Id));
        if (slug is null) return View(EditView, input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return View(EditView, input);

        await RecordSlugChangeAsync(_db, SlugTypes.News, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        var leaving = ApplyImage(_db, item.Image, uploaded, input, m => item.Image = m);

        try
        {
            await SaveWithImageAsync(_db, uploaded, leaving);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            ModelState.AddModelError("Slug", L(MessageKeys.SlugTaken));
            return View(EditView, input);
        }
        TempData["Success"] = L(MessageKeys.Admin.NewsUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.News.Include(n => n.Image).FirstOrDefaultAsync(n => n.Id == id);
        if (item != null)
        {
            var image = item.Image;
            _db.News.Remove(item);
            if (image is not null) _db.MediaFiles.Remove(image);
            await RemoveSlugRedirectsAsync(_db, SlugTypes.News, id);
            await SaveWithImageAsync(_db, null, image);
            TempData["Success"] = L(MessageKeys.Admin.NewsDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(NewsInput input, News item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.SummaryAr = input.SummaryAr?.Trim() ?? "";
        item.SummaryEn = Optional(input.SummaryEn);
        item.BodyAr = Lf(input.BodyAr) ?? "";
        item.BodyEn = Lf(input.BodyEn);
        item.CategoryAr = input.CategoryAr?.Trim() ?? "";
        item.CategoryEn = Optional(input.CategoryEn);
        item.NewsDateAr = input.NewsDateAr?.Trim() ?? "";
        item.NewsDateEn = Optional(input.NewsDateEn);
        item.PublishedOn = input.PublishedOn;
        item.LinkUrl = Optional(input.LinkUrl);
        item.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        item.MetaTitleEn = Optional(input.MetaTitleEn);
        item.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        item.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
