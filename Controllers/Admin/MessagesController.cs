using System.Globalization;
using System.Text;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Leads;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace InnovestHolding.Controllers.Admin;

public class MessagesController : AdminControllerBase
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db) => _db = db;

    /// <summary>Newest first, <see cref="AdminControllerBase.PageSize"/> per page (index IX_ContactMessages_CreatedAt).</summary>
    public async Task<IActionResult> Index(int? page)
    {
        var total = await _db.ContactMessages.CountAsync();
        var unread = await _db.ContactMessages.CountAsync(m => !m.IsRead);
        var current = PagedListViewModel<ContactMessage>.Clamp(page, total, PageSize);
        var items = await _db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Skip((current - 1) * PageSize).Take(PageSize)
            .ToListAsync();
        return View("~/Views/Admin/Messages/Index.cshtml", new PagedListViewModel<ContactMessage>
        {
            Items = items, Page = current, PageSize = PageSize, TotalCount = total, UnreadCount = unread
        });
    }

    /// <summary>
    /// Every contact message as CSV for Excel (UTF-8 with a BOM), newest first, with the full message text (the list
    /// shows the first 80 characters). A GET behind the admin role that changes nothing; cells go through
    /// <see cref="LeadCsv"/> (quoted, formula-like values prefixed) and rows are streamed as they are read.
    /// </summary>
    public async Task Export()
    {
        var rows = _db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).AsAsyncEnumerable();
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = $"messages-{DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.csv",
        }.ToString();
        Response.Headers.CacheControl = "no-store";

        var ct = HttpContext.RequestAborted;
        await Response.Body.WriteAsync(Encoding.UTF8.GetPreamble(), ct);
        await using var writer = new StreamWriter(Response.Body, new UTF8Encoding(false), 16 * 1024, leaveOpen: true);
        await writer.WriteAsync(LeadCsv.Row(new[]
        {
            MessageKeys.Admin.CsvId, MessageKeys.Admin.CsvDate, MessageKeys.Admin.CsvName, MessageKeys.Admin.CsvEmail,
            MessageKeys.Admin.CsvSubject, MessageKeys.Admin.CsvMessage, MessageKeys.Admin.CsvRead, MessageKeys.Admin.CsvCulture,
        }.Select(k => L(k))));
        await foreach (var m in rows.WithCancellation(ct))
            await writer.WriteAsync(LeadCsv.Row(new[]
            {
                m.Id.ToString(CultureInfo.InvariantCulture), m.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                m.FullName, m.Email, m.Subject, m.Message,
                L(m.IsRead ? MessageKeys.Admin.CsvYes : MessageKeys.Admin.CsvNo),
                L(m.Culture == "en" ? MessageKeys.Admin.CsvEnglish : MessageKeys.Admin.CsvArabic),
            }));
        await writer.FlushAsync(ct);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, int? page)
    {
        var item = await _db.ContactMessages.FindAsync(id);
        if (item != null)
        {
            item.IsRead = !item.IsRead;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { page = page > 1 ? page : null });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int? page)
    {
        var item = await _db.ContactMessages.FindAsync(id);
        if (item != null)
        {
            _db.ContactMessages.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = L(MessageKeys.Admin.MessageDeleted);
        }
        return RedirectToAction(nameof(Index), new { page = page > 1 ? page : null });
    }
}
