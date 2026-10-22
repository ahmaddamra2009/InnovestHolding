using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

public class EventsController : AdminControllerBase
{
    private const string CreateView = "~/Views/Admin/Events/Create.cshtml";
    private const string EditView = "~/Views/Admin/Events/Edit.cshtml";
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View("~/Views/Admin/Events/Index.cshtml",
            await _db.Events.AsNoTracking().OrderBy(e => e.SortOrder).ToListAsync());

    /// <summary>The site's default UTC offset (content key default_utc_offset) for a new event.</summary>
    private async Task<string> DefaultOffsetAsync()
    {
        var stored = await _db.SiteContents.AsNoTracking().Where(c => c.SectionKey == ContentKeys.DefaultUtcOffset)
            .Select(c => c.ContentAr).FirstOrDefaultAsync();
        return EventDates.TryParseOffset(stored, out _) ? stored! : ContentKeys.DefaultUtcOffsetFallback;
    }

    public async Task<IActionResult> Create() => View(CreateView, new EventInput { UtcOffset = await DefaultOffsetAsync() });

    [HttpPost]
    public async Task<IActionResult> Create(EventInput input)
    {
        if (!ModelState.IsValid)
            return View(CreateView, input);
        var slug = await CheckSlugAsync(_db, SlugTypes.Event, input.Slug, input.TitleEn, null, null,
            s => _db.Events.AnyAsync(e => e.Slug == s));
        if (slug is null) return View(CreateView, input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return View(CreateView, input);

        await using var tx = await _db.Database.BeginTransactionAsync();
        var item = new Event
        {
            SortOrder = (await _db.Events.MaxAsync(e => (int?)e.SortOrder) ?? 0) + 1,
            Slug = slug.Length > 0 ? slug : "tmp-" + Guid.NewGuid().ToString("N"),
        };
        Map(input, item);
        ApplyImage(_db, null, uploaded, input, m => item.Image = m);
        _db.Events.Add(item);
        await SaveWithImageAsync(_db, uploaded);
        if (slug.Length == 0)
        {
            item.Slug = "event-" + item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await _db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        TempData["Success"] = L(MessageKeys.Admin.EventAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Events.AsNoTracking().Include(e => e.Image).FirstOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        var offset = item.StartsAt is { } s ? EventDates.Offset(s.Offset)[3..] : await DefaultOffsetAsync();
        return View(EditView, new EventEditInput
        {
            Id = item.Id,
            Slug = item.Slug,
            TitleAr = item.TitleAr,
            TitleEn = item.TitleEn,
            SummaryAr = item.SummaryAr,
            SummaryEn = item.SummaryEn,
            DescriptionAr = item.DescriptionAr,
            DescriptionEn = item.DescriptionEn,
            StartsAtLocal = item.StartsAt?.DateTime,
            EndsAtLocal = item.EndsAt?.DateTime,
            UtcOffset = offset,
            Status = item.Status,
            AttendanceMode = item.AttendanceMode,
            EventDay = item.StartsAt is null ? item.EventDay : null,
            EventMonthAr = item.StartsAt is null ? item.EventMonthAr : null,
            EventMonthEn = item.StartsAt is null ? item.EventMonthEn : null,
            LocationAr = item.LocationAr,
            LocationEn = item.LocationEn,
            CityAr = item.CityAr,
            CityEn = item.CityEn,
            AddressAr = item.AddressAr,
            AddressEn = item.AddressEn,
            OnlineUrl = item.OnlineUrl,
            TimeAr = item.TimeAr,
            TimeEn = item.TimeEn,
            RegistrationOpen = item.RegistrationOpen,
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
    public async Task<IActionResult> Edit(EventEditInput input)
    {
        var item = await _db.Events.Include(e => e.Image).FirstOrDefaultAsync(e => e.Id == input.Id);
        if (item == null) return NotFound();
        input.Current = MediaRefs.Of(item.Image);
        if (!ModelState.IsValid)
            return View(EditView, input);
        var slug = await CheckSlugAsync(_db, SlugTypes.Event, input.Slug, input.TitleEn, item.Id, item.Slug,
            s => _db.Events.AnyAsync(e => e.Slug == s && e.Id != item.Id));
        if (slug is null) return View(EditView, input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return View(EditView, input);

        await RecordSlugChangeAsync(_db, SlugTypes.Event, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        var leaving = ApplyImage(_db, item.Image, uploaded, input, m => item.Image = m);

        await SaveWithImageAsync(_db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.EventUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Events.Include(e => e.Image).FirstOrDefaultAsync(e => e.Id == id);
        if (item != null)
        {
            var image = item.Image;
            _db.Events.Remove(item);
            if (image is not null) _db.MediaFiles.Remove(image);
            await RemoveSlugRedirectsAsync(_db, SlugTypes.Event, id);
            await SaveWithImageAsync(_db, null, image);
            TempData["Success"] = L(MessageKeys.Admin.EventDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(EventInput input, Event item)
    {
        var starts = input.StartsAt();
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.SummaryAr = input.SummaryAr?.Trim() ?? "";
        item.SummaryEn = Optional(input.SummaryEn);
        item.DescriptionAr = Lf(input.DescriptionAr) ?? "";
        item.DescriptionEn = Lf(input.DescriptionEn);
        item.StartsAt = starts;
        item.EndsAt = input.EndsAt();
        // The free-text day/month stay the list's fallback: with a date they are derived from it.
        if (starts is { } s)
        {
            item.EventDay = s.Day;
            item.EventMonthAr = EventDates.Month(s, false);
            item.EventMonthEn = EventDates.Month(s, true);
        }
        else
        {
            item.EventDay = input.EventDay!.Value;
            item.EventMonthAr = input.EventMonthAr!.Trim();
            item.EventMonthEn = Optional(input.EventMonthEn);
        }
        item.Status = input.Status;
        item.AttendanceMode = input.AttendanceMode;
        item.LocationAr = input.LocationAr?.Trim() ?? "";
        item.LocationEn = Optional(input.LocationEn);
        item.CityAr = input.CityAr?.Trim() ?? "";
        item.CityEn = Optional(input.CityEn);
        item.AddressAr = input.AddressAr?.Trim() ?? "";
        item.AddressEn = Optional(input.AddressEn);
        item.OnlineUrl = Optional(input.OnlineUrl);
        item.TimeAr = input.TimeAr?.Trim() ?? "";
        item.TimeEn = Optional(input.TimeEn);
        item.RegistrationOpen = input.RegistrationOpen;
        item.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        item.MetaTitleEn = Optional(input.MetaTitleEn);
        item.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        item.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
