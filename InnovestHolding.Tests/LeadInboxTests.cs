using System.Net;
using System.Text.Json;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Leads;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The admin applications inbox (/Admin/Leads): authorization, list/filter, detail, read state, delete.</summary>
[Collection("App")]
public class LeadInboxTests(TestAppFactory f)
{
    [Fact] // Nader P3-4: the dashboard names the sections as the sidebar does and no longer describes the old menu editor
    public async Task Dashboard_uses_the_current_admin_names()
    {
        var html = await (await f.SignedInClientAsync()).GetStringAsync("/Admin/Dashboard");
        Assert.Contains("<p>الخدمات</p>", html);
        Assert.Contains("<p>روابط إضافية في القائمة</p>", html);
        Assert.DoesNotContain("<p>المنصات</p>", html);
        Assert.DoesNotContain("عناصر فرعية", html);
        Assert.DoesNotContain("عناصر القائمة", html);
    }

    private async Task<(int Id, string Email)> AddSponsorshipAsync()
    {
        var email = Unique("inbox") + "@example.com";
        var id = await f.DbAsync(async db =>
        {
            var row = new SponsorshipEnquiry
            {
                Unit = SponsorshipEnquiry.Tv, CompanyName = "Inbox Co " + email, ContactName = "Inbox Person", Email = email,
                Message = "line one\nline <b>two</b>", ConsentAt = DateTime.UtcNow, Culture = "en", CreatedAt = DateTime.UtcNow,
            };
            db.SponsorshipEnquiries.Add(row);
            await db.SaveChangesAsync();
            return row.Id;
        });
        return (id, email);
    }

    [Theory]
    [InlineData("/Admin/Leads")]
    [InlineData("/Admin/Leads/Details/1?type=sponsorship")]
    public async Task Anonymous_is_sent_to_the_login_page(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Anonymous_post_is_refused_and_changes_nothing()
    {
        var (id, _) = await AddSponsorshipAsync();
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/Account/Login");
        var r = await client.PostAsync("/Admin/Leads/Delete", Form(token, ("type", "sponsorship"), ("id", id.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);
        Assert.True(await f.DbAsync(db => db.SponsorshipEnquiries.AnyAsync(s => s.Id == id)));
    }

    [Fact]
    public async Task Admin_lists_filters_reads_toggles_and_deletes_a_lead()
    {
        var (id, email) = await AddSponsorshipAsync();
        var client = await f.SignedInClientAsync();

        var list = await client.GetStringAsync("/Admin/Leads?type=sponsorship&read=unread");
        Assert.Contains(email, list);
        Assert.Contains("<time class=\"utc-time\" datetime=\"", list);
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=investor"));
        Assert.Contains(email, await client.GetStringAsync("/Admin/Leads"));            // the unified list
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&read=read"));

        var detail = await client.GetStringAsync($"/Admin/Leads/Details/{id}?type=sponsorship");
        Assert.Contains("Inbox Person", detail);
        Assert.Contains("line &lt;b&gt;two&lt;/b&gt;", detail);   // encoded, never raw HTML
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=interest")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=nonsense")).StatusCode);

        var token = ExtractToken(detail);
        var toggled = await client.PostAsync("/Admin/Leads/ToggleRead", Form(token, ("type", "sponsorship"), ("id", id.ToString()), ("listType", "sponsorship")));
        Assert.Equal(HttpStatusCode.Redirect, toggled.StatusCode);
        Assert.Equal("/Admin/Leads?type=sponsorship", toggled.Headers.Location!.OriginalString);
        Assert.True(await f.DbAsync(db => db.SponsorshipEnquiries.Where(s => s.Id == id).Select(s => s.IsRead).SingleAsync()));
        Assert.Contains(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&read=read"));

        // the type must match the row's table: a sponsorship id posted as an interest deletes nothing
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/Delete", Form(token, ("type", "interest"), ("id", id.ToString())))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Leads/Delete", Form(("type", "sponsorship"), ("id", id.ToString())))).StatusCode);
        Assert.True(await f.DbAsync(db => db.SponsorshipEnquiries.AnyAsync(s => s.Id == id)));

        var deleted = await client.PostAsync("/Admin/Leads/Delete", Form(token, ("type", "sponsorship"), ("id", id.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.False(await f.DbAsync(db => db.SponsorshipEnquiries.AnyAsync(s => s.Id == id)));
    }

    [Fact]
    public async Task Registrations_keep_their_page_and_join_the_inbox_by_kind()
    {
        var quick = Unique("quick") + "@example.com";
        var forum = Unique("forumreg") + "@example.com";
        await f.DbAsync(async db =>
        {
            db.Registrations.Add(new Registration { FullName = "Quick One", Email = quick, Culture = "ar", CreatedAt = DateTime.UtcNow });
            db.Registrations.Add(new Registration { Kind = Registration.Forum, FullName = "Forum One", Email = forum, Culture = "ar", ConsentAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
            return await db.SaveChangesAsync();
        });
        var client = await f.SignedInClientAsync();
        var page = await client.GetStringAsync("/Admin/Registrations");
        Assert.Contains(quick, page);
        Assert.Contains(forum, page);
        Assert.Contains(quick, await client.GetStringAsync("/Admin/Leads?type=quick"));
        Assert.DoesNotContain(forum, await client.GetStringAsync("/Admin/Leads?type=quick"));
        Assert.Contains(forum, await client.GetStringAsync("/Admin/Leads?type=forum"));
        Assert.Equal(Registration.Quick, await f.DbAsync(db => db.Registrations.Where(r => r.Email == quick).Select(r => r.Kind).SingleAsync()));
    }

    [Fact]
    public async Task Unread_counts_match_the_tables()
    {
        await AddSponsorshipAsync();
        var counts = await f.DbAsync(db => LeadInbox.UnreadAsync(db));
        Assert.Equal(await f.DbAsync(db => db.SponsorshipEnquiries.CountAsync(s => !s.IsRead)), counts.Sponsorship);
        Assert.Equal(await f.DbAsync(db => db.Registrations.CountAsync(s => !s.IsRead && s.Kind == "quick")), counts.Quick);
        Assert.Equal(await f.DbAsync(db => db.ContactMessages.CountAsync(s => !s.IsRead)), counts.Messages);
        var client = await f.SignedInClientAsync();
        Assert.Contains($"<span class=\"sidebar-badge\">{counts.Leads}</span>", await client.GetStringAsync("/Admin/Dashboard"));
    }

    private async Task<int> AddQuickAsync(string email) => await f.DbAsync(async db =>
    {
        var r = new Registration { FullName = "Quick Row", Email = email, Culture = "ar", CreatedAt = DateTime.UtcNow };
        db.Registrations.Add(r);
        await db.SaveChangesAsync();
        return r.Id;
    });

    [Fact] // a quick registration's id posted as a forum lead finds nothing on every action
    public async Task A_type_that_does_not_match_the_row_is_404_on_details_toggle_update_and_delete()
    {
        var email = Unique("mismatch") + "@example.com";
        var id = await AddQuickAsync(email);
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Leads");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=forum")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/ToggleRead", Form(token, ("type", "forum"), ("id", id.ToString())))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/Update", Form(token, ("Type", "forum"), ("Id", id.ToString()), ("Status", "closed")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/Delete", Form(token, ("type", "forum"), ("id", id.ToString())))).StatusCode);
        var row = await f.DbAsync(db => db.Registrations.AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal((false, LeadStatus.New, Registration.Quick), (row.IsRead, row.Status, row.Kind));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=quick")).StatusCode);
    }

    [Fact] // the same for the Join table: an investor application's id posted as a startup finds nothing
    public async Task An_investor_id_posted_as_startup_is_404_on_details_toggle_update_and_delete()
    {
        var email = Unique("inv") + "@example.com";
        var id = await f.DbAsync(async db =>
        {
            var a = new NetworkApplication { Kind = NetworkApplication.Investor, FullName = "Investor Row", Email = email, Country = "Egypt",
                Culture = "ar", ConsentAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
            db.NetworkApplications.Add(a);
            await db.SaveChangesAsync();
            return a.Id;
        });
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Leads");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=startup")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/ToggleRead", Form(token, ("type", "startup"), ("id", id.ToString())))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/Update", Form(token, ("Type", "startup"), ("Id", id.ToString()), ("Status", "closed")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/Leads/Delete", Form(token, ("type", "startup"), ("id", id.ToString())))).StatusCode);
        var row = await f.DbAsync(db => db.NetworkApplications.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal((false, LeadStatus.New, NetworkApplication.Investor), (row.IsRead, row.Status, row.Kind));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Admin/Leads/Details/{id}?type=investor")).StatusCode);
    }

    [Fact] // C4 Y4: saving or marking on the detail screen returns to it with the list filter it was opened from
    public async Task Save_and_mark_on_the_detail_screen_keep_the_list_filter()
    {
        var (id, _) = await AddSponsorshipAsync();
        var client = await f.SignedInClientAsync();
        var detail = await client.GetStringAsync($"/Admin/Leads/Details/{id}?type=sponsorship&listType=sponsorship&read=unread&status=new&page=2");
        Assert.Contains("href=\"/Admin/Leads?type=sponsorship&amp;read=unread&amp;status=new&amp;page=2\"", detail);
        // the forms of the detail page carry the filter
        Assert.Contains("name=\"ListType\" value=\"sponsorship\"", detail);
        Assert.Contains("name=\"ListPage\" value=\"2\"", detail);
        Assert.Contains("name=\"listType\" value=\"sponsorship\"", detail);
        var token = ExtractToken(detail);
        var expected = $"/Admin/Leads/Details/{id}?type=sponsorship&listType=sponsorship&read=unread&status=new&page=2";

        var saved = await client.PostAsync("/Admin/Leads/Update", Form(token, ("Type", "sponsorship"), ("Id", id.ToString()), ("Status", "contacted"),
            ("AdminNote", ""), ("ListType", "sponsorship"), ("ListRead", "unread"), ("ListStatus", "new"), ("ListPage", "2")));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal(expected, saved.Headers.Location!.OriginalString);
        var invalid = await client.PostAsync("/Admin/Leads/Update", Form(token, ("Type", "sponsorship"), ("Id", id.ToString()), ("Status", "bad"),
            ("ListType", "sponsorship"), ("ListRead", "unread"), ("ListStatus", "new"), ("ListPage", "2")));
        Assert.Equal(expected, invalid.Headers.Location!.OriginalString);

        var marked = await client.PostAsync("/Admin/Leads/ToggleRead", Form(token, ("type", "sponsorship"), ("id", id.ToString()), ("fromDetails", "true"),
            ("listType", "sponsorship"), ("read", "unread"), ("status", "new"), ("page", "2")));
        Assert.Equal(expected, marked.Headers.Location!.OriginalString);

        // a hostile filter is dropped, never echoed into the redirect
        var hostile = await client.PostAsync("/Admin/Leads/Update", Form(token, ("Type", "sponsorship"), ("Id", id.ToString()), ("Status", "new"),
            ("ListType", "//evil.example"), ("ListRead", "x"), ("ListStatus", "2"), ("ListPage", "-1")));
        Assert.Equal($"/Admin/Leads/Details/{id}?type=sponsorship", hostile.Headers.Location!.OriginalString);
    }

    [Fact] // C4 F2: the five read-state indexes hold unread rows only; the two kind-split ones include Kind
    public async Task Read_state_indexes_are_filtered_to_unread_rows()
    {
        var rows = await f.DbAsync(db => db.Database.SqlQueryRaw<string>("""
            SELECT OBJECT_NAME(i.object_id) + '|' + ISNULL(i.filter_definition, '-') + '|' +
                   ISNULL((SELECT STRING_AGG(c.name, ',') FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1), '-') AS [Value]
            FROM sys.indexes i WHERE i.name LIKE 'IX[_]%[_]IsRead'
            """).ToListAsync());
        Assert.Equal(
            ["ContactMessages|([IsRead]=(0))|-", "NetworkApplications|([IsRead]=(0))|Kind", "OpportunityInterests|([IsRead]=(0))|-",
             "Registrations|([IsRead]=(0))|Kind", "SponsorshipEnquiries|([IsRead]=(0))|-"],
            rows.Order());
    }

    [Fact] // C4 F3: the inbox list (and the dashboard) share one unread-count query with the layout's sidebar badges
    public async Task Admin_pages_run_the_unread_count_query_once()
    {
        var sink = new CapturingLoggerProvider();
        await using var app = f.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(sink)));
        var (user, password) = await f.CreateAdminAsync();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostLoginAsync(user, password)).StatusCode);
        foreach (var path in new[] { "/Admin/Leads", "/Admin/Dashboard" })
        {
            sink.Lines.Clear();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            var commands = sink.Lines.Where(l => l.StartsWith("Microsoft.EntityFrameworkCore.Database.Command|Executed DbCommand")).ToList();
            Assert.NotEmpty(commands);
            Assert.Single(commands, l => l.Contains("AS Messages"));
        }
    }

    [Fact]
    public async Task Status_and_note_are_saved_by_an_antiforgery_post_and_filter_the_list()
    {
        var (id, email) = await AddSponsorshipAsync();
        var client = await f.SignedInClientAsync();
        var detail = await client.GetStringAsync($"/Admin/Leads/Details/{id}?type=sponsorship");
        var token = ExtractToken(detail);
        FormUrlEncodedContent Body(string? tk, string status, string note) =>
            Form(tk, ("Type", "sponsorship"), ("Id", id.ToString()), ("Status", status), ("AdminNote", note));

        // anonymous and token-less posts change nothing
        var anon = f.NewClient();
        var anonToken = await anon.GetTokenAsync("/Account/Login");
        Assert.Contains("/Account/Login", (await anon.PostAsync("/Admin/Leads/Update", Body(anonToken, "closed", "x"))).Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Leads/Update", Body(null, "closed", "x"))).StatusCode);
        // only the three keys; a number or a casing variant is not a status; an over-long note is refused
        foreach (var bad in new[] { "2", "Closed", "archived", "" })
            await client.PostAsync("/Admin/Leads/Update", Body(token, bad, "x"));
        await client.PostAsync("/Admin/Leads/Update", Body(token, "closed", new string('n', LeadLimits.NoteMax + 1)));
        var row = await f.DbAsync(db => db.SponsorshipEnquiries.AsNoTracking().SingleAsync(s => s.Id == id));
        Assert.Equal((LeadStatus.New, (string?)null), (row.Status, row.AdminNote));

        var ok = await client.PostAsync("/Admin/Leads/Update", Body(token, "contacted", "Called <b>Monday</b>"));
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        row = await f.DbAsync(db => db.SponsorshipEnquiries.AsNoTracking().SingleAsync(s => s.Id == id));
        Assert.Equal((LeadStatus.Contacted, "Called <b>Monday</b>"), (row.Status, row.AdminNote));
        Assert.Contains("Called &lt;b&gt;Monday&lt;/b&gt;", await client.GetStringAsync($"/Admin/Leads/Details/{id}?type=sponsorship"));

        Assert.Contains(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=contacted"));
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=new"));
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=closed"));
    }

    [Fact]
    public async Task Export_is_admin_only_csv_with_bom_that_respects_the_filter_and_neutralises_formulas()
    {
        var formulaName = "=HYPERLINK(\"http://x\")" + Guid.NewGuid().ToString("N");
        var email = Unique("csv") + "@example.com";
        await f.DbAsync(async db =>
        {
            db.OpportunityInterests.Add(new OpportunityInterest { FullName = formulaName, Email = email, Phone = "+20 1", Message = "m",
                ConsentAt = DateTime.UtcNow, Culture = "ar", CreatedAt = DateTime.UtcNow, Status = LeadStatus.Closed, AdminNote = "@note" });
            return await db.SaveChangesAsync();
        });
        // another type with the SAME status: only the type filter keeps it out of the file
        var (otherId, otherEmail) = await AddSponsorshipAsync();
        await f.DbAsync(db => db.SponsorshipEnquiries.Where(s => s.Id == otherId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, LeadStatus.Closed)));

        var anon = await f.NewClient().GetAsync("/Admin/Leads/Export");
        Assert.Equal(HttpStatusCode.Redirect, anon.StatusCode);
        Assert.Contains("/Account/Login", anon.Headers.Location!.OriginalString);

        var client = await f.SignedInClientAsync();
        var r = await client.GetAsync("/Admin/Leads/Export?type=interest&status=closed");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv", r.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("attachment", r.Content.Headers.ContentDisposition!.ToString());
        Assert.Equal("no-store", r.Headers.CacheControl!.ToString());
        // streamed (C4 F5): not a buffered body sent with its length (NonValidated: the ContentLength getter would compute one)
        Assert.False(r.Content.Headers.NonValidated.Contains("Content-Length"));
        var bytes = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var text = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.StartsWith(LeadCsv.Header(k => Msg.Ar(k)), text);   // the Arabic resx headers (admin requests are Arabic)
        Assert.StartsWith("\"النوع\",", text);
        Assert.Contains("\"'" + formulaName.Replace("\"", "\"\"") + "\"", text);   // apostrophe + doubled quotes
        Assert.Contains("\"'@note\"", text);
        Assert.Contains("\"" + Msg.Ar(MessageKeys.Admin.LeadStatusClosed) + "\"", text);
        Assert.Contains(email, text);
        Assert.DoesNotContain(otherEmail, text);   // the type filter is applied
        Assert.DoesNotContain(email, await (await client.GetAsync("/Admin/Leads/Export?type=interest&status=new")).Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("+20 100", "\"'+20 100\"")]
    [InlineData("-5", "\"'-5\"")]
    [InlineData("@SUM(A1)", "\"'@SUM(A1)\"")]
    [InlineData("\tx", "\"'\tx\"")]
    [InlineData("\rx", "\"'\rx\"")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    [InlineData("نص عربي", "\"نص عربي\"")]
    [InlineData(null, "\"\"")]
    // C4 L4: fullwidth forms, the pipe, and whitespace before a formula character
    [InlineData("＝1+1", "\"'＝1+1\"")]
    [InlineData("＋20", "\"'＋20\"")]
    [InlineData("－5", "\"'－5\"")]
    [InlineData("＠SUM(A1)", "\"'＠SUM(A1)\"")]
    [InlineData("|calc", "\"'|calc\"")]
    [InlineData(" =1+1", "\"' =1+1\"")]
    [InlineData("\n=1+1", "\"'\n=1+1\"")]
    [InlineData("  \t+1", "\"'  \t+1\"")]
    [InlineData("a=b", "\"a=b\"")]
    [InlineData("  text", "\"  text\"")]
    public void Csv_cells_are_quoted_and_formula_prefixes_neutralised(string? value, string expected) =>
        Assert.Equal(expected, LeadCsv.Cell(value));

    [Fact] // headers and words come from the resx of the culture asked for (English here; admin requests are Arabic)
    public void Csv_header_and_words_follow_the_resx_language()
    {
        Assert.Equal("\"Type\",\"No.\",\"Date (UTC)\",\"Name\",\"Email\",\"Phone\",\"Details\",\"Status\",\"Read\",\"Page language\",\"Internal note\"," +
            "\"Reference (opportunity / package / event)\",\"Reference ID\"\r\n",
            LeadCsv.Header(k => Msg.En(k)));
        Assert.EndsWith("\"الملاحظة الداخلية\",\"المرجع (الفرصة / الباقة / الفعالية)\",\"رقم المرجع\"\r\n", LeadCsv.Header(k => Msg.Ar(k)));
        var row = LeadCsv.Row(new LeadRow { Type = LeadInbox.Interest, Id = 7, Name = "N", Email = "e@x", Summary = "", Culture = "en", IsRead = true,
            Status = LeadStatus.Contacted, CreatedAt = new DateTime(2026, 10, 1, 9, 5, 0), Reference = "فرصة", ReferenceId = 12 }, k => Msg.En(k));
        Assert.EndsWith("\"Contacted\",\"Yes\",\"English\",\"\",\"فرصة\",\"12\"\r\n", row);
        Assert.Contains("\"7\",\"2026-10-01 09:05\"", row);
    }
}

/// <summary>The AddLeadForms migration on a database in the state before it (AddEcosystemEntities) holding real rows.</summary>
[Collection("App")]
public class LeadFormsMigrationTests
{
    private const string Before = "20261022213309_AddEcosystemEntities";
    private const string LeadForms = "20261022221815_AddLeadForms";

    private static async Task<string> Json(AppDbContext db, string sql) =>
        (await db.Database.SqlQueryRaw<string>(sql).ToListAsync()).Single() ?? "";

    private const string OldRegistrations =
        "SELECT (SELECT Id, FullName, Email, Phone, Interest, InterestOptionId, Culture, IsRead, CreatedAt FROM Registrations ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";
    private const string OldMessages = "SELECT (SELECT * FROM ContactMessages ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";
    private const string OldEvents = "SELECT (SELECT * FROM Events ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]";

    private static Task<string> Intro(AppDbContext db, string key, string column) =>
        // column is one of two constants of this file; the key travels as a parameter
        db.Database.SqlQueryRaw<string>("SELECT ISNULL([" + (column == "IntroEn" ? "IntroEn" : "IntroAr") + "], N'<NULL>') AS [Value] FROM Pages WHERE [Key] = {0}", key).SingleAsync();

    [Fact]
    public async Task Up_keeps_every_row_and_admin_edit_and_Down_restores_the_previous_state()
    {
        await using var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Before);

        // Real data of an existing install + three admin edits the migration must not touch.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Events (Slug, TitleAr, DescriptionAr, EventDay, EventMonthAr, LocationAr, TimeAr, IsActive, SortOrder, CreatedAt)
              VALUES ('real-event', N'فعالية حقيقية', N'وصف', 5, N'نوفمبر', N'القاهرة', N'مساءً', 1, 1, '2026-09-01T10:00:00');
            INSERT INTO Registrations (FullName, Email, Phone, Interest, InterestOptionId, Culture, IsRead, CreatedAt)
              VALUES (N'زائر', N'real-1@example.com', N'+20 1', N'الاستثمار', NULL, 'ar', 0, '2026-10-01T10:00:00'),
                     (N'Visitor', N'real-2@example.com', N'', N'', NULL, 'en', 1, '2026-10-02T10:00:00');
            INSERT INTO ContactMessages (FullName, Email, Subject, Message, Culture, IsRead, CreatedAt)
              VALUES (N'مرسل', N'real-3@example.com', N'موضوع', N'رسالة', 'ar', 0, '2026-10-03T10:00:00');
            UPDATE Pages SET IntroEn = N'Admin wrote this.' WHERE [Key] = 'network.join';
            INSERT INTO ContentItems ([Group], TextAr, TextEn, SortOrder, IsActive, CreatedAt) VALUES ('investor_types', N'نوع أضافه المشرف', NULL, 1, 1, SYSUTCDATETIME());
            INSERT INTO SiteContents (SectionKey, ContentAr, ContentEn, UpdatedAt) VALUES ('forms_consent', N'موافقة المشرف', NULL, SYSUTCDATETIME());
            """);
        var regs = await Json(db, OldRegistrations);
        var msgs = await Json(db, OldMessages);
        var events = await Json(db, OldEvents);
        var joinIntroAr = await Intro(db, "network.join", "IntroAr");

        await migrator.MigrateAsync(LeadForms);

        Assert.Equal(regs, await Json(db, OldRegistrations));
        Assert.Equal(msgs, await Json(db, OldMessages));
        Assert.Equal(events, await Json(db, OldEvents));
        Assert.Equal(["quick", "quick"], await db.Registrations.OrderBy(r => r.Id).Select(r => r.Kind).ToListAsync());
        Assert.All(await db.Registrations.ToListAsync(), r => Assert.True(r.Status == LeadStatus.New && r.AdminNote is null));
        Assert.All(await db.Registrations.ToListAsync(), r => Assert.True(r.ConsentAt is null && r.EventId is null && r.EventTitle is null));
        Assert.Equal(0, await db.NetworkApplications.CountAsync() + await db.SponsorshipEnquiries.CountAsync() + await db.OpportunityInterests.CountAsync());

        // admin edits kept; unedited values updated
        Assert.Equal(["نوع أضافه المشرف"], await db.ContentItems.Where(i => i.Group == "investor_types").Select(i => i.TextAr).ToListAsync());
        Assert.Equal(4, await db.ContentItems.CountAsync(i => i.Group == "ticket_sizes"));
        Assert.Equal(5, await db.ContentItems.CountAsync(i => i.Group == "startup_stages"));
        Assert.Equal(4, await db.ContentItems.CountAsync(i => i.Group == "funding_ranges"));
        Assert.Equal("موافقة المشرف", await db.SiteContents.Where(c => c.SectionKey == "forms_consent").Select(c => c.ContentAr).SingleAsync());
        Assert.Equal(7, await db.SiteContents.CountAsync(c => new[] { "join_kind_title", "join_side_title", "join_side_text", "sponsorship_form_title",
            "sponsorship_form_text", "interest_form_title", "interest_form_text" }.Contains(c.SectionKey)));
        Assert.Equal("Admin wrote this.", await Intro(db, "network.join", "IntroEn"));
        Assert.Contains("املأ النموذج أدناه", await Intro(db, "network.join", "IntroAr"));
        Assert.StartsWith("سواء كنت ترغب", await Intro(db, "tv.sponsorship", "IntroAr"));
        Assert.StartsWith("Whether you want", await Intro(db, "tv.sponsorship", "IntroEn"));
        // every content key of the registry exists after the migration
        var keys = await db.SiteContents.Select(c => c.SectionKey).ToListAsync();
        Assert.Empty(Infrastructure.Content.ContentKeys.All.Select(k => k.Key).Except(AdminLinksTests.AddedByAdminControl)
            .Except(Migrations.AddForumContent.NewKeys.Select(k => k[0])).Except(keys));

        var after = await StateAsync(db);

        // Down: the previous schema and values come back; the admin's rows and edits stay.
        await migrator.MigrateAsync(Before);
        Assert.Equal(regs, await Json(db, OldRegistrations));
        Assert.Equal(msgs, await Json(db, OldMessages));
        Assert.Equal(joinIntroAr, await Intro(db, "network.join", "IntroAr"));
        Assert.Equal("Admin wrote this.", await Intro(db, "network.join", "IntroEn"));
        Assert.Equal("", await Intro(db, "tv.sponsorship", "IntroAr"));
        Assert.Equal("<NULL>", await Intro(db, "tv.sponsorship", "IntroEn"));
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM ContentItems WHERE [Group] = 'investor_types'").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM ContentItems WHERE [Group] IN ('ticket_sizes','startup_stages','funding_ranges')").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM SiteContents WHERE SectionKey = 'forms_consent'").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('NetworkApplications','SponsorshipEnquiries','OpportunityInterests')").SingleAsync());

        // Up again reaches the same state.
        await migrator.MigrateAsync(LeadForms);
        Assert.Equal(after, await StateAsync(db));
    }

    private static async Task<string> StateAsync(AppDbContext db) =>
        await Json(db, OldRegistrations) + await Json(db, OldMessages)
        + await Json(db, "SELECT (SELECT [Group], TextAr, TextEn, SortOrder, IsActive FROM ContentItems ORDER BY [Group], SortOrder, TextAr FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]")
        + await Json(db, "SELECT (SELECT SectionKey, ContentAr, ContentEn FROM SiteContents ORDER BY SectionKey FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]")
        + await Json(db, "SELECT (SELECT [Key], IntroAr, IntroEn FROM Pages ORDER BY [Key] FOR JSON PATH, INCLUDE_NULL_VALUES) AS [Value]");
}
