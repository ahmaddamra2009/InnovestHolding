using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Leads;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Controllers.Admin;

/// <summary>
/// «الطلبات» — the applications inbox: every lead of the public forms in one list (filter by type and read state,
/// newest first, paged), a detail screen per lead, mark read/unread, delete. Leads are personal data: admin-only, never
/// logged. The quick registrations stay listed in «التسجيلات» and contact messages in «رسائل التواصل» as before.
/// </summary>
public class LeadsController(AppDbContext db) : AdminControllerBase
{
    public const string Unread = "unread";
    public const string Read = "read";

    private static string? CleanType(string? type) => LeadInbox.IsType(type) ? type : null;
    private static string? CleanStatus(string? status) => status is Unread or Read ? status : null;

    public async Task<IActionResult> Index(string? type, string? status, int? page)
    {
        type = CleanType(type);
        status = CleanStatus(status);
        var query = LeadInbox.Query(db, type);
        if (status == Unread) query = query.Where(r => !r.IsRead);
        else if (status == Read) query = query.Where(r => r.IsRead);

        var total = await query.CountAsync();
        var current = PagedListViewModel<LeadRow>.Clamp(page, total, PageSize);
        var rows = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((current - 1) * PageSize).Take(PageSize).ToListAsync();
        var unread = await LeadInbox.UnreadAsync(db);

        return View("~/Views/Admin/Leads/Index.cshtml", new LeadsIndexVm(
            new PagedListViewModel<LeadRow> { Items = rows, Page = current, PageSize = PageSize, TotalCount = total, UnreadCount = unread.Leads },
            type, status, unread));
    }

    public async Task<IActionResult> Details(string? type, int id, string? status, int? page)
    {
        var detail = CleanType(type) is { } t ? await LoadAsync(t, id) : null;
        if (detail is null) return NotFound();
        return View("~/Views/Admin/Leads/Details.cshtml", detail with { Status = CleanStatus(status), Page = page > 1 ? page : null });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleRead(string? type, int id, string? status, int? page, bool fromDetails = false)
    {
        type = CleanType(type);
        var changed = type switch
        {
            LeadInbox.Quick or LeadInbox.Forum => await Toggle(db.Registrations.Where(r => r.Id == id && r.Kind == type)),
            LeadInbox.Investor or LeadInbox.Startup => await Toggle(db.NetworkApplications.Where(a => a.Id == id && a.Kind == type)),
            LeadInbox.Sponsorship => await Toggle(db.SponsorshipEnquiries.Where(s => s.Id == id)),
            LeadInbox.Interest => await Toggle(db.OpportunityInterests.Where(i => i.Id == id)),
            _ => false,
        };
        if (!changed) return NotFound();
        return fromDetails
            ? RedirectToAction(nameof(Details), new { type, id, status = CleanStatus(status), page = page > 1 ? page : null })
            : BackToIndex(status, page);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string? type, int id, string? status, int? page)
    {
        type = CleanType(type);
        object? row = type switch
        {
            LeadInbox.Quick or LeadInbox.Forum => await db.Registrations.FirstOrDefaultAsync(r => r.Id == id && r.Kind == type),
            LeadInbox.Investor or LeadInbox.Startup => await db.NetworkApplications.FirstOrDefaultAsync(a => a.Id == id && a.Kind == type),
            LeadInbox.Sponsorship => await db.SponsorshipEnquiries.FindAsync(id),
            LeadInbox.Interest => await db.OpportunityInterests.FindAsync(id),
            _ => null,
        };
        if (row is null) return NotFound();
        db.Remove(row);
        await db.SaveChangesAsync();
        TempData["Success"] = L(MessageKeys.Admin.LeadDeleted);
        return BackToIndex(status, page);
    }

    /// <summary>Back to the list with the filter the admin was on (values re-validated, never a URL from the request).</summary>
    private IActionResult BackToIndex(string? status, int? page) =>
        RedirectToAction(nameof(Index), new
        {
            type = CleanType(Request.Form["listType"].FirstOrDefault()),
            status = CleanStatus(status),
            page = page > 1 ? page : null,
        });

    private async Task<bool> Toggle<T>(IQueryable<T> rows) where T : class
    {
        var row = await rows.FirstOrDefaultAsync();
        if (row is null) return false;
        var entry = db.Entry(row).Property<bool>("IsRead");
        entry.CurrentValue = !entry.CurrentValue;
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<LeadDetailVm?> LoadAsync(string type, int id)
    {
        static LeadField F(string label, string? value, bool ltr = false) => new(label, value, ltr);

        switch (type)
        {
            case LeadInbox.Quick or LeadInbox.Forum:
            {
                var r = await db.Registrations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Kind == type);
                return r is null ? null : new LeadDetailVm(type, r.Id, r.FullName, r.IsRead, r.CreatedAt, r.ConsentAt, r.Culture,
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
                return new LeadDetailVm(type, a.Id, a.FullName, a.IsRead, a.CreatedAt, a.ConsentAt, a.Culture, fields);
            }
            case LeadInbox.Sponsorship:
            {
                var s = await db.SponsorshipEnquiries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return s is null ? null : new LeadDetailVm(type, s.Id, s.CompanyName, s.IsRead, s.CreatedAt, s.ConsentAt, s.Culture,
                [
                    F("الشركة", s.CompanyName), F("مسؤول التواصل", s.ContactName), F("البريد الإلكتروني", s.Email, true),
                    F("الهاتف", s.Phone, true), F("الرعاية المطلوبة", s.Unit == SponsorshipEnquiry.Forum ? "منتدى إنوفست العربي" : "البرنامج التلفزيوني"),
                    F("الباقة", s.PackageTitle), F("الرسالة", s.Message),
                ]);
            }
            case LeadInbox.Interest:
            {
                var i = await db.OpportunityInterests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return i is null ? null : new LeadDetailVm(type, i.Id, i.FullName, i.IsRead, i.CreatedAt, i.ConsentAt, i.Culture,
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

public sealed record LeadsIndexVm(PagedListViewModel<LeadRow> List, string? Type, string? Status, UnreadCounts Unread);

public sealed record LeadField(string Label, string? Value, bool Ltr);

public sealed record LeadDetailVm(string Type, int Id, string Title, bool IsRead, DateTime CreatedAt, DateTime? ConsentAt,
    string Culture, IReadOnlyList<LeadField> Fields)
{
    /// <summary>The list filter to return to.</summary>
    public string? Status { get; init; }
    public int? Page { get; init; }
}
