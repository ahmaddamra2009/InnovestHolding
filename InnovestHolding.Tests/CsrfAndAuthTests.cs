using System.Net;
using System.Reflection;
using InnovestHolding.Controllers.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

[Collection("App")]
public class DatabaseIsolationTests(TestAppFactory f)
{
    [Fact]
    public void App_uses_the_LocalDB_test_database()
    {
        Assert.Equal(TestDb.Name("InnovestHolding_Tests"), f.EffectiveDatabase);
        Assert.StartsWith(@"(localdb)\InnovestLocal", f.EffectiveDataSource, StringComparison.OrdinalIgnoreCase);
    }
}

[Collection("App")]
public class CsrfTests(TestAppFactory f)
{
    [Fact]
    public async Task Contact_without_token_is_refused_and_writes_nothing()
    {
        var client = f.NewClient();
        await client.GetAsync("/"); // has the antiforgery cookie, but sends no form token
        var email = Unique("csrf") + "@example.com";
        var response = await client.PostAsync("/Home/Contact",
            Form(("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m")));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact]
    public async Task Register_without_token_is_refused_and_writes_nothing()
    {
        var client = f.NewClient();
        await client.GetAsync("/");
        var email = Unique("csrf") + "@example.com";
        var response = await client.PostAsync("/Home/Register",
            Form(("FullName", "QA"), ("Email", email), ("InterestOptionId", f.InterestId)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await f.DbAsync(db => db.Registrations.CountAsync(r => r.Email == email)));
    }

    [Fact]
    public async Task Login_without_token_is_refused_even_with_correct_credentials()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        await client.GetAsync("/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(("Username", user), ("Password", password)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task Admin_post_without_token_is_refused_and_writes_nothing()
    {
        var client = await f.SignedInClientAsync();
        var name = Unique("partner");
        var response = await client.PostAsync("/Admin/Partners/Create", Form(("NameAr", name)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await f.DbAsync(db => db.Partners.CountAsync(p => p.NameAr == name)));
    }

    [Fact]
    public async Task Logout_without_token_is_refused_and_session_survives()
    {
        var client = await f.SignedInClientAsync();
        var response = await client.PostAsync("/Account/Logout", Form());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/Dashboard")).StatusCode);
    }

    [Fact] // positive control
    public async Task Contact_with_token_is_accepted_and_written()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("ok") + "@example.com";
        var response = await client.PostAsync("/Home/Contact",
            Form(token, ("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.JsonAsync()).GetProperty("success").GetBoolean());
        Assert.Equal(1, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact]
    public async Task Public_forms_render_an_antiforgery_token_and_the_honeypot()
    {
        var html = await (await f.NewClient().GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "name=\"__RequestVerificationToken\"").Count);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "name=\"Website\" class=\"hp-field\"").Count);
    }
}

[Collection("App")]
public class AdminAuthTests(TestAppFactory f)
{
    public static readonly TheoryData<string> AdminRoutes =
    [
        "/Admin/Dashboard", "/Admin/Content/Edit", "/Admin/Events", "/Admin/Events/Create", "/Admin/Faq",
        "/Admin/Faq/Create", "/Admin/Messages", "/Admin/Nav", "/Admin/News", "/Admin/News/Create",
        "/Admin/Partners", "/Admin/Platforms", "/Admin/Registrations", "/Admin/Settings", "/Admin/Stats",
        "/Admin/InterestOptions", "/Admin/RegisterBenefits", "/Admin/SocialLinks",
        "/Admin/Pages", "/Admin/Pages/Edit/about", "/Admin/ContentItems", "/Admin/ContentItems?group=values",
        "/Admin/SiteTexts", "/Admin/SiteTexts?tab=messages", "/Admin/Brand",
        "/Admin/PageSections", "/Admin/PageSections/Index/forum"
    ];

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task Anonymous_is_redirected_to_login(string route)
    {
        var response = await f.NewClient().GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public void Every_admin_controller_requires_the_Admin_role()
    {
        var controllers = typeof(AdminControllerBase).Assembly.GetTypes()
            .Where(t => t.Namespace == "InnovestHolding.Controllers.Admin" && !t.IsAbstract
                        && typeof(Controller).IsAssignableFrom(t))
            .ToList();

        // An empty scan cannot pass: 15 + Pages, ContentItems, PartnerGroups + Speakers, TvGuests, TvEpisodes,
        // Opportunities, SponsorshipPackages + Leads + (C5) SiteTexts, Brand, PageSections.
        Assert.Equal(27, controllers.Count);
        Assert.All(controllers, t => Assert.True(typeof(AdminControllerBase).IsAssignableFrom(t), t.Name));

        var authorize = typeof(AdminControllerBase).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", authorize?.Roles);
        // No action may opt out.
        Assert.DoesNotContain(controllers.SelectMany(t => t.GetMethods()),
            m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
    }

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task Signed_in_admin_gets_the_page(string route)
    {
        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task Anonymous_post_with_a_valid_token_is_still_redirected_to_login()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/Account/Login");
        var name = Unique("anon-partner");
        var response = await client.PostAsync("/Admin/Partners/Create", Form(token, ("NameAr", name)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Equal(0, await f.DbAsync(db => db.Partners.CountAsync(p => p.NameAr == name)));
    }
}
