using System.Globalization;
using System.Linq.Expressions;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
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
}

/// <summary>
/// The create / edit / delete flow of an item with a detail page (speaker, guest, episode, opportunity): slug rules and
/// redirects, the image (upload, replace, remove, alt texts), the {type}-{id} address when there is no English title,
/// and optional links to other rows. A subclass supplies the mapping, the image navigation and its texts.
/// </summary>
public abstract class DetailAdminController<TEntity, TInput>(AppDbContext db) : EcosystemAdminBase(db)
    where TEntity : class, IDetailEntity, new()
    where TInput : DetailMetaInput, new()
{
    protected abstract string SlugType { get; }
    protected abstract AdminListLabels Labels { get; }
    protected abstract string FormView { get; }

    /// <summary>The image navigation (photo, thumbnail, image).</summary>
    protected abstract Expression<Func<TEntity, MediaFile?>> ImageNav { get; }
    protected abstract void SetImage(TEntity item, MediaFile? image);

    /// <summary>The English title a new slug is made from.</summary>
    protected abstract string? TitleEn(TInput input);
    protected abstract void Map(TInput input, TEntity item);
    protected abstract TInput ToInput(TEntity item);
    protected abstract Task<AdminListVm> ListPageAsync(int? page);

    /// <summary>Fills the form's display-only lists (pickers).</summary>
    protected virtual Task PrepareFormAsync(TInput input, TEntity? stored) => Task.CompletedTask;

    /// <summary>Checks that need the database (linked ids…); adds ModelState errors.</summary>
    protected virtual Task ValidateAsync(TInput input, TEntity? stored) => Task.CompletedTask;

    /// <summary>Writes the item's links (the item has its id); the caller saves.</summary>
    protected virtual Task SaveLinksAsync(TInput input, TEntity item) => Task.CompletedTask;

    /// <summary>Loads the item's links into the edit form.</summary>
    protected virtual Task LoadLinksAsync(TInput input, TEntity item) => Task.CompletedTask;

    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    private Func<TEntity, MediaFile?>? _imageOf;
    private MediaFile? ImageOf(TEntity item) => (_imageOf ??= ImageNav.Compile())(item);

    private async Task<IActionResult> FormAsync(TInput input, TEntity? stored)
    {
        await PrepareFormAsync(input, stored);
        ViewData["Labels"] = Labels;
        return View(FormView, input);
    }

    public async Task<IActionResult> Index(int? page) => View(ListView, await ListPageAsync(page));

    public Task<IActionResult> Create() => FormAsync(new TInput(), null);

    [HttpPost]
    public async Task<IActionResult> Create(TInput input)
    {
        input.Id = 0;
        await ValidateAsync(input, null);
        if (!ModelState.IsValid) return await FormAsync(input, null);
        var slug = await CheckSlugAsync(Db, SlugType, input.Slug, TitleEn(input), null, null, s => Set.AnyAsync(x => x.Slug == s));
        if (slug is null) return await FormAsync(input, null);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input, null);

        await using var tx = await Db.Database.BeginTransactionAsync();
        var item = new TEntity
        {
            SortOrder = (await Set.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1,
            Slug = slug.Length > 0 ? slug : "tmp-" + Guid.NewGuid().ToString("N"),
        };
        Map(input, item);
        item.UpdatedAt = DateTime.UtcNow;
        ApplyImage(Db, null, uploaded, input, m => SetImage(item, m));
        Set.Add(item);
        await SaveWithImageAsync(Db, uploaded);
        // No English title: {type}-{id}, an address CheckSlugAsync keeps for this use (typed ones are refused).
        if (slug.Length == 0) item.Slug = SlugType + "-" + item.Id.ToString(CultureInfo.InvariantCulture);
        await SaveLinksAsync(input, item);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = L(MessageKeys.Admin.ItemAdded);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await Set.AsNoTracking().Include(ImageNav).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var input = ToInput(item);
        var image = ImageOf(item);
        input.Id = item.Id;
        input.Slug = item.Slug;
        input.SortOrder = item.SortOrder;
        input.ImageAltAr = image?.AltAr;
        input.ImageAltEn = image?.AltEn;
        input.Current = MediaRefs.Of(image);
        await LoadLinksAsync(input, item);
        return await FormAsync(input, item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TInput input)
    {
        var item = await Set.Include(ImageNav).FirstOrDefaultAsync(x => x.Id == input.Id);
        if (item is null) return NotFound();
        input.Current = MediaRefs.Of(ImageOf(item));
        await ValidateAsync(input, item);
        if (!ModelState.IsValid) return await FormAsync(input, item);
        var slug = await CheckSlugAsync(Db, SlugType, input.Slug, TitleEn(input), item.Id, item.Slug,
            s => Set.AnyAsync(x => x.Slug == s && x.Id != item.Id));
        if (slug is null) return await FormAsync(input, item);
        var uploaded = await UploadImageAsync(input);
        if (!ModelState.IsValid) return await FormAsync(input, item);

        await RecordSlugChangeAsync(Db, SlugType, item.Id, item.Slug, slug);
        item.Slug = slug;
        Map(input, item);
        item.SortOrder = input.SortOrder;
        item.UpdatedAt = DateTime.UtcNow;
        var leaving = ApplyImage(Db, ImageOf(item), uploaded, input, m => SetImage(item, m));
        await SaveLinksAsync(input, item);
        await SaveWithImageAsync(Db, uploaded, leaving);
        TempData["Success"] = L(MessageKeys.Admin.ItemUpdated);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes the item, its image and its old-slug redirects; its link rows go with it (cascade).</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Set.Include(ImageNav).FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null)
        {
            var image = ImageOf(item);
            Set.Remove(item);
            if (image is not null) Db.MediaFiles.Remove(image);
            await RemoveSlugRedirectsAsync(Db, SlugType, id);
            await SaveWithImageAsync(Db, null, image);
            TempData["Success"] = L(MessageKeys.Admin.ItemDeleted);
        }
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Speakers and TV guests: the same profile fields, one table each.</summary>
public abstract class PeopleAdminController<T>(AppDbContext db) : DetailAdminController<T, PersonInput>(db) where T : PersonProfile, new()
{
    protected override string FormView => "~/Views/Admin/Ecosystem/Person.cshtml";
    protected override Expression<Func<T, MediaFile?>> ImageNav => x => x.Photo;
    protected override void SetImage(T item, MediaFile? image) => item.Photo = image;
    protected override string? TitleEn(PersonInput input) => input.NameEn;

    protected override async Task<AdminListVm> ListPageAsync(int? page) => await ListAsync(
        EcosystemQueries.PeopleOrdered(Set.AsNoTracking())
            .Select(p => new { p.Id, p.NameAr, p.NameEn, p.JobTitleAr, p.JobTitleEn, p.OrganizationAr, p.BioAr, p.BioEn, p.IsActive, p.Slug }),
        page,
        p => new AdminRowVm(p.Id, p.NameAr, string.Join(" — ", new[] { p.JobTitleAr, p.OrganizationAr }.Where(s => s.Length > 0)),
            EcosystemQueries.PersonHasEnglish(p.NameEn, p.JobTitleAr, p.JobTitleEn, p.BioAr, p.BioEn), p.IsActive, p.Slug),
        Labels);

    protected override PersonInput ToInput(T p) => new()
    {
        NameAr = p.NameAr, NameEn = p.NameEn, JobTitleAr = p.JobTitleAr, JobTitleEn = p.JobTitleEn,
        OrganizationAr = p.OrganizationAr, OrganizationEn = p.OrganizationEn, BioAr = p.BioAr, BioEn = p.BioEn,
        LinkedInUrl = p.LinkedInUrl, XUrl = p.XUrl, MetaTitleAr = p.MetaTitleAr, MetaTitleEn = p.MetaTitleEn,
        MetaDescriptionAr = p.MetaDescriptionAr, MetaDescriptionEn = p.MetaDescriptionEn, IsActive = p.IsActive,
    };

    protected override void Map(PersonInput input, T item)
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

public sealed class TvEpisodesController(AppDbContext db) : DetailAdminController<TvEpisode, EpisodeInput>(db)
{
    protected override string SlugType => SlugTypes.Episode;
    protected override string FormView => "~/Views/Admin/Ecosystem/Episode.cshtml";
    protected override Expression<Func<TvEpisode, MediaFile?>> ImageNav => x => x.Thumbnail;
    protected override void SetImage(TvEpisode item, MediaFile? image) => item.Thumbnail = image;
    protected override string? TitleEn(EpisodeInput input) => input.TitleEn;

    protected override AdminListLabels Labels { get; } = new("TvEpisodes", "حلقات البرنامج", "إضافة حلقة", "الموسم / الحلقة — تاريخ العرض",
        "/tv/episodes/", "الصورة المصغّرة (ملصق الفيديو وصورة المشاركة)", "حذف هذه الحلقة؟");

    protected override async Task PrepareFormAsync(EpisodeInput input, TvEpisode? stored) =>
        input.GuestChoices = await Db.TvGuests.AsNoTracking().OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .Select(g => new PickVm(g.Id, g.NameAr, g.IsActive)).ToListAsync();

    protected override async Task<AdminListVm> ListPageAsync(int? page) => await ListAsync(
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
        Labels);

    private List<int> _guests = [];

    /// <summary>The posted guests must exist (checked ids, in the posted order).</summary>
    protected override async Task ValidateAsync(EpisodeInput input, TvEpisode? stored)
    {
        var known = (await Db.TvGuests.AsNoTracking().Where(g => input.GuestIds.Contains(g.Id)).Select(g => g.Id).ToListAsync()).ToHashSet();
        _guests = CheckLinks(input.GuestIds, known, nameof(EpisodeInput.GuestIds)) ?? [];
    }

    protected override async Task SaveLinksAsync(EpisodeInput input, TvEpisode item) =>
        ReplaceLinks(Db.TvEpisodeGuests, await Db.TvEpisodeGuests.Where(g => g.TvEpisodeId == item.Id).ToListAsync(), _guests,
            g => g.TvGuestId, (id, i) => new TvEpisodeGuest { TvEpisodeId = item.Id, TvGuestId = id, SortOrder = i }, (g, i) => g.SortOrder = i);

    protected override async Task LoadLinksAsync(EpisodeInput input, TvEpisode item) =>
        input.GuestIds = await Db.TvEpisodeGuests.AsNoTracking().Where(g => g.TvEpisodeId == item.Id).OrderBy(g => g.SortOrder)
            .Select(g => g.TvGuestId).ToListAsync();

    protected override EpisodeInput ToInput(TvEpisode e) => new()
    {
        TitleAr = e.TitleAr, TitleEn = e.TitleEn, SummaryAr = e.SummaryAr, SummaryEn = e.SummaryEn,
        BodyAr = e.BodyAr, BodyEn = e.BodyEn, Season = e.Season, Number = e.Number, AiredOn = e.AiredOn, VideoUrl = e.VideoUrl,
        MetaTitleAr = e.MetaTitleAr, MetaTitleEn = e.MetaTitleEn, MetaDescriptionAr = e.MetaDescriptionAr, MetaDescriptionEn = e.MetaDescriptionEn,
        IsActive = e.IsActive,
    };

    protected override void Map(EpisodeInput input, TvEpisode item)
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
    }
}

public sealed class OpportunitiesController(AppDbContext db) : DetailAdminController<InvestmentOpportunity, OpportunityInput>(db)
{
    protected override string SlugType => SlugTypes.Opportunity;
    protected override string FormView => "~/Views/Admin/Ecosystem/Opportunity.cshtml";
    protected override Expression<Func<InvestmentOpportunity, MediaFile?>> ImageNav => x => x.Image;
    protected override void SetImage(InvestmentOpportunity item, MediaFile? image) => item.Image = image;
    protected override string? TitleEn(OpportunityInput input) => input.TitleEn;

    protected override AdminListLabels Labels { get; } = new("Opportunities", "الفرص الاستثمارية", "إضافة فرصة", "القطاع — المرحلة",
        "/angel-network/opportunities/", "صورة الفرصة", "حذف هذه الفرصة؟", "منشورة", "مسودة");

    /// <summary>The active sectors, plus the stored one when it has been deactivated since.</summary>
    protected override async Task PrepareFormAsync(OpportunityInput input, InvestmentOpportunity? stored)
    {
        var storedSector = stored?.SectorItemId;
        input.SectorChoices = await Db.ContentItems.AsNoTracking()
            .Where(i => i.Group == ContentItemGroups.Sectors && (i.IsActive || i.Id == storedSector))
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new PickVm(i.Id, i.TextAr, i.IsActive)).ToListAsync();
    }

    protected override async Task<AdminListVm> ListPageAsync(int? page) => await ListAsync(
        EcosystemQueries.OpportunitiesOrdered(Db.InvestmentOpportunities.AsNoTracking())
            .Select(o => new { o.Id, o.TitleAr, o.TitleEn, o.SummaryEn, Sector = o.SectorItem == null ? "" : o.SectorItem.TextAr, o.StageTextAr, o.IsActive, o.Slug }),
        page,
        o => new AdminRowVm(o.Id, o.TitleAr, string.Join(" — ", new[] { o.Sector, o.StageTextAr }.Where(s => s.Length > 0)),
            EcosystemQueries.OpportunityHasEnglish(o.TitleEn, o.SummaryEn), o.IsActive, o.Slug),
        Labels);

    /// <summary>The sector must be an active row of the "sectors" list (or the one already stored).</summary>
    protected override async Task ValidateAsync(OpportunityInput input, InvestmentOpportunity? stored)
    {
        if (input.SectorItemId is not { } id) return;
        var storedSector = stored?.SectorItemId;
        var ok = await Db.ContentItems.AnyAsync(i => i.Id == id && i.Group == ContentItemGroups.Sectors && (i.IsActive || i.Id == storedSector));
        if (!ok) ModelState.AddModelError(nameof(input.SectorItemId), L(MessageKeys.InvalidValueNoField));
    }

    protected override OpportunityInput ToInput(InvestmentOpportunity o) => new()
    {
        TitleAr = o.TitleAr, TitleEn = o.TitleEn, SummaryAr = o.SummaryAr, SummaryEn = o.SummaryEn,
        BodyAr = o.BodyAr, BodyEn = o.BodyEn, SectorItemId = o.SectorItemId, StageTextAr = o.StageTextAr, StageTextEn = o.StageTextEn,
        AskTextAr = o.AskTextAr, AskTextEn = o.AskTextEn, IsOpen = o.IsOpen,
        MetaTitleAr = o.MetaTitleAr, MetaTitleEn = o.MetaTitleEn, MetaDescriptionAr = o.MetaDescriptionAr, MetaDescriptionEn = o.MetaDescriptionEn,
        IsActive = o.IsActive,
    };

    protected override void Map(OpportunityInput input, InvestmentOpportunity item)
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
