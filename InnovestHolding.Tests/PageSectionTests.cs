using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 Dev 2: page sections (show/hide/order, counts), unpublished pages (branded 404), the 404 links and the empty-state
/// button (ARCHITECTURE-C5 T5.4–T5.6, T5.10 for PageSections). Every test restores what it changed.
/// </summary>
[Collection("App")]
public class PageSectionTests(TestAppFactory f)
{
    /// <summary>The home section ids of C4, in order (hero first; cta band has no id).</summary>
    private static readonly string[] HomeIds = ["hero", "about", "ecosystem", "platforms", "stats", "events", "news", "partners", "faq", "register", "contact"];

    private async Task<string> PageAsync(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadAsStringAsync();
    }

    private static List<string> SectionIds(string html) =>
        Regex.Matches(html, "<section class=\"[^\"]*\" id=\"([a-z]+)\"").Select(m => m.Groups[1].Value).ToList();

    /// <summary>Runs <paramref name="body"/> and puts the page's PageSections rows back exactly as they were.</summary>
    private async Task WithSectionsRestoredAsync(string page, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.PageSections.AsNoTracking().Where(r => r.PageKey == page)
            .Select(r => new { r.SectionKey, r.IsVisible, r.SortOrder }).ToListAsync());
        try { await body(); }
        finally
        {
            await f.DbAsync(async db =>
            {
                var rows = await db.PageSections.Where(r => r.PageKey == page).ToListAsync();
                foreach (var row in rows)
                    if (saved.FirstOrDefault(s => s.SectionKey == row.SectionKey) is { } s) { row.IsVisible = s.IsVisible; row.SortOrder = s.SortOrder; }
                return await db.SaveChangesAsync();
            });
        }
    }

    private async Task SetSectionAsync(string page, string section, bool? visible = null, int? order = null) =>
        await f.DbAsync(async db =>
        {
            var row = await db.PageSections.SingleAsync(r => r.PageKey == page && r.SectionKey == section);
            if (visible is { } v) row.IsVisible = v;
            if (order is { } o) row.SortOrder = o;
            return await db.SaveChangesAsync();
        });

    private async Task WithPageRestoredAsync(string page, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == page));
        try { await body(); }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.Pages.SingleAsync(p => p.Key == page);
                row.IsPublished = saved.IsPublished; row.EmptyActionPage = saved.EmptyActionPage;
                return await db.SaveChangesAsync();
            });
        }
    }

    private async Task WithContentRestoredAsync(string key, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == key));
        try { await body(); }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.SiteContents.SingleAsync(s => s.SectionKey == key);
                row.ContentAr = saved.ContentAr; row.ContentEn = saved.ContentEn;
                return await db.SaveChangesAsync();
            });
        }
    }

    private Task SetContentAsync(string key, string value) => f.DbAsync(async db =>
    {
        (await db.SiteContents.SingleAsync(s => s.SectionKey == key)).ContentAr = value;
        return await db.SaveChangesAsync();
    });

    // ── T5.5 Home ──

    [Theory]
    [InlineData("/")]
    [InlineData("/en")]
    public async Task Home_default_rows_render_the_C4_section_sequence(string path)
    {
        var html = await PageAsync(path);
        Assert.Equal(HomeIds, SectionIds(html));
        Assert.Contains("class=\"hero-stats", html);
        Assert.Contains("class=\"about-values\"", html);
        Assert.Contains("class=\"cta-band\"", html);
        Assert.DoesNotContain("section-anchor", html);
        // The migration's rows are what is rendered (not a code default): one row per registry section.
        var keys = await f.DbAsync(db => db.PageSections.Where(r => r.PageKey == PageKeys.Home).OrderBy(r => r.SortOrder).Select(r => r.SectionKey).ToListAsync());
        Assert.Equal(PageSectionRegistry.Of(PageKeys.Home).Select(d => d.Key), keys);
    }

    [Fact]
    public async Task Hiding_a_home_section_leaves_only_its_anchor_and_no_link_to_it()
    {
        await WithSectionsRestoredAsync(PageKeys.Home, async () =>
        {
            await SetSectionAsync(PageKeys.Home, "news", visible: false);
            await SetSectionAsync(PageKeys.Home, "hero_stats", visible: false);
            await SetSectionAsync(PageKeys.Home, "about_values", visible: false);
            await SetSectionAsync(PageKeys.Home, "cta_band", visible: false);
            foreach (var path in new[] { "/", "/en" })
            {
                var html = await PageAsync(path);
                Assert.Equal(HomeIds.Where(i => i != "news"), SectionIds(html));
                Assert.Contains("<span id=\"news\" class=\"section-anchor\"></span>", html);
                Assert.DoesNotContain("href=\"#news\"", html);
                Assert.DoesNotContain("class=\"news-section\"", html);
                Assert.DoesNotContain("class=\"hero-stats", html);
                Assert.DoesNotContain("class=\"about-values\"", html);
                Assert.DoesNotContain("class=\"cta-band\"", html);
                Assert.Contains("class=\"events-section\" id=\"events\"", html); // positive control
            }
        });
        Assert.Equal(HomeIds, SectionIds(await PageAsync("/")));
    }

    [Fact]
    public async Task Moving_a_home_section_changes_the_order_in_both_languages()
    {
        await WithSectionsRestoredAsync(PageKeys.Home, async () =>
        {
            await SetSectionAsync(PageKeys.Home, "partners", order: 75); // events 70, news 80
            foreach (var path in new[] { "/", "/en" })
            {
                var ids = SectionIds(await PageAsync(path));
                Assert.True(ids.IndexOf("partners") < ids.IndexOf("news"));
                Assert.True(ids.IndexOf("events") < ids.IndexOf("partners"));
                Assert.Equal("hero", ids[0]); // the hero never moves
            }
        });
    }

    [Fact]
    public async Task Home_counts_follow_their_content_keys_and_default_to_the_C4_numbers()
    {
        var events = await f.DbAsync(db => db.Events.CountAsync(e => e.IsActive));
        var stats = await f.DbAsync(db => db.Stats.CountAsync(s => s.IsActive));
        Assert.True(events >= 2 && stats >= 2, "seed has events and figures");
        var html = await PageAsync("/");
        Assert.Equal(Math.Min(3, events), CountEvents(html));
        Assert.Equal(Math.Min(4, stats), Regex.Matches(html, "class=\"hero-stat\"").Count);

        await WithContentRestoredAsync("home_events_limit", () => WithContentRestoredAsync("hero_stats_limit", async () =>
        {
            await SetContentAsync("home_events_limit", "1");
            await SetContentAsync("hero_stats_limit", "1");
            var after = await PageAsync("/en");
            Assert.Equal(1, CountEvents(after));
            Assert.Single(Regex.Matches(after, "class=\"hero-stat\""));
            // An invalid stored value → the C4 default.
            await SetContentAsync("hero_stats_limit", "99");
            Assert.Equal(Math.Min(4, stats), Regex.Matches(await PageAsync("/"), "class=\"hero-stat\"").Count);
        }));
    }

    private static string EventsSection(string html)
    {
        var start = html.IndexOf("id=\"events\"", StringComparison.Ordinal);
        if (start < 0) return "";
        var end = html.IndexOf("</section>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    private static int CountEvents(string html) =>
        Regex.Matches(EventsSection(html), "href=\"(/en)?/forum/events/[^\"/?]+\"").Select(m => m.Value).Distinct().Count();

    // ── T5.6 Inner pages ──

    [Fact]
    public async Task Inner_page_blocks_hide_and_move_per_page()
    {
        await WithSectionsRestoredAsync(PageKeys.About, () => WithSectionsRestoredAsync(PageKeys.NetworkStartups, async () =>
        {
            var aboutBefore = await PageAsync("/about");
            var startupsBefore = await PageAsync("/en/angel-network/startups");
            Assert.Contains("class=\"cta-band\"", aboutBefore);

            await SetSectionAsync(PageKeys.About, "cta_band", visible: false);
            await SetSectionAsync(PageKeys.NetworkStartups, "sectors", visible: false);
            foreach (var path in new[] { "/about", "/en/about" })
                Assert.DoesNotContain("class=\"cta-band\"", await PageAsync(path));
            Assert.Contains("class=\"cta-band\"", await PageAsync("/ecosystem")); // another page keeps its band
            // About keeps its sectors (the band is not a page-section): hiding Startups' sectors is per page.
            Assert.Equal(Regex.Matches(aboutBefore, "class=\"page-section").Count, Regex.Matches(await PageAsync("/about"), "class=\"page-section").Count);
            Assert.True(Regex.Matches(await PageAsync("/en/angel-network/startups"), "class=\"page-section").Count
                        < Regex.Matches(startupsBefore, "class=\"page-section").Count);

            // Move: vision/mission after the CTA band on About (cta band visible again for the order check).
            await SetSectionAsync(PageKeys.About, "cta_band", visible: true);
            var html = await PageAsync("/about");
            Assert.True(html.IndexOf("class=\"vision-grid\"", StringComparison.Ordinal) < html.IndexOf("class=\"cta-band\"", StringComparison.Ordinal));
            await SetSectionAsync(PageKeys.About, "vision", order: 1000);
            var moved = await PageAsync("/en/about");
            Assert.True(moved.IndexOf("class=\"vision-grid\"", StringComparison.Ordinal) > moved.IndexOf("class=\"cta-band\"", StringComparison.Ordinal));
        }));
    }

    [Fact]
    public async Task A_section_that_cannot_be_hidden_renders_even_if_stored_hidden()
    {
        await WithSectionsRestoredAsync(PageKeys.Contact, async () =>
        {
            await SetSectionAsync(PageKeys.Contact, "contact", visible: false);
            var html = await PageAsync("/contact");
            Assert.Contains("class=\"contact-section page-form\"", html);
        });
    }

    // ── Admin screen (T5.5 400s, T5.10) ──

    [Fact]
    public async Task Admin_toggle_move_and_limit_work_and_refuse_what_the_registry_forbids()
    {
        var client = await f.SignedInClientAsync();
        var screen = await client.GetAsync("/Admin/PageSections");
        Assert.Equal(HttpStatusCode.OK, screen.StatusCode);
        var token = ExtractToken(await screen.Content.ReadAsStringAsync());

        await WithSectionsRestoredAsync(PageKeys.Home, () => WithContentRestoredAsync("home_events_limit", async () =>
        {
            var r = await client.PostAsync("/Admin/PageSections/Toggle/home", Form(token, ("section", "faq")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.DoesNotContain("class=\"faq-section\"", await PageAsync("/")); // next GET sees it (T5.12)

            r = await client.PostAsync("/Admin/PageSections/Move/home", Form(token, ("section", "contact"), ("direction", "-1")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var ids = SectionIds(await PageAsync("/"));
            Assert.True(ids.IndexOf("contact") < ids.IndexOf("register"));

            // Edge: the first movable section up = no change, no error.
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/PageSections/Move/home", Form(token, ("section", "about"), ("direction", "-1")))).StatusCode);
            Assert.Equal("about", SectionIds(await PageAsync("/"))[1]);

            r = await client.PostAsync("/Admin/PageSections/SetLimit", Form(token, ("key", "home_events_limit"), ("value", "2")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal("2", await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "home_events_limit").Select(s => s.ContentAr).SingleAsync()));
            foreach (var bad in new[] { "0", "13", "-1", "x", "", "٣" })
            {
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/PageSections/SetLimit", Form(token, ("key", "home_events_limit"), ("value", bad)))).StatusCode);
                Assert.Equal("2", await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "home_events_limit").Select(s => s.ContentAr).SingleAsync()));
            }
        }));

        // Registry refusals.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/PageSections/Toggle/contact", Form(token, ("section", "contact")))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/PageSections/Toggle/forum.events", Form(token, ("section", "list")))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/PageSections/Move/home", Form(token, ("section", "hero_stats"), ("direction", "1")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/PageSections/Toggle/home", Form(token, ("section", "hero")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/PageSections/Toggle/nope", Form(token, ("section", "faq")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/PageSections/Move/home", Form(token, ("section", "faq"), ("direction", "2")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/Admin/PageSections/SetLimit", Form(token, ("key", "list_page_size"), ("value", "3")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/PageSections/Index/privacy")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/PageSections/Index/network.startups")).StatusCode);
    }

    [Fact]
    public async Task Admin_sections_actions_need_admin_and_a_token()
    {
        var anon = f.NewClient();
        var get = await anon.GetAsync("/Admin/PageSections");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Contains("/Account/Login", get.Headers.Location!.OriginalString);
        var token = await anon.GetTokenAsync("/Account/Login");
        foreach (var (url, form) in new[]
        {
            ("/Admin/PageSections/Toggle/home", Form(token, ("section", "faq"))),
            ("/Admin/PageSections/Move/home", Form(token, ("section", "faq"), ("direction", "1"))),
            ("/Admin/PageSections/SetLimit", Form(token, ("key", "home_events_limit"), ("value", "2"))),
        })
        {
            var r = await anon.PostAsync(url, form);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);
        }
        var admin = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/Admin/PageSections/Toggle/home", Form(("section", "faq")))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/Admin/PageSections/SetLimit", Form(("key", "home_events_limit"), ("value", "2")))).StatusCode);
        Assert.True(await f.DbAsync(db => db.PageSections.Where(r => r.PageKey == "home" && r.SectionKey == "faq").Select(r => r.IsVisible).SingleAsync()));
    }

    // ── T5.4 Unpublished pages ──

    [Fact]
    public async Task An_unpublished_page_and_its_details_are_the_branded_404_and_leave_the_404_links()
    {
        var slug = await f.DbAsync(db => db.Events.Where(e => e.IsActive).Select(e => e.Slug).FirstAsync());
        await WithPageRestoredAsync(PageKeys.ForumEvents, async () =>
        {
            Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync("/forum/events/" + slug)).StatusCode); // positive control
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.ForumEvents)).IsPublished = false; return await db.SaveChangesAsync(); });
            foreach (var url in new[] { "/forum/events", "/en/forum/events", "/forum/events?page=1", "/forum/events/" + slug, "/en/forum/events/" + slug })
            {
                var r = await f.NewClient().GetAsync(url);
                Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
                Assert.Contains("noindex", string.Join(",", r.Headers.GetValues("X-Robots-Tag")));
                Assert.Contains("not-found-code", await r.Content.ReadAsStringAsync());
            }
            // No link to it from the pages this branch renders: home "view all", the forum's events block.
            var home = await PageAsync("/");
            Assert.DoesNotContain("<a class=\"btn btn-outline\" href=\"/forum/events\">", home);
        });
        Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync("/forum/events")).StatusCode);
    }

    [Fact]
    public async Task An_unpublished_hub_takes_its_sub_pages_with_it()
    {
        await WithPageRestoredAsync(PageKeys.Tv, async () =>
        {
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.Tv)).IsPublished = false; return await db.SaveChangesAsync(); });
            foreach (var url in new[] { "/tv", "/en/tv", "/tv/episodes", "/tv/sponsorship", "/en/tv/guests" })
                Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync("/forum")).StatusCode);
        });
    }

    [Fact]
    public async Task NotFound_links_follow_notfound_pages_skipping_unknown_and_unpublished_keys()
    {
        static List<string> Links(string html) =>
            Regex.Matches(html, "<ul class=\"not-found-links\">(.*?)</ul>", RegexOptions.Singleline).Single().Groups[1].Value is var ul
                ? Regex.Matches(ul, "href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList() : [];

        var r = await f.NewClient().GetAsync("/no-such-page-c5d2");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal(["/", "/ecosystem", "/contact"], Links(await r.Content.ReadAsStringAsync()));

        await WithContentRestoredAsync("notfound_pages", () => WithPageRestoredAsync(PageKeys.About, async () =>
        {
            await SetContentAsync("notfound_pages", "news, nope ,about,news,contact");
            var en = await f.NewClient().GetAsync("/en/no-such-page-c5d2");
            Assert.Equal(HttpStatusCode.NotFound, en.StatusCode);
            Assert.Equal(["/en/news", "/en/about", "/en/contact"], Links(await en.Content.ReadAsStringAsync()));

            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.About)).IsPublished = false; return await db.SaveChangesAsync(); });
            Assert.Equal(["/news", "/contact"], Links(await (await f.NewClient().GetAsync("/no-such-page-c5d2")).Content.ReadAsStringAsync()));

            await SetContentAsync("notfound_pages", "nope, about");
            Assert.Equal(["/", "/ecosystem", "/contact"], Links(await (await f.NewClient().GetAsync("/no-such-page-c5d2")).Content.ReadAsStringAsync()));
        }));
    }

    [Fact]
    public async Task Empty_state_button_follows_EmptyActionPage_and_disappears_for_an_unpublished_target()
    {
        var active = await f.DbAsync(db => db.TvEpisodes.Where(e => e.IsActive).Select(e => e.Id).ToListAsync());
        try
        {
            await f.DbAsync(async db => { foreach (var e in db.TvEpisodes) e.IsActive = false; return await db.SaveChangesAsync(); });
            var html = await PageAsync("/tv/episodes");
            Assert.Contains("href=\"/tv/sponsorship\"", EmptySection(html)); // code default = the TV action

            await WithPageRestoredAsync(PageKeys.TvEpisodes, () => WithPageRestoredAsync(PageKeys.About, async () =>
            {
                await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.TvEpisodes)).EmptyActionPage = PageKeys.About; return await db.SaveChangesAsync(); });
                Assert.Contains("href=\"/en/about\"", EmptySection(await PageAsync("/en/tv/episodes")));
                await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.About)).IsPublished = false; return await db.SaveChangesAsync(); });
                Assert.DoesNotContain("href=\"/about\"", EmptySection(await PageAsync("/tv/episodes"))); // (menus: Dev 3)
            }));
        }
        finally
        {
            await f.DbAsync(async db => { foreach (var e in db.TvEpisodes.Where(e => active.Contains(e.Id))) e.IsActive = true; return await db.SaveChangesAsync(); });
        }
    }

    [Fact]
    public async Task A_hub_keeps_its_coming_soon_button_when_it_has_an_intro()
    {
        var saved = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.Tv));
        try
        {
            await f.DbAsync(async db => { var p = await db.Pages.SingleAsync(x => x.Key == PageKeys.Tv); p.IntroAr = "مقدمة C5D2"; p.IntroEn = "C5D2 intro"; return await db.SaveChangesAsync(); });
            var ar = await PageAsync("/tv");
            Assert.Contains("مقدمة C5D2", ar);
            Assert.Contains("href=\"/tv/sponsorship\"", EmptySection(ar));
            var en = await PageAsync("/en/tv");
            Assert.Contains("C5D2 intro", en);
            Assert.Contains("href=\"/en/tv/sponsorship\"", EmptySection(en));
            // A non-hub page with text and no block still drops its empty block (privacy has a body).
            Assert.DoesNotContain("empty-state", await PageAsync("/privacy"));
        }
        finally
        {
            await f.DbAsync(async db => { var p = await db.Pages.SingleAsync(x => x.Key == PageKeys.Tv); p.IntroAr = saved.IntroAr; p.IntroEn = saved.IntroEn; return await db.SaveChangesAsync(); });
        }
    }

    [Fact]
    public async Task Privacy_page_renders_its_body_and_is_404_when_unpublished()
    {
        var html = await PageAsync("/en/privacy");
        Assert.Contains("class=\"page-section page-body\"", html);
        await WithPageRestoredAsync(PageKeys.Privacy, async () =>
        {
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.Privacy)).IsPublished = false; return await db.SaveChangesAsync(); });
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/privacy")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/en/privacy")).StatusCode);
        });
    }

    private static string EmptySection(string html)
    {
        var start = html.IndexOf("empty-", StringComparison.Ordinal);
        Assert.True(start >= 0, "the page shows its empty state");
        return html[start..Math.Min(html.Length, start + 1500)];
    }

    [Fact]
    public async Task Event_and_tv_actions_follow_their_link_keys()
    {
        await WithContentRestoredAsync("events_action_link", () => WithContentRestoredAsync("events_action_label", async () =>
        {
            await SetContentAsync("events_action_link", PageKeys.Contact);
            await SetContentAsync("events_action_label", "سجّل اهتمامك");
            var html = await PageAsync("/");
            Assert.Contains("href=\"/contact\"", EventsSection(html));
            Assert.Contains("سجّل اهتمامك", EventsSection(html));
            // English: the Arabic label is never used; the target's English title is.
            var en = await PageAsync("/en");
            Assert.DoesNotContain("سجّل اهتمامك", EventsSection(en));
            Assert.Contains("href=\"/en/contact\"", EventsSection(en));
            // An unsafe stored value falls back to the default page.
            await SetContentAsync("events_action_link", "javascript:alert(1)");
            Assert.Contains("href=\"/forum/registration\"", EventsSection(await PageAsync("/")));
        }));
    }

    [Fact] // the public-view rule of BilingualUnitTests, for the home section partials it does not scan
    public void Home_section_partials_contain_no_arabic_letters()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InnovestHolding.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var files = Directory.GetFiles(Path.Combine(dir!.FullName, "Views", "Home", "Sections"), "*.cshtml");
        Assert.True(files.Length >= 10, $"scanned {files.Length} files");
        Assert.Empty(files.SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
            .Where(x => Regex.IsMatch(x.l, "[؀-ۿ]")).Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}"));
    }

    [Fact]
    public void Registry_limits_and_layout_rules()
    {
        var limit = PageSectionRegistry.FindLimit("home_events_limit")!;
        Assert.Equal(12, PageSectionsTestAccess.ParseMax(limit));
        Assert.Null(PageSectionRegistry.ParseLimit(limit, "13"));
        Assert.Null(PageSectionRegistry.ParseLimit(limit, "0"));
        Assert.Equal(12, PageSectionRegistry.ParseLimit(limit, "12"));
        Assert.Null(PageSectionRegistry.FindLimit("list_page_size"));
        // Every page with blocks is a page of the route table; the home hero is not a section.
        Assert.All(PageSectionRegistry.Pages.Keys, k => Assert.True(SiteRoutes.IsKey(k)));
        Assert.Null(PageSectionRegistry.Find(PageKeys.Home, "hero"));
    }
}

internal static class PageSectionsTestAccess
{
    public static int ParseMax(PageSectionRegistry.SectionLimit l) => l.Max;
}
