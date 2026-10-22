using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Leads;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «الطلبات» — the applications inbox: every lead of the public forms in one list (filter by type, read state and
/// workflow status; newest first, paged), a detail screen per lead with its status and an internal note, mark
/// read/unread, delete, and a CSV export of the current filter. Leads are personal data: admin-only, never logged.
/// The quick registrations stay listed in «التسجيلات» and contact messages in «رسائل التواصل» as before.
/// </summary>
public class LeadsController(AppDbContext db, ISiteContentCache cache) : AdminControllerBase
{
    public const string Unread = "unread";
    public const string Read = "read";

    /// <summary>The list filter. Values are re-validated on every request (<see cref="Clean"/>); never a URL from the request.</summary>
    public sealed record Filter(string? Type, string? Read, LeadStatus? Status)
    {
        public object Route(int? page = null) => new
        {
            type = Type,
            read = Read,
            status = Status is { } s ? LeadStatuses.Key(s) : null,
            page = page > 1 ? page : null,
        };
    }

    public static Filter Clean(string? type, string? read, string? status) => new(
        LeadInbox.IsType(type) ? type : null,
        read is Unread or Read ? read : null,
        LeadStatuses.Parse(status));

    private static IQueryable<LeadRow> Apply(IQueryable<LeadRow> query, Filter f)
    {
        if (f.Read == Unread) query = query.Where(r => !r.IsRead);
        else if (f.Read == Read) query = query.Where(r => r.IsRead);
        if (f.Status is { } s) query = query.Where(r => r.Status == s);
        return query;
    }

    public async Task<IActionResult> Index(string? type, string? read, string? status, int? page)
    {
        var filter = Clean(type, read, status);
        var query = Apply(LeadInbox.Query(db, filter.Type), filter);

        var total = await query.CountAsync();
        var current = PagedListViewModel<LeadRow>.Clamp(page, total, PageSize);
        var rows = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((current - 1) * PageSize).Take(PageSize).ToListAsync();
        var unread = await LeadInbox.UnreadAsync(db);

        return View("~/Views/Admin/Leads/Index.cshtml", new LeadsIndexVm(
            new PagedListViewModel<LeadRow> { Items = rows, Page = current, PageSize = PageSize, TotalCount = total, UnreadCount = unread.Leads },
            filter, unread));
    }

    /// <param name="type">The lead's own type (which table).</param>
    /// <param name="listType">The list filter to return to (another value than <paramref name="type"/> when the list shows all types).</param>
    public async Task<IActionResult> Details(string? type, int id, string? listType, string? read, string? status, int? page)
    {
        var detail = LeadInbox.IsType(type) ? await LoadAsync(type!, id) : null;
        if (detail is null) return NotFound();
        return View("~/Views/Admin/Leads/Details.cshtml", detail with { Back = Clean(listType, read, status), Page = page > 1 ? page : null });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleRead(string? type, int id, string? listType, string? read, string? status, int? page, bool fromDetails = false)
    {
        if (await FindAsync(type, id) is not { } lead) return NotFound();
        lead.IsRead = !lead.IsRead;
        await db.SaveChangesAsync();
        return fromDetails
            ? DetailsRedirect(type!, id)
            : RedirectToAction(nameof(Index), Clean(listType, read, status).Route(page));
    }

    /// <summary>The workflow status and the internal note of one lead (the detail screen's form).</summary>
    [HttpPost]
    public async Task<IActionResult> Update(LeadUpdateInput input)
    {
        if (await FindAsync(input.Type, input.Id) is not { } lead) return NotFound();
        var status = LeadStatuses.Parse(input.Status);
        if (!ModelState.IsValid || status is null)
        {
            TempData["Error"] = ModelState.IsValid ? L(MessageKeys.InvalidValueNoField) : JoinErrors();
            return DetailsRedirect(input.Type!, input.Id);
        }
        lead.Status = status.Value;
        lead.AdminNote = Optional(input.AdminNote);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.LeadUpdated);
        return DetailsRedirect(input.Type!, input.Id);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string? type, int id, string? listType, string? read, string? status, int? page)
    {
        if (await FindAsync(type, id) is not { } lead) return NotFound();
        db.Remove(lead);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.LeadDeleted);
        return RedirectToAction(nameof(Index), Clean(listType, read, status).Route(page));
    }

    /// <summary>
    /// The leads of the current filter as CSV for Excel (UTF-8 with a BOM, so Arabic opens correctly), newest first. A GET
    /// behind the admin role that changes nothing. Cells go through <see cref="LeadCsv"/> (quoted; formula-like values
    /// prefixed with an apostrophe).
    /// </summary>
    public async Task<IActionResult> Export(string? type, string? read, string? status)
    {
        var filter = Clean(type, read, status);
        var rows = await Apply(LeadInbox.Query(db, filter.Type), filter)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToListAsync();

        var csv = new StringBuilder();
        LeadCsv.AppendRow(csv, ["النوع", "الرقم", "التاريخ (UTC)", "الاسم", "البريد الإلكتروني", "الهاتف", "التفاصيل", "الحالة",
            "مقروء", "لغة الصفحة", "الملاحظة الداخلية"]);
        foreach (var r in rows)
        {
            LeadCsv.AppendRow(csv,
            [
                LeadInbox.Label(r.Type), r.Id.ToString(CultureInfo.InvariantCulture),
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.Name, r.Email, r.Phone, r.Summary,
                L(LeadStatuses.LabelKey(r.Status)), r.IsRead ? "نعم" : "لا", r.Culture == "en" ? "English" : "العربية", r.Note,
            ]);
        }
        Response.Headers.CacheControl = "no-store";
        return File(LeadCsv.Utf8WithBom(csv.ToString()), "text/csv; charset=utf-8",
            $"leads-{filter.Type ?? "all"}-{DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.csv");
    }

    private RedirectToActionResult DetailsRedirect(string type, int id) => RedirectToAction(nameof(Details), new { id, type });

    /// <summary>The tracked lead of that type and id; a type that does not match the row's table finds nothing.</summary>
    private async Task<ILead?> FindAsync(string? type, int id) => type switch
    {
        LeadInbox.Quick or LeadInbox.Forum => await db.Registrations.FirstOrDefaultAsync(r => r.Id == id && r.Kind == type),
        LeadInbox.Investor or LeadInbox.Startup => await db.NetworkApplications.FirstOrDefaultAsync(a => a.Id == id && a.Kind == type),
        LeadInbox.Sponsorship => await db.SponsorshipEnquiries.FirstOrDefaultAsync(s => s.Id == id),
        LeadInbox.Interest => await db.OpportunityInterests.FirstOrDefaultAsync(i => i.Id == id),
        _ => null,
    };

    private async Task<LeadDetailVm?> LoadAsync(string type, int id)
    {
        static LeadField F(string label, string? value, bool ltr = false) => new(label, value, ltr);
        LeadDetailVm Vm(ILead lead, string title, DateTime created, DateTime? consent, string culture, LeadField[] fields) =>
            new(type, lead.Id, title, lead.IsRead, lead.Status, lead.AdminNote, created, consent, culture, fields);

        switch (type)
        {
            case LeadInbox.Quick or LeadInbox.Forum:
            {
                var r = await db.Registrations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Kind == type);
                return r is null ? null : Vm(r, r.FullName, r.CreatedAt, r.ConsentAt, r.Culture,
                [
                    F("الاسم", r.FullName), F("البريد الإلكتروني", r.Email, true), F("الهاتف", r.Phone, true),
                    F("الجهة", r.Organization), F("المسمى الوظيفي", r.JobTitle), F("الدولة", r.Country),
                    F("مجال الاهتمام", r.Interest), F("الفعالية", r.EventTitle ?? (type == LeadInbox.Forum ? "أي فعالية" : null)),
                ]);
            }
            case LeadInbox.Investor or LeadInbox.Startup:
            {
                var a = await db.NetworkApplications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Kind == type);
                if (a is null) return null;
                LeadField[] fields = type == LeadInbox.Investor
                    ?
                    [
                        F("الاسم", a.FullName), F("البريد الإلكتروني", a.Email, true), F("الهاتف", a.Phone, true), F("الدولة", a.Country),
                        F("الجهة", a.Organization), F("نوع المستثمر", a.InvestorType), F("حجم الاستثمار", a.TicketSize),
                        F("القطاعات التي تهمه", a.Sectors), F("رابط LinkedIn أو الموقع", a.LinkedInUrl, true), F("الرسالة", a.Message),
                    ]
                    :
                    [
                        F("اسم المؤسس", a.FullName), F("البريد الإلكتروني", a.Email, true), F("الهاتف", a.Phone, true), F("الدولة", a.Country),
                        F("اسم الشركة الناشئة", a.Organization), F("القطاع", a.Sectors), F("المرحلة", a.Stage),
                        F("التمويل المطلوب", a.FundingSought), F("الموقع أو العرض التقديمي", a.WebsiteUrl, true), F("النبذة", a.Pitch),
                    ];
                return Vm(a, a.FullName, a.CreatedAt, a.ConsentAt, a.Culture, fields);
            }
            case LeadInbox.Sponsorship:
            {
                var s = await db.SponsorshipEnquiries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (s is null) return null;
                // The unit's name is its page title (Pages, admin-editable), as on the public form.
                var snapshot = await cache.GetAsync(HttpContext.RequestAborted);
                var unitTitle = snapshot.Page(s.Unit == SponsorshipEnquiry.Forum ? PageKeys.Forum : PageKeys.Tv)?.TitleAr ?? s.Unit;
                return Vm(s, s.CompanyName, s.CreatedAt, s.ConsentAt, s.Culture,
                [
                    F("الشركة", s.CompanyName), F("مسؤول التواصل", s.ContactName), F("البريد الإلكتروني", s.Email, true),
                    F("الهاتف", s.Phone, true), F("الرعاية المطلوبة", unitTitle), F("الباقة", s.PackageTitle), F("الرسالة", s.Message),
                ]);
            }
            case LeadInbox.Interest:
            {
                var i = await db.OpportunityInterests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return i is null ? null : Vm(i, i.FullName, i.CreatedAt, i.ConsentAt, i.Culture,
                [
                    F("الاسم", i.FullName), F("البريد الإلكتروني", i.Email, true), F("الهاتف", i.Phone, true),
                    F("الفرصة", i.OpportunityTitle ?? "الفرص الاستثمارية عموماً"), F("الرسالة", i.Message),
                ]);
            }
            default:
                return null;
        }
    }
}

/// <summary>POST /Admin/Leads/Update: the lead (type + id), its new status and its internal note.</summary>
public sealed class LeadUpdateInput
{
    public string? Type { get; set; }
    public int Id { get; set; }

    [Required(ErrorMessage = MessageKeys.ValueRequiredNoField)]
    public string? Status { get; set; }

    [Display(Name = MessageKeys.Fields.AdminNote)]
    [StringLength(LeadLimits.NoteMax, ErrorMessage = MessageKeys.MaxLength)]
    public string? AdminNote { get; set; }
}

public sealed record LeadsIndexVm(PagedListViewModel<LeadRow> List, LeadsController.Filter Filter, UnreadCounts Unread);

public sealed record LeadField(string Label, string? Value, bool Ltr);

public sealed record LeadDetailVm(string Type, int Id, string Title, bool IsRead, LeadStatus Status, string? AdminNote,
    DateTime CreatedAt, DateTime? ConsentAt, string Culture, IReadOnlyList<LeadField> Fields)
{
    /// <summary>The list filter to return to.</summary>
    public LeadsController.Filter Back { get; init; } = new(null, null, null);
    public int? Page { get; init; }
}
