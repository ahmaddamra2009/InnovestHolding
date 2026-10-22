using System.Net;
using System.Text.Json;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure.Leads;
using InnovestHolding.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The admin applications inbox (/Admin/Leads): authorization, list/filter, detail, read state, delete.</summary>
[Collection("App")]
public class LeadInboxTests(TestAppFactory f)
{
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

        var list = await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=unread");
        Assert.Contains(email, list);
        Assert.Contains("<time class=\"utc-time\" datetime=\"", list);
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=investor"));
        Assert.Contains(email, await client.GetStringAsync("/Admin/Leads"));            // the unified list
        Assert.DoesNotContain(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=read"));

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
        Assert.Contains(email, await client.GetStringAsync("/Admin/Leads?type=sponsorship&status=read"));

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
}

/// <summary>The AddLeadForms migration on a database in the multi-page state (RefineSiteStructure) holding real rows.</summary>
[Collection("App")]
public class LeadFormsMigrationTests
{
    private const string Before = "20261022172550_RefineSiteStructure";
    private const string LeadForms = "20261022194726_AddLeadForms";

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
            INSERT INTO Events (TitleAr, DescriptionAr, EventDay, EventMonthAr, LocationAr, TimeAr, IsActive, SortOrder, CreatedAt)
              VALUES (N'فعالية حقيقية', N'وصف', 5, N'نوفمبر', N'القاهرة', N'مساءً', 1, 1, '2026-09-01T10:00:00');
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
        Assert.Empty(Infrastructure.Content.ContentKeys.All.Select(k => k.Key).Except(keys));

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
