using System.Globalization;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

// Admin CRUD of the ecosystem entities: Forum speakers, TV guests and episodes, Network opportunities, sponsorship
// packages. Input models only (no entity is bound), slugs and images through AdminControllerBase (the same rules
// as news and events), every save invalidates the site snapshot through the interceptor.

/// <summary>Shared parts of the ecosystem list screens.</summary>
public abstract class EcosystemAdminBase(AppDbContext db) : AdminControllerBase
{
    protected AppDbContext Db => db;

    protected const string ListView = "~/Views/Admin/Ecosystem/Index.cshtml";

    /// <summary>One page (50 rows) of an admin list, in the public order.</summary>
    protected async Task<AdminListVm> ListAsync<TRow>(IQueryable<TRow> ordered, int? page, Func<TRow, AdminRowVm> map, AdminListLabels labels)
    {
        var total = await ordered.CountAsync();
        var n = PagedListViewModel<AdminRowVm>.Clamp(page, total, PageSize);
        var rows = await ordered.Skip((n - 1) * PageSize).Take(PageSize).ToListAsync();
        return new AdminListVm
        {
            Labels = labels,
            Page = new PagedListViewModel<AdminRowVm> { Items = rows.Select(map).ToList(), Page = n, PageSize = PageSize, TotalCount = total, UnreadCount = 0 },
        };
    }

    /// <summary>A slug left empty on create and not derivable from an English title becomes {prefix}-{id} after the insert.</summary>
    protected static string TemporarySlug(string slug) => slug.Length > 0 ? slug : "tmp-" + Guid.NewGuid().ToString("N");

    protected static string IdSlug(string prefix, int id) => prefix + "-" + id.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Ids of linked rows posted by a checkbox list: duplicates dropped, order kept; every id must be in
    /// <paramref name="known"/>, otherwise a ModelState error and null.
    /// </summary>
    protected List<int>? CheckLinks(IEnumerable<int> posted, IReadOnlySet<int> known, string field)
    {
        var ids = posted.Distinct().ToList();
        if (ids.All(known.Contains)) return ids;
        ModelState.AddModelError(field, L(MessageKeys.InvalidValueNoField));
        return null;
    }
}

/// <summary>Speakers and TV guests: the same profile fields, one table each.</summary>
public abstract class PeopleAdminController<T>(AppDbContext db) : EcosystemAdminBase(db) where T : PersonProfile, new()
{
    private const string FormView = "~/Views/Admin/Ecosystem/Person.cshtml";

    protected abstract string SlugType { get; }
    protected abstract AdminListLabels Labels { get; }

    private DbSet<T> Set => Db.Set<T>();

    private IActionResult Form(PersonInput input)
    {
        ViewData["Labels"] = Labels;
        return View(FormView, input);
    }

    public async Task<IActionResult> Index(int? page) =>
        View(ListView, await ListAsync(
            EcosystemQueries.PeopleOrdered(Set.AsNoTracking())
                .Select(p => new { p.Id, p.NameAr, p.NameEn, p.JobTitleAr, p.JobTitleEn, p.OrganizationAr, p.BioAr, p.BioEn, p.IsActive, p.Slug }),
            page,
            p => new AdminRowVm(p.Id, p.NameAr, string.Join(" — ", new[] { p.JobTitleAr, p.OrganizationAr }.Where(s => s.Length > 0)),
                EcosystemQueries.PersonHasEnglish(p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn), p.IsActive, p.Slug),
            Labels));

    public IActionResult Create() => Form(new PersonInput());

    [HttpPost]
    public async Task<IActionResult> Create(PersonInput input)
    {
        if (!ModelState.IsValid) return Form(input);
        var slug = await CheckSlugAsync(Db, SlugType, input.Slug, input.NameEn, null, null, s => Set.AnyAsync(x => x.Slug == s));
        if (slug is null) return Form(input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return Form(input);

        await using var tx = await Db.Database.BeginTransactionAsync();
        var item = new T { SortOrder = (await Set.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1, Slug = TemporarySlug(slug) };
        Map(input, item);
        ApplyImage(Db, null, uploaded, input, m => item.Photo = m);
        Set.Add(item);
        await SaveWithImageAsync(Db, uploaded);
        if (slug.Length == 0)
        {
            item.Slug = IdSlug(SlugType, item.Id);
            await Db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await Set.AsNoTracking().Include(x => x.Photo).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return Form(new PersonInput
        {
            Id = p.Id, Slug = p.Slug, NameAr = p.NameAr, NameEn = p.NameEn, JobTitleAr = p.JobTitleAr, JobTitleEn = p.JobTitleEn,
            OrganizationAr = p.OrganizationAr, OrganizationEn = p.OrganizationEn, BioAr = p.BioAr, BioEn = p.BioEn,
            LinkedInUrl = p.LinkedInUrl, XUrl = p.XUrl, MetaTitleAr = p.MetaTitleAr, MetaTitleEn = p.MetaTitleEn,
            MetaDescriptionAr = p.MetaDescriptionAr, MetaDescriptionEn = p.MetaDescriptionEn, IsActive = p.IsActive, SortOrder = p.SortOrder,
            ImageAltAr = p.Photo?.AltAr, ImageAltEn = p.Photo?.AltEn, Current = MediaRefs.Of(p.Photo),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PersonInput input)
    {
        var item = await Set.Include(x => x.Photo).FirstOrDefaultAsync(x => x.Id == input.Id);
        if (item is null) return NotFound();
        input.Current = MediaRefs.Of(item.Photo);
        if (!ModelState.IsValid) return Form(input);
        var slug = await CheckSlugAsync(Db, SlugType, input.Slug, input.NameEn, item.Id, item.Slug, s => Set.AnyAsync(x => x.Slug == s && x.Id != item.Id));
        if (slug is null) return Form(input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return Form(input);

        await RecordSlugChangeAsync(Db, SlugType, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        var leaving = ApplyImage(Db, item.Photo, uploaded, input, m => item.Photo = m);
        await SaveWithImageAsync(Db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes the profile, its photo and its old-slug redirects; its links to events/episodes go with it.</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Set.Include(x => x.Photo).FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null)
        {
            var photo = item.Photo;
            Set.Remove(item);
            if (photo is not null) Db.MediaFiles.Remove(photo);
            await RemoveSlugRedirectsAsync(Db, SlugType, id);
            await SaveWithImageAsync(Db, null, photo);
            TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Map(PersonInput input, T item)
    {
        item.NameAr = input.NameAr!.Trim();
        item.NameEn = Optional(input.NameEn);
        item.JobTitleAr = input.JobTitleAr?.Trim() ?? "";
        item.JobTitleEn = Optional(input.JobTitleEn);
        item.OrganizationAr = input.OrganizationAr?.Trim() ?? "";
        item.OrganizationEn = Optional(input.OrganizationEn);
        item.BioAr = Lf(input.BioAr) ?? "";
        item.BioEn = Lf(input.BioEn);
        item.LinkedInUrl = Optional(input.LinkedInUrl);
        item.XUrl = Optional(input.XUrl);
        item.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        item.MetaTitleEn = Optional(input.MetaTitleEn);
        item.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        item.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class SpeakersController(AppDbContext db) : PeopleAdminController<Speaker>(db)
{
    protected override string SlugType => SlugTypes.Speaker;

    protected override AdminListLabels Labels { get; } = new("Speakers", "متحدثو المنتدى", "إضافة متحدث", "المنصب — الجهة",
        "/forum/speakers/", "الصورة الشخصية", "حذف هذا المتحدث؟ يُزال من فعالياته أيضاً.");
}

public sealed class TvGuestsController(AppDbContext db) : PeopleAdminController<TvGuest>(db)
{
    protected override string SlugType => SlugTypes.Guest;

    protected override AdminListLabels Labels { get; } = new("TvGuests", "ضيوف البرنامج", "إضافة ضيف", "المنصب — الجهة",
        "/tv/guests/", "الصورة الشخصية", "حذف هذا الضيف؟ يُزال من حلقاته أيضاً.");
}

public sealed class TvEpisodesController(AppDbContext db) : EcosystemAdminBase(db)
{
    private const string FormView = "~/Views/Admin/Ecosystem/Episode.cshtml";

    private static readonly AdminListLabels Labels = new("TvEpisodes", "حلقات البرنامج", "إضافة حلقة", "الموسم / الحلقة — تاريخ العرض",
        "/tv/episodes/", "الصورة المصغّرة (ملصق الفيديو وصورة المشاركة)", "حذف هذه الحلقة؟");

    private async Task<IActionResult> FormAsync(EpisodeInput input)
    {
        input.GuestChoices = await Db.TvGuests.AsNoTracking().OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .Select(g => new PickVm(g.Id, g.NameAr, g.IsActive)).ToListAsync();
        ViewData["Labels"] = Labels;
        return View(FormView, input);
    }

    public async Task<IActionResult> Index(int? page) =>
        View(ListView, await ListAsync(
            EcosystemQueries.EpisodesOrdered(Db.TvEpisodes.AsNoTracking())
                .Select(e => new { e.Id, e.TitleAr, e.TitleEn, e.SummaryEn, e.Season, e.Number, e.AiredOn, e.IsActive, e.Slug }),
            page,
            e => new AdminRowVm(e.Id, e.TitleAr,
                string.Join(" — ", new[]
                {
                    e.Season is null && e.Number is null ? "" : $"{e.Season?.ToString(CultureInfo.InvariantCulture) ?? "-"} / {e.Number?.ToString(CultureInfo.InvariantCulture) ?? "-"}",
                    e.AiredOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                }.Where(s => s.Length > 0)),
                EcosystemQueries.EpisodeHasEnglish(e.TitleEn, e.SummaryEn), e.IsActive, e.Slug),
            Labels));

    public Task<IActionResult> Create() => FormAsync(new EpisodeInput());

    [HttpPost]
    public async Task<IActionResult> Create(EpisodeInput input)
    {
        var guests = await CheckGuestsAsync(input);
        if (!ModelState.IsValid || guests is null) return await FormAsync(input);
        var slug = await CheckSlugAsync(Db, SlugTypes.Episode, input.Slug, input.TitleEn, null, null, s => Db.TvEpisodes.AnyAsync(x => x.Slug == s));
        if (slug is null) return await FormAsync(input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input);

        await using var tx = await Db.Database.BeginTransactionAsync();
        var item = new TvEpisode { SortOrder = (await Db.TvEpisodes.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1, Slug = TemporarySlug(slug) };
        Map(input, item);
        ApplyImage(Db, null, uploaded, input, m => item.Thumbnail = m);
        Db.TvEpisodes.Add(item);
        await SaveWithImageAsync(Db, uploaded);
        if (slug.Length == 0) item.Slug = IdSlug(SlugTypes.Episode, item.Id);
        LinkGuests(item.Id, guests, []);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await Db.TvEpisodes.AsNoTracking().Include(x => x.Thumbnail).FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();
        var guests = await Db.TvEpisodeGuests.AsNoTracking().Where(g => g.TvEpisodeId == id).OrderBy(g => g.SortOrder)
            .Select(g => g.TvGuestId).ToListAsync();
        return await FormAsync(new EpisodeInput
        {
            Id = e.Id, Slug = e.Slug, TitleAr = e.TitleAr, TitleEn = e.TitleEn, SummaryAr = e.SummaryAr, SummaryEn = e.SummaryEn,
            BodyAr = e.BodyAr, BodyEn = e.BodyEn, Season = e.Season, Number = e.Number, AiredOn = e.AiredOn, VideoUrl = e.VideoUrl,
            MetaTitleAr = e.MetaTitleAr, MetaTitleEn = e.MetaTitleEn, MetaDescriptionAr = e.MetaDescriptionAr, MetaDescriptionEn = e.MetaDescriptionEn,
            IsActive = e.IsActive, SortOrder = e.SortOrder, GuestIds = guests,
            ImageAltAr = e.Thumbnail?.AltAr, ImageAltEn = e.Thumbnail?.AltEn, Current = MediaRefs.Of(e.Thumbnail),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EpisodeInput input)
    {
        var item = await Db.TvEpisodes.Include(x => x.Thumbnail).FirstOrDefaultAsync(x => x.Id == input.Id);
        if (item is null) return NotFound();
        input.Current = MediaRefs.Of(item.Thumbnail);
        var guests = await CheckGuestsAsync(input);
        if (!ModelState.IsValid || guests is null) return await FormAsync(input);
        var slug = await CheckSlugAsync(Db, SlugTypes.Episode, input.Slug, input.TitleEn, item.Id, item.Slug,
            s => Db.TvEpisodes.AnyAsync(x => x.Slug == s && x.Id != item.Id));
        if (slug is null) return await FormAsync(input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input);

        await RecordSlugChangeAsync(Db, SlugTypes.Episode, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        var leaving = ApplyImage(Db, item.Thumbnail, uploaded, input, m => item.Thumbnail = m);
        LinkGuests(item.Id, guests, await Db.TvEpisodeGuests.Where(g => g.TvEpisodeId == item.Id).ToListAsync());
        await SaveWithImageAsync(Db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Db.TvEpisodes.Include(x => x.Thumbnail).FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null)
        {
            var image = item.Thumbnail;
            Db.TvEpisodes.Remove(item);
            if (image is not null) Db.MediaFiles.Remove(image);
            await RemoveSlugRedirectsAsync(Db, SlugTypes.Episode, id);
            await SaveWithImageAsync(Db, null, image);
            TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<int>?> CheckGuestsAsync(EpisodeInput input)
    {
        if (input.GuestIds.Count == 0) return [];
        var known = (await Db.TvGuests.AsNoTracking().Where(g => input.GuestIds.Contains(g.Id)).Select(g => g.Id).ToListAsync()).ToHashSet();
        return CheckLinks(input.GuestIds, known, nameof(EpisodeInput.GuestIds));
    }

    /// <summary>Replaces the episode's guests with <paramref name="ids"/>, in that order.</summary>
    private void LinkGuests(int episodeId, IReadOnlyList<int> ids, List<TvEpisodeGuest> current)
    {
        Db.TvEpisodeGuests.RemoveRange(current.Where(c => !ids.Contains(c.TvGuestId)));
        for (var i = 0; i < ids.Count; i++)
        {
            var row = current.FirstOrDefault(c => c.TvGuestId == ids[i]);
            if (row is null) Db.TvEpisodeGuests.Add(new TvEpisodeGuest { TvEpisodeId = episodeId, TvGuestId = ids[i], SortOrder = i });
            else row.SortOrder = i;
        }
    }

    private static void Map(EpisodeInput input, TvEpisode item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.SummaryAr = input.SummaryAr!.Trim();
        item.SummaryEn = Optional(input.SummaryEn);
        item.BodyAr = Lf(input.BodyAr) ?? "";
        item.BodyEn = Lf(input.BodyEn);
        item.Season = input.Season;
        item.Number = input.Number;
        item.AiredOn = input.AiredOn;
        item.VideoUrl = Optional(input.VideoUrl);
        item.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        item.MetaTitleEn = Optional(input.MetaTitleEn);
        item.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        item.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class OpportunitiesController(AppDbContext db) : EcosystemAdminBase(db)
{
    private const string FormView = "~/Views/Admin/Ecosystem/Opportunity.cshtml";

    private static readonly AdminListLabels Labels = new("Opportunities", "الفرص الاستثمارية", "إضافة فرصة", "القطاع — المرحلة",
        "/angel-network/opportunities/", "صورة الفرصة", "حذف هذه الفرصة؟", "منشورة", "مسودة");

    /// <summary>The active sectors, plus the stored one when it has been deactivated since.</summary>
    private async Task<IActionResult> FormAsync(OpportunityInput input, int? storedSector = null)
    {
        input.SectorChoices = await Db.ContentItems.AsNoTracking()
            .Where(i => i.Group == ContentItemGroups.Sectors && (i.IsActive || i.Id == storedSector))
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new PickVm(i.Id, i.TextAr, i.IsActive)).ToListAsync();
        ViewData["Labels"] = Labels;
        return View(FormView, input);
    }

    public async Task<IActionResult> Index(int? page) =>
        View(ListView, await ListAsync(
            EcosystemQueries.OpportunitiesOrdered(Db.InvestmentOpportunities.AsNoTracking())
                .Select(o => new { o.Id, o.TitleAr, o.TitleEn, o.SummaryEn, Sector = o.SectorItem == null ? "" : o.SectorItem.TextAr, o.StageTextAr, o.IsActive, o.Slug }),
            page,
            o => new AdminRowVm(o.Id, o.TitleAr, string.Join(" — ", new[] { o.Sector, o.StageTextAr }.Where(s => s.Length > 0)),
                EcosystemQueries.OpportunityHasEnglish(o.TitleEn, o.SummaryEn), o.IsActive, o.Slug),
            Labels));

    public Task<IActionResult> Create() => FormAsync(new OpportunityInput());

    [HttpPost]
    public async Task<IActionResult> Create(OpportunityInput input)
    {
        await CheckSectorAsync(input, null);
        if (!ModelState.IsValid) return await FormAsync(input);
        var slug = await CheckSlugAsync(Db, SlugTypes.Opportunity, input.Slug, input.TitleEn, null, null,
            s => Db.InvestmentOpportunities.AnyAsync(x => x.Slug == s));
        if (slug is null) return await FormAsync(input);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input);

        await using var tx = await Db.Database.BeginTransactionAsync();
        var item = new InvestmentOpportunity
        {
            SortOrder = (await Db.InvestmentOpportunities.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1,
            Slug = TemporarySlug(slug),
        };
        Map(input, item);
        ApplyImage(Db, null, uploaded, input, m => item.Image = m);
        Db.InvestmentOpportunities.Add(item);
        await SaveWithImageAsync(Db, uploaded);
        if (slug.Length == 0)
        {
            item.Slug = IdSlug(SlugTypes.Opportunity, item.Id);
            await Db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var o = await Db.InvestmentOpportunities.AsNoTracking().Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        return await FormAsync(new OpportunityInput
        {
            Id = o.Id, Slug = o.Slug, TitleAr = o.TitleAr, TitleEn = o.TitleEn, SummaryAr = o.SummaryAr, SummaryEn = o.SummaryEn,
            BodyAr = o.BodyAr, BodyEn = o.BodyEn, SectorItemId = o.SectorItemId, StageTextAr = o.StageTextAr, StageTextEn = o.StageTextEn,
            AskTextAr = o.AskTextAr, AskTextEn = o.AskTextEn, IsOpen = o.IsOpen,
            MetaTitleAr = o.MetaTitleAr, MetaTitleEn = o.MetaTitleEn, MetaDescriptionAr = o.MetaDescriptionAr, MetaDescriptionEn = o.MetaDescriptionEn,
            IsActive = o.IsActive, SortOrder = o.SortOrder,
            ImageAltAr = o.Image?.AltAr, ImageAltEn = o.Image?.AltEn, Current = MediaRefs.Of(o.Image),
        }, o.SectorItemId);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(OpportunityInput input)
    {
        var item = await Db.InvestmentOpportunities.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == input.Id);
        if (item is null) return NotFound();
        input.Current = MediaRefs.Of(item.Image);
        await CheckSectorAsync(input, item.SectorItemId);
        if (!ModelState.IsValid) return await FormAsync(input, item.SectorItemId);
        var slug = await CheckSlugAsync(Db, SlugTypes.Opportunity, input.Slug, input.TitleEn, item.Id, item.Slug,
            s => Db.InvestmentOpportunities.AnyAsync(x => x.Slug == s && x.Id != item.Id));
        if (slug is null) return await FormAsync(input, item.SectorItemId);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input, item.SectorItemId);

        await RecordSlugChangeAsync(Db, SlugTypes.Opportunity, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        var leaving = ApplyImage(Db, item.Image, uploaded, input, m => item.Image = m);
        await SaveWithImageAsync(Db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Db.InvestmentOpportunities.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null)
        {
            var image = item.Image;
            Db.InvestmentOpportunities.Remove(item);
            if (image is not null) Db.MediaFiles.Remove(image);
            await RemoveSlugRedirectsAsync(Db, SlugTypes.Opportunity, id);
            await SaveWithImageAsync(Db, null, image);
            TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>The sector must be an active row of the "sectors" list (or the one already stored).</summary>
    private async Task CheckSectorAsync(OpportunityInput input, int? stored)
    {
        if (input.SectorItemId is not { } id) return;
        var ok = await Db.ContentItems.AnyAsync(i => i.Id == id && i.Group == ContentItemGroups.Sectors && (i.IsActive || i.Id == stored));
        if (!ok) ModelState.AddModelError(nameof(input.SectorItemId), L(MessageKeys.InvalidValueNoField));
    }

    private static void Map(OpportunityInput input, InvestmentOpportunity item)
    {
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.SummaryAr = input.SummaryAr!.Trim();
        item.SummaryEn = Optional(input.SummaryEn);
        item.BodyAr = Lf(input.BodyAr) ?? "";
        item.BodyEn = Lf(input.BodyEn);
        item.SectorItemId = input.SectorItemId;
        item.StageTextAr = input.StageTextAr?.Trim() ?? "";
        item.StageTextEn = Optional(input.StageTextEn);
        item.AskTextAr = input.AskTextAr?.Trim() ?? "";
        item.AskTextEn = Optional(input.AskTextEn);
        item.IsOpen = input.IsOpen;
        item.MetaTitleAr = input.MetaTitleAr?.Trim() ?? "";
        item.MetaTitleEn = Optional(input.MetaTitleEn);
        item.MetaDescriptionAr = input.MetaDescriptionAr?.Trim() ?? "";
        item.MetaDescriptionEn = Optional(input.MetaDescriptionEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class SponsorshipPackagesController(AppDbContext db) : EcosystemAdminBase(db)
{
    private const string IndexView = "~/Views/Admin/Ecosystem/Packages.cshtml";
    private const string FormView = "~/Views/Admin/Ecosystem/Package.cshtml";

    public async Task<IActionResult> Index() =>
        View(IndexView, await Db.SponsorshipPackages.AsNoTracking().OrderBy(p => p.Unit).ThenBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    public IActionResult Create() => View(FormView, new PackageInput());

    [HttpPost]
    public async Task<IActionResult> Create(PackageInput input)
    {
        CheckUnit(input);
        if (!ModelState.IsValid) return View(FormView, input);
        var item = new SponsorshipPackage { SortOrder = (await Db.SponsorshipPackages.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1 };
        Map(input, item);
        Db.SponsorshipPackages.Add(item);
        await Db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await Db.SponsorshipPackages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View(FormView, new PackageInput
        {
            Id = p.Id, Unit = p.Unit, TitleAr = p.TitleAr, TitleEn = p.TitleEn, PriceTextAr = p.PriceTextAr, PriceTextEn = p.PriceTextEn,
            BenefitsAr = p.BenefitsAr, BenefitsEn = p.BenefitsEn, SortOrder = p.SortOrder, IsActive = p.IsActive,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PackageInput input)
    {
        var item = await Db.SponsorshipPackages.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (item is null) return NotFound();
        CheckUnit(input);
        if (!ModelState.IsValid) return View(FormView, input);
        Map(input, item);
        item.SortOrder = input.SortOrder;
        await Db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Db.SponsorshipPackages.FindAsync(id);
        if (item is not null)
        {
            Db.SponsorshipPackages.Remove(item);
            await Db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        }
        return RedirectToAction(nameof(Index));
    }

    private void CheckUnit(PackageInput input)
    {
        if (!SponsorshipUnits.IsKnown(input.Unit)) ModelState.AddModelError(nameof(input.Unit), L(MessageKeys.InvalidValueNoField));
    }

    private static void Map(PackageInput input, SponsorshipPackage item)
    {
        item.Unit = input.Unit!;
        item.TitleAr = input.TitleAr!.Trim();
        item.TitleEn = Optional(input.TitleEn);
        item.PriceTextAr = input.PriceTextAr?.Trim() ?? "";
        item.PriceTextEn = Optional(input.PriceTextEn);
        item.BenefitsAr = Lf(input.BenefitsAr) ?? "";
        item.BenefitsEn = Lf(input.BenefitsEn);
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
