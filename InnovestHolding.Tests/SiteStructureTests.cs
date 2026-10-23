using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InnovestHolding.Controllers;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The multi-page site: route table, default route, normaliser, forms, robots, sitemap, menus, 404.</summary>
[Collection("App")]
public class SiteStructureTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com";

    public static TheoryData<string, bool> AllPages()
    {
        var data = new TheoryData<string, bool>();
        foreach (var p in SiteRoutes.All)
        {
            data.Add(p.Key, false);
            data.Add(p.Key, true);
        }
        return data;
    }

    private static string? Robots(HttpResponseMessage r) =>
        r.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : null;

    [Theory]
    [MemberData(nameof(AllPages))]
    public async Task Every_page_of_the_route_table_answers_200_in_its_language_with_one_h1(string key, bool english)
    {
        var url = CultureUrls.Page(key, english);
        var r = await f.NewClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains(english ? "<html lang=\"en\" dir=\"ltr\">" : "<html lang=\"ar\" dir=\"rtl\">", html);
        Assert.Single(Regex.Matches(html, "<h1[ >]"));
        // The language switch is a real link to the same page in the other language.
        Assert.Contains($"id=\"lang-switch\" href=\"{CultureUrls.Page(key, !english)}\"", html);
        // Every internal page link stays in the page's language.
        var links = Regex.Matches(html, "<a [^>]*href=\"(/[^\"#?]*)\"").Select(m => m.Groups[1].Value)
            .Where(h => SiteRoutes.MatchCanonical(h) is not null && h != CultureUrls.Page(key, !english)).ToList();
        Assert.NotEmpty(links);
        Assert.All(links, h => Assert.Equal(english, CultureUrls.IsEnglishPath(h)));
    }

    [Theory] // admin controllers are not reachable at the site root any more
    [InlineData("/Dashboard")]
    [InlineData("/Events")]
    [InlineData("/Faq")]
    [InlineData("/Partners")]
    [InlineData("/Content/Edit")]
    [InlineData("/Registrations")]
    public async Task Admin_controller_names_at_the_root_are_the_404_page(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Contains(Msg.Ar(MessageKeys.Ui.NotFoundTitle), await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_under_admin_still_redirects_anonymous_users_to_login()
    {
        var r = await f.NewClient().GetAsync("/Admin/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.StartsWith("/Account/Login", r.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public void No_public_controller_shares_a_name_with_an_admin_controller()
    {
        var all = typeof(HomeController).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(Controller).IsAssignableFrom(t)).ToList();
        var admin = all.Where(t => t.IsSubclassOf(typeof(AdminControllerBase))).Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var publicNames = all.Where(t => !t.IsSubclassOf(typeof(AdminControllerBase))).Select(t => t.Name).ToList();
        Assert.True(admin.Count >= 15 && publicNames.Count >= 9, $"{admin.Count} admin, {publicNames.Count} public controllers");
        Assert.DoesNotContain(publicNames, admin.Contains);
    }

    [Theory] // one hop to the canonical URL
    [InlineData("/About", "/about")]
    [InlineData("/about/", "/about")]
    [InlineData("/EN/About/", "/en/about")]
    [InlineData("/ar/about", "/about")]
    [InlineData("/Forum/Events", "/forum/events")]
    [InlineData("/en/Angel-Network/Join?x=1", "/en/angel-network/join?x=1")]
    public async Task Aliases_of_a_page_are_one_301_to_the_canonical_url(string path, string location)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, r.StatusCode);
        Assert.Equal(location, r.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync(location)).StatusCode);
    }

    [Theory]
    [InlineData("/unknown/")]
    [InlineData("/Unknown")]
    [InlineData("/ar/nothing-here")]
    public async Task Unknown_paths_are_not_redirected_but_404(string path) =>
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(path)).StatusCode);

    [Fact]
    public async Task Unknown_url_is_the_branded_404_in_the_url_language_and_noindex()
    {
        var en = await f.NewClient().GetAsync("/en/xyz");
        Assert.Equal(HttpStatusCode.NotFound, en.StatusCode);
        Assert.Equal(RobotsHeader.Value, Robots(en));
        var html = await en.Content.ReadAsStringAsync();
        Assert.Contains("<html lang=\"en\" dir=\"ltr\">", html);
        Assert.Contains(Msg.En(MessageKeys.Ui.NotFoundTitle), html);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\">", html);
        Assert.Contains("id=\"lang-switch\" href=\"/\"", html);

        var ar = await f.NewClient().GetAsync("/xyz");
        Assert.Equal(HttpStatusCode.NotFound, ar.StatusCode);
        Assert.Contains(Msg.Ar(MessageKeys.Ui.NotFoundTitle), await ar.Content.ReadAsStringAsync());

        // A missing static file keeps the plain 404 (no page rendered for it).
        var css = await f.NewClient().GetAsync("/css/missing.css");
        Assert.Equal(HttpStatusCode.NotFound, css.StatusCode);
        Assert.DoesNotContain(Msg.Ar(MessageKeys.Ui.NotFoundTitle), await css.Content.ReadAsStringAsync());
    }

    [Fact] // the one-page anchors still land on a section of the home page
    public async Task Home_keeps_the_section_ids_of_the_one_page_site()
    {
        foreach (var path in new[] { "/", "/en" })
        {
            var html = await f.NewClient().GetStringAsync(path);
            foreach (var id in new[] { "hero", "about", "platforms", "stats", "news", "events", "partners", "faq", "register", "contact" })
                Assert.Contains($"id=\"{id}\"", html);
        }
    }

    // ── Forms ──

    private static (string, string)[] Contact(string email) => [("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m")];

    [Fact]
    public async Task Forms_post_to_forms_urls_in_both_languages_and_the_old_arabic_alias_still_works()
    {
        var email = Unique("forms") + "@example.com";
        var ar = f.NewClient();
        var tAr = await ar.GetTokenAsync("/contact");
        var en = f.NewClient();
        var tEn = await en.GetTokenAsync("/en/contact");
        Assert.Contains("action=\"/en/forms/contact\"", await en.GetStringAsync("/en/contact"));

        var r1 = await ar.PostAsync("/forms/contact", Form(tAr, Contact(email)));
        Assert.Equal(Msg.Ar(MessageKeys.ContactSuccess), (await r1.JsonAsync()).GetProperty("message").GetString());
        var r2 = await en.PostAsync("/en/forms/contact", Form(tEn, Contact(email)));
        Assert.Equal(Msg.En(MessageKeys.ContactSuccess), (await r2.JsonAsync()).GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/Home/Contact", Form(tAr, Contact(email)))).StatusCode);
        Assert.Equal(["ar", "en", "ar"], await f.DbAsync(db => db.ContactMessages.Where(m => m.Email == email).OrderBy(m => m.Id).Select(m => m.Culture).ToListAsync()));

        // The English endpoints of the bilingual release are gone: /en/contact is a GET-only page, so a POST reaches no action but the
        // fallback (the 404 page) — nothing is written.
        Assert.Equal(HttpStatusCode.NotFound, (await en.PostAsync("/en/Contact", Form(tEn, Contact(email)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await en.PostAsync("/en/Register", Form(tEn, ("FullName", "QA"), ("Email", email)))).StatusCode);
        Assert.Equal(3, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact]
    public async Task Forms_refuse_a_post_without_an_antiforgery_token()
    {
        var client = f.NewClient();
        await client.GetAsync("/contact");
        var email = Unique("noaf") + "@example.com";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/forms/contact", Form(Contact(email)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/en/forms/register", Form(("FullName", "QA"), ("Email", email)))).StatusCode);
        Assert.Equal(0, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact] // rate limit: per action, every URL of the action in one bucket, the 429 in JSON in the page language
    public async Task Register_rate_limit_is_json_in_the_page_language_and_shared_by_its_urls()
    {
        var ip = NewIp();
        var ar = f.NewClient(ip);
        var tAr = await ar.GetTokenAsync("/forum/registration");
        var en = f.NewClient(ip);
        var tEn = await en.GetTokenAsync("/en/forum/registration");
        var email = Unique("rl") + "@example.com";
        FormUrlEncodedContent Body(string t) => Form(t, ("FullName", "QA"), ("Email", email), ("InterestOptionId", f.InterestId));

        Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/forms/register", Body(tAr))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/Home/Register", Body(tAr))).StatusCode);
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.OK, (await en.PostAsync("/en/forms/register", Body(tEn))).StatusCode);
        var limited = await en.PostAsync("/en/forms/register", Body(tEn));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.TooManyRequests), (await limited.JsonAsync()).GetProperty("message").GetString());
        var limitedAr = await ar.PostAsync("/forms/register", Body(tAr));
        Assert.Equal(Msg.Ar(MessageKeys.TooManyRequests), (await limitedAr.JsonAsync()).GetProperty("message").GetString());
        Assert.Equal(5, await f.DbAsync(db => db.Registrations.CountAsync(r => r.Email == email)));
    }

    // ── Indexability, sitemap ──

    [Theory]
    [InlineData("/forum/speakers")]
    [InlineData("/en/forum/speakers")]
    [InlineData("/angel-network/opportunities")]
    [InlineData("/tv/episodes")] // (/tv has its intro since C5 AddAdminControl: indexable)
    public async Task A_page_with_no_content_yet_is_200_with_its_empty_text_and_noindex_follow(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(RobotsHeader.FollowValue, Robots(r));
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("<meta name=\"robots\" content=\"noindex, follow\">", html);
        Assert.Contains("class=\"empty-text\"", html);
        Assert.DoesNotContain("hreflang=\"x-default\"", html);
    }

    [Theory]
    [InlineData("/forum/events")]
    [InlineData("/en/news")]
    [InlineData("/about")]
    [InlineData("/en/contact")]
    public async Task A_page_with_content_is_indexable_with_reciprocal_hreflang(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Null(Robots(r));
        var html = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("noindex", html);
        var key = SiteRoutes.MatchCanonical(path)!.Key;
        Assert.Contains($"<link rel=\"canonical\" href=\"{Base}{path}\">", html);
        Assert.Contains($"<link rel=\"alternate\" hreflang=\"ar\" href=\"{Base}{CultureUrls.Page(key, false)}\">", html);
        Assert.Contains($"<link rel=\"alternate\" hreflang=\"en\" href=\"{Base}{CultureUrls.Page(key, true)}\">", html);
    }

    [Fact]
    public async Task Sitemap_lists_exactly_the_indexable_pages_and_follows_the_content()
    {
        async Task<List<string>> Locs()
        {
            var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
            XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
            return xml.Root!.Elements(sm + "url").Select(u => u.Element(sm + "loc")!.Value).ToList();
        }

        var locs = await Locs();
        Assert.Contains($"{Base}/forum/events", locs);
        Assert.Contains($"{Base}/en/forum/events", locs);
        Assert.DoesNotContain($"{Base}/forum/speakers", locs);
        // C5 AddAdminControl: the TV hub has its intro and the privacy page its body → both listed (intended change).
        Assert.Contains($"{Base}/tv", locs);
        Assert.Contains($"{Base}/privacy", locs);
        Assert.DoesNotContain(locs, l => l.Contains("/Admin", StringComparison.OrdinalIgnoreCase) || l.Contains("/forms"));
        // Each listed URL answers 200 without a noindex header.
        foreach (var loc in locs)
            Assert.Null(Robots(await f.NewClient().GetAsync(loc[Base.Length..])));

        // Every active event deactivated → the events page drops out of the sitemap and becomes noindex, follow.
        var active = await f.DbAsync(db => db.Events.Where(e => e.IsActive).Select(e => e.Id).ToListAsync());
        try
        {
            await f.DbAsync(async db => { foreach (var e in db.Events) e.IsActive = false; return await db.SaveChangesAsync(); });
            Assert.DoesNotContain($"{Base}/forum/events", await Locs());
            Assert.Equal(RobotsHeader.FollowValue, Robots(await f.NewClient().GetAsync("/forum/events")));
        }
        finally
        {
            await f.DbAsync(async db => { foreach (var e in db.Events.Where(e => active.Contains(e.Id))) e.IsActive = true; return await db.SaveChangesAsync(); });
        }
        Assert.Contains($"{Base}/forum/events", await Locs());
    }

    [Fact] // English page without its English meta: rendered (Arabic fallback), noindex, follow; no hreflang either side
    public async Task English_page_without_english_meta_is_noindex_follow()
    {
        var original = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.About));
        try
        {
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.About)).MetaDescriptionEn = null; return await db.SaveChangesAsync(); });
            var en = await f.NewClient().GetAsync("/en/about");
            Assert.Equal(HttpStatusCode.OK, en.StatusCode);
            Assert.Equal(RobotsHeader.FollowValue, Robots(en));
            var ar = await f.NewClient().GetAsync("/about");
            Assert.Null(Robots(ar));
            Assert.DoesNotContain("<link rel=\"alternate\" hreflang=\"en\"", await ar.Content.ReadAsStringAsync());
        }
        finally
        {
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.About)).MetaDescriptionEn = original.MetaDescriptionEn; return await db.SaveChangesAsync(); });
        }
    }

    [Theory]
    [InlineData("/forum/events?page=1", HttpStatusCode.MovedPermanently)]
    [InlineData("/forum/events?page=99", HttpStatusCode.NotFound)]
    [InlineData("/news?page=abc", HttpStatusCode.NotFound)]
    [InlineData("/news?page=0", HttpStatusCode.NotFound)]
    public async Task List_page_numbers_out_of_range_are_404_and_page_one_is_the_bare_url(string url, HttpStatusCode status)
    {
        var r = await f.NewClient().GetAsync(url);
        Assert.Equal(status, r.StatusCode);
        if (status == HttpStatusCode.MovedPermanently) Assert.Equal("/forum/events", r.Headers.Location!.OriginalString);
    }

    // ── Admin: pages ──

    [Fact]
    public async Task Hiding_a_page_removes_it_from_the_menus_but_not_from_its_url_and_a_title_edit_shows_at_once()
    {
        var client = await f.SignedInClientAsync();
        var original = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.ForumSpeakers));
        var url = CultureUrls.Page(PageKeys.ForumSpeakers, false);
        string Link() => $"href=\"{url}\"";
        await f.NewClient().GetStringAsync("/"); // warm the cache
        Assert.Contains(Link(), await f.NewClient().GetStringAsync("/"));
        try
        {
            var token = await client.GetTokenAsync("/Admin/Pages");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Admin/Pages/ToggleMenu/{PageKeys.ForumSpeakers}", Form(token))).StatusCode);
            var home = await f.NewClient().GetStringAsync("/");
            Assert.DoesNotContain(Link(), home);                                       // header, drawer, footer
            Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync(url)).StatusCode); // still reachable

            token = await client.GetTokenAsync($"/Admin/Pages/Edit/{PageKeys.ForumSpeakers}");
            var title = "متحدثون " + Guid.NewGuid().ToString("N")[..6];
            var r = await client.PostAsync($"/Admin/Pages/Edit/{PageKeys.ForumSpeakers}", Form(token,
                ("TitleAr", title), ("TitleEn", "Speakers QA"), ("ShowInMenu", "true"), ("EmptyTextAr", "قريباً جداً"),
                // C5: the form always posts its publication checkboxes (an omitted checkbox = unchecked = unpublished).
                ("IsPublished", "true"), ("ShowInFooter", original.ShowInFooter ? "true" : "false")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var page = await f.NewClient().GetStringAsync(url);
            Assert.Contains($">{title}</h1>", page);
            Assert.Contains("قريباً جداً", page);
            Assert.Contains(Link(), await f.NewClient().GetStringAsync("/")); // shown again
            Assert.Contains(">Speakers QA</h1>", await f.NewClient().GetStringAsync("/en/forum/speakers"));
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.Pages.SingleAsync(p => p.Key == PageKeys.ForumSpeakers);
                row.TitleAr = original.TitleAr; row.TitleEn = original.TitleEn; row.ShowInMenu = original.ShowInMenu;
                row.EmptyTextAr = original.EmptyTextAr; row.EmptyTextEn = original.EmptyTextEn;
                row.IsPublished = original.IsPublished; row.ShowInFooter = original.ShowInFooter;
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Pages_admin_refuses_unknown_keys_anonymous_users_and_posts_without_a_token()
    {
        var anonymous = f.NewClient();
        var r = await anonymous.GetAsync("/Admin/Pages/Edit/about");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.StartsWith("/Account/Login", r.Headers.Location!.PathAndQuery);

        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/Pages/Edit/not-a-page")).StatusCode);
        var before = await f.DbAsync(db => db.Pages.Where(p => p.Key == "about").Select(p => p.TitleAr).SingleAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Pages/Edit/about", Form(("TitleAr", "x")))).StatusCode);
        var token = await client.GetTokenAsync("/Admin/Pages/Edit/about");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Admin/Pages/Edit/about", Form(token, ("TitleAr", "")))).StatusCode); // re-rendered with the error
        Assert.Equal(before, await f.DbAsync(db => db.Pages.Where(p => p.Key == "about").Select(p => p.TitleAr).SingleAsync()));
    }

    [Fact]
    public async Task Content_items_admin_accepts_only_registry_groups_and_known_icons_and_a_new_item_shows_on_its_page()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/ContentItems?group=sectors");
        var text = "قطاع " + Guid.NewGuid().ToString("N")[..6];
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/ContentItems/Create", Form(token, ("Group", "not_a_group"), ("TextAr", text)))).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/ContentItems/Create", Form(token, ("Group", "sectors"), ("TextAr", text), ("IconKey", "<svg onload=x>")))).StatusCode);
            Assert.Equal(0, await f.DbAsync(db => db.ContentItems.CountAsync(i => i.TextAr == text)));

            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/ContentItems/Create", Form(token, ("Group", "sectors"), ("TextAr", text), ("TextEn", "QA sector"), ("IconKey", "globe")))).StatusCode);
            Assert.Contains(text, await f.NewClient().GetStringAsync("/about"));
            Assert.Contains("QA sector", await f.NewClient().GetStringAsync("/en/angel-network/startups"));
        }
        finally
        {
            await f.DbAsync(db => db.ContentItems.Where(i => i.TextAr == text).ExecuteDeleteAsync());
            f.Services.GetRequiredService<Infrastructure.Content.ISiteContentCache>().Invalidate();
        }
    }

    [Fact] // the profile content reaches the pages, from the database
    public async Task Profile_content_and_real_data_are_on_the_new_pages()
    {
        var contact = WebUtility.HtmlDecode(await f.NewClient().GetStringAsync("/contact")); // "+" is written as &#x2B;
        Assert.Contains("info@innovest-holding.com", contact);
        Assert.Contains("+20 01141410808", contact);
        Assert.Contains("https://wa.me/201141410808", contact);
        var about = await f.NewClient().GetStringAsync("/en/about");
        Assert.Contains("To become the leading Arab platform connecting innovation with investment", about);
        Assert.Contains("Sustainability", about);
        var partners = await f.NewClient().GetStringAsync("/forum/partners");
        Assert.Contains("بنك الاستثمار", partners); // the existing partners are listed
        var news = await f.NewClient().GetStringAsync("/news");
        Assert.Contains("إطلاق برنامج تدريبي جديد في ريادة الأعمال والابتكار", news); // the existing news
    }
}
