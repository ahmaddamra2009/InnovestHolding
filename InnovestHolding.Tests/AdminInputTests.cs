using System.Net;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

[Collection("App")]
public class OverPostingTests(TestAppFactory f)
{
    private async Task<(HttpClient Client, string Token)> AdminAsync(string page)
    {
        var client = await f.SignedInClientAsync();
        return (client, await client.GetTokenAsync(page));
    }

    private static async Task<string?> ErrorBannerAfterRedirect(HttpClient client, HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var page = await (await client.GetAsync(response.Headers.Location)).Content.ReadAsStringAsync();
        return page.Contains("alert alert-error") ? page : null;
    }

    [Fact]
    public async Task Event_create_ignores_over_posted_id_and_created_at()
    {
        var (c, t) = await AdminAsync("/Admin/Events/Create");
        var title = Unique("event");
        var response = await c.PostAsync("/Admin/Events/Create", Form(t,
            ("TitleAr", title), ("EventDay", "5"), ("EventMonthAr", "يوليو"), ("IsActive", "true"),
            ("Id", "999999"), ("CreatedAt", "2000-01-01"), ("SortOrder", "-50")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var row = await f.DbAsync(db => db.Events.AsNoTracking().SingleAsync(e => e.TitleAr == title));
        Assert.NotEqual(999999, row.Id);
        Assert.True(row.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
        Assert.True(row.SortOrder > 0);
    }

    [Theory]
    [InlineData("", "5")]
    [InlineData("LONG", "5")]
    [InlineData("ok", "abc")]
    [InlineData("ok", "32")]
    public async Task Invalid_event_rerenders_the_form_and_saves_nothing(string title, string day)
    {
        var (c, t) = await AdminAsync("/Admin/Events/Create");
        var month = Unique("m")[..20];
        if (title == "LONG") title = new string('t', 201);
        var before = await f.DbAsync(db => db.Events.CountAsync());
        var response = await c.PostAsync("/Admin/Events/Create",
            Form(t, ("TitleAr", title), ("EventDay", day), ("EventMonthAr", month)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("class=\"form-errors", html);
        Assert.Contains($"value=\"{month}\"", html); // the admin's input is kept
        Assert.Equal(before, await f.DbAsync(db => db.Events.CountAsync()));
    }

    [Fact]
    public async Task Content_edit_updates_known_keys_only()
    {
        var (c, t) = await AdminAsync("/Admin/Content/Edit");
        var evilKey = Unique("evil")[..30];
        var value = Unique("years")[..30];
        var original = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "about_years").Select(s => s.ContentAr).SingleAsync());
        try
        {
            var response = await c.PostAsync("/Admin/Content/Edit",
                Form(t, ($"content[{evilKey}].Ar", "x"), ("content[about_years].Ar", value)));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(0, await f.DbAsync(db => db.SiteContents.CountAsync(s => s.SectionKey == evilKey)));
            Assert.Equal(value, await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "about_years").Select(s => s.ContentAr).SingleAsync()));
        }
        finally
        {
            await f.DbAsync(async db => { (await db.SiteContents.SingleAsync(s => s.SectionKey == "about_years")).ContentAr = original; return await db.SaveChangesAsync(); });
        }
    }

    [Fact]
    public async Task Content_value_over_limit_rerenders_and_saves_nothing()
    {
        var (c, t) = await AdminAsync("/Admin/Content/Edit");
        var original = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "vision_text").Select(s => s.ContentAr).SingleAsync());
        var response = await c.PostAsync("/Admin/Content/Edit", Form(t, ("content[vision_text].Ar", new string('v', 4001))));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("class=\"form-errors", await response.Content.ReadAsStringAsync());
        Assert.Equal(original, await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "vision_text").Select(s => s.ContentAr).SingleAsync()));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,x")]
    [InlineData("//evil.example")]
    public async Task Nav_rejects_unsafe_href(string href)
    {
        var (c, t) = await AdminAsync("/Admin/Nav");
        var title = Unique("nav")[..30];
        var response = await c.PostAsync("/Admin/Nav/Create", Form(t, ("TitleAr", title), ("Href", href)));
        Assert.NotNull(await ErrorBannerAfterRedirect(c, response));
        Assert.Equal(0, await f.DbAsync(db => db.NavItems.CountAsync(n => n.TitleAr == title)));
    }

    [Fact]
    public async Task Nav_accepts_a_safe_href()
    {
        var (c, t) = await AdminAsync("/Admin/Nav");
        var title = Unique("nav")[..30];
        var response = await c.PostAsync("/Admin/Nav/Create", Form(t, ("TitleAr", title), ("Href", "#about")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(1, await f.DbAsync(db => db.NavItems.CountAsync(n => n.TitleAr == title)));
    }

    [Fact]
    public async Task Partner_name_over_201_chars_is_refused_not_500()
    {
        var (c, t) = await AdminAsync("/Admin/Partners");
        var name = new string('p', 201);
        var response = await c.PostAsync("/Admin/Partners/Create", Form(t, ("NameAr", name)));
        Assert.NotNull(await ErrorBannerAfterRedirect(c, response));
        Assert.Equal(0, await f.DbAsync(db => db.Partners.CountAsync(p => p.NameAr.Length > 200)));
    }

    [Fact]
    public async Task Stat_with_non_numeric_value_is_refused_not_500()
    {
        var (c, t) = await AdminAsync("/Admin/Stats");
        var stat = await f.DbAsync(db => db.Stats.AsNoTracking().FirstAsync());
        var response = await c.PostAsync("/Admin/Stats/Update",
            Form(t, ("id", stat.Id.ToString()), ("LabelAr", "x"), ("value", "abc"), ("suffix", "+")));
        Assert.NotNull(await ErrorBannerAfterRedirect(c, response));
        Assert.Equal(stat.LabelAr, await f.DbAsync(db => db.Stats.Where(s => s.Id == stat.Id).Select(s => s.LabelAr).SingleAsync()));
    }

    [Fact]
    public async Task Change_password_too_short_rerenders_without_echoing_passwords()
    {
        var (c, t) = await AdminAsync("/Admin/Settings");
        var response = await c.PostAsync("/Admin/Settings/ChangePassword",
            Form(t, ("CurrentPassword", "secret-current"), ("NewPassword", "short"), ("ConfirmPassword", "short")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("class=\"form-errors", html);
        Assert.DoesNotContain("secret-current", html);
    }
}
