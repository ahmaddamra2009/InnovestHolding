using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 (Dev 3, T5.8): the admin-chosen buttons (a page or an https address), the 404 suggestions, the counts and the
/// section icons — resolved by SiteViews from the content keys AddAdminControl inserts, saved through the Content editor.
/// </summary>
[Collection("App")]
public class AdminLinksTests(TestAppFactory f)
{
    /// <summary>The registry keys the migration AddAdminControl inserts (absent from a database migrated to an earlier
    /// point — the older migration tests exclude them).</summary>
    public static readonly string[] AddedByAdminControl =
    [
        "hero_cta_main_label", "hero_cta_main_link", "hero_cta_secondary_link", "about_more_label", "about_more_link",
        "cta_band_button1_label", "cta_band_button1_link", "cta_band_button2_label", "cta_band_button2_link",
        "events_action_label", "events_action_link", "tv_action_label", "tv_action_link", "notfound_pages",
        "hero_stats_limit", "home_events_limit", "forum_events_limit", "vision_icon", "mission_icon", "values_icon",
        "ecosystem_icon", "platforms_icon", "events_icon", "news_icon", "partners_icon", "faq_icon", "contact_icon", "register_icon",
    ];

    [Fact] // every C5 key of the registry is one AddAdminControl inserts — and the full migration has them all
    public async Task Every_new_registry_key_is_delivered_by_the_migration()
    {
        var keys = await f.DbAsync(db => db.SiteContents.Select(c => c.SectionKey).ToListAsync());
        Assert.Empty(AddedByAdminControl.Except(keys));
        Assert.All(AddedByAdminControl, k => Assert.NotNull(ContentKeys.Find(k)));
    }

    private async Task<SiteSnapshot> SnapshotAsync()
    {
        var cache = f.Services.GetRequiredService<ISiteContentCache>();
        cache.Invalidate();
        return await cache.GetAsync();
    }

    /// <summary>The migrated snapshot with some content keys replaced (value = Arabic/neutral, En optional).</summary>
    private static SiteSnapshot With(SiteSnapshot s, params (string Key, string Ar, string? En)[] values)
    {
        var content = new Dictionary<string, TextPair>(s.Content);
        foreach (var (key, ar, en) in values) content[key] = new TextPair(ar, en);
        return s with { Content = content };
    }

    private static SiteSnapshot Unpublished(SiteSnapshot s, string key)
    {
        var pages = new Dictionary<string, PageRow>(s.Pages, StringComparer.Ordinal) { [key] = s.Pages[key] with { IsPublished = false } };
        return s with { Pages = pages };
    }

    private static string Url(string key, bool en) => CultureUrls.Page(key, en);

    [Fact] // NC-0: the migrated values reproduce every C4 button, count and icon, in both languages
    public async Task Defaults_reproduce_the_C4_targets_labels_counts_and_icons()
    {
        var s = await SnapshotAsync();
        foreach (var en in new[] { false, true })
        {
            var v = new SiteViews(s, en);
            LinkVm Page(string key) => new(Url(key, en), v.Title(key));

            Assert.Equal([Page(PageKeys.NetworkJoin), Page(PageKeys.ForumRegistration)], v.CtaBand().Buttons);
            Assert.Equal(Page(PageKeys.About), v.AboutMore());
            Assert.Equal(Page(PageKeys.ForumRegistration), v.EventsAction());
            Assert.Equal(Page(PageKeys.TvSponsorship), v.TvAction());
            Assert.Equal(new LinkVm(Url(PageKeys.Ecosystem, en), v.Content.Text("hero_cta_primary")), v.HeroSecondaryCta());
            var header = new LinkVm(Url(PageKeys.NetworkJoin, en), v.Content.Text("header_cta_label"));
            Assert.Equal(header, v.HeaderCta());
            Assert.Equal(header, v.HeroMainCta());
            Assert.Equal([Page(PageKeys.Home), Page(PageKeys.Ecosystem), Page(PageKeys.Contact)], v.NotFoundLinks());
            Assert.All(v.CtaBand().Buttons, b => Assert.Null(b.Rel)); // internal links: no rel

            var shell = v.Shell(PageKeys.About, null);
            Assert.Equal(header, shell.HeaderCta);
            Assert.Equal(shell.HeaderCta, shell.Cta);
            Assert.Equal(v.Content.Text("site_name"), shell.Brand.LogoAlt); // no uploaded logo → the site name
        }

        var ar = new SiteViews(s, false);
        Assert.Equal((4, 3, 3), (ar.Count(ContentKeys.HeroStatsLimit), ar.Count(ContentKeys.HomeEventsLimit), ar.Count(ContentKeys.ForumEventsLimit)));
        foreach (var (key, icon) in new[] { ("vision_icon", "eye"), ("mission_icon", "target"), ("values_icon", "handshake"),
                     ("ecosystem_icon", "layers"), ("events_icon", "calendar"), ("news_icon", "book"), ("partners_icon", "handshake"),
                     ("contact_icon", "mic"), ("register_icon", "user_plus") })
            Assert.Equal(Icons.Svg(icon), ar.Icon(key));
        // The glyphs drawn in code before C5 are the same markup (header badges of services / FAQ, quick registration).
        Assert.Equal("<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\"/>", ar.Icon("platforms_icon"));
        Assert.Equal("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/>", ar.Icon("faq_icon"));
        Assert.Equal(SiteViews.BadgeGrid, ar.Header("platforms", "x").BadgeIconSvg);
        Assert.Equal(SiteViews.BadgeQuestion, ar.Header("faq", "x").BadgeIconSvg);
        Assert.Equal("x", ar.Header("nokey", "x").BadgeIconSvg); // a prefix without an icon key keeps the caller's icon
    }

    [Theory] // a stored value that is not a page key nor a safe https address falls back to the default page
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("data:text/html,x")]
    [InlineData("http://example.com")]
    [InlineData("https://exa mple.com")]
    [InlineData("no.such.page")]
    public async Task An_unsafe_or_unknown_stored_target_renders_the_default_page(string bad)
    {
        var s = With(await SnapshotAsync(), ("cta_band_button1_link", bad, null), ("cta_band_button1_label", "نص", null));
        var b = new SiteViews(s, false).CtaBand().Buttons[0];
        Assert.Equal(Url(PageKeys.NetworkJoin, false), b.Href);
        Assert.False(b.External);
    }

    [Fact] // a page key renders the URL of the page language; a label in Arabic only gives the English page title on /en
    public async Task Page_targets_follow_the_language_and_the_label_falls_back_to_the_page_title()
    {
        var s = With(await SnapshotAsync(), ("about_more_link", PageKeys.Contact, null), ("about_more_label", "راسلنا", null));
        var ar = new SiteViews(s, false).AboutMore()!;
        var en = new SiteViews(s, true).AboutMore()!;
        Assert.Equal(("/contact", "راسلنا"), (ar.Href, ar.Title.Value));
        Assert.Equal("/en/contact", en.Href);
        Assert.Equal(new SiteViews(s, true).Title(PageKeys.Contact), en.Title); // never the Arabic override on /en
        Assert.NotEqual("راسلنا", en.Title.Value);

        var both = With(s, ("about_more_label", "راسلنا", "Write to us"));
        Assert.Equal("Write to us", new SiteViews(both, true).AboutMore()!.Title.Value);
    }

    [Fact] // an external https target: rel noopener noreferrer, needs its text (Arabic fallback on /en), none → not rendered
    public async Task An_external_target_gets_rel_and_needs_a_label()
    {
        const string url = "https://partner.example.org/apply";
        var s = With(await SnapshotAsync(), ("events_action_link", url, null), ("events_action_label", "سجّل لدى الشريك", null));
        var ar = new SiteViews(s, false).EventsAction()!;
        Assert.Equal((url, true, "noopener noreferrer"), (ar.Href, ar.External, ar.Rel));
        var en = new SiteViews(s, true).EventsAction()!;
        Assert.Equal(url, en.Href); // an address is the same in both languages
        Assert.True(en.Title.IsFallback);
        Assert.Equal("سجّل لدى الشريك", en.Title.Value);

        var noLabel = With(s, ("events_action_label", "", null));
        Assert.Null(new SiteViews(noLabel, false).EventsAction()); // no empty anchor, no raw URL as text
    }

    [Fact] // a button pointing to an unpublished page is not rendered; menus, cards, 404 links skip the page too
    public async Task An_unpublished_target_is_not_rendered()
    {
        var s = With(Unpublished(await SnapshotAsync(), PageKeys.ForumRegistration),
            (ContentKeys.NotFoundPages, "home,forum.registration,about", null));
        var v = new SiteViews(s, false);
        Assert.Equal([Url(PageKeys.NetworkJoin, false)], v.CtaBand().Buttons.Select(b => b.Href)); // second button gone
        Assert.Null(v.EventsAction());
        Assert.Null(v.PageLink(PageKeys.ForumRegistration));
        Assert.False(v.InMenu(PageKeys.ForumRegistration));
        Assert.DoesNotContain(v.Cards(PageKeys.Forum, false), c => c.Href == Url(PageKeys.ForumRegistration, false));
        Assert.Equal([Url(PageKeys.Home, false), Url(PageKeys.About, false)], v.NotFoundLinks().Select(l => l.Href));

        // An unpublished hub hides its sub-pages (the chain rule of SiteSnapshot.IsPublished).
        var hub = new SiteViews(Unpublished(await SnapshotAsync(), PageKeys.Forum), false);
        Assert.Null(hub.PageLink(PageKeys.ForumRegistration));

        // The header button: hidden, while the legacy Cta stays non-null for the home hero until it reads HeroMainCta().
        var header = new SiteViews(With(Unpublished(await SnapshotAsync(), PageKeys.NetworkJoin)), false);
        var shell = header.Shell(PageKeys.About, null);
        Assert.Null(shell.HeaderCta);
        Assert.Null(header.HeroMainCta());
        Assert.NotNull(shell.Cta);
    }

    [Fact]
    public async Task Hero_main_button_is_the_header_button_until_it_has_its_own_target()
    {
        var s = await SnapshotAsync();
        var header = new SiteViews(s, false).HeaderCta()!;
        var labelOnly = new SiteViews(With(s, ("hero_cta_main_label", "ابدأ", null)), false).HeroMainCta()!;
        Assert.Equal((header.Href, "ابدأ"), (labelOnly.Href, labelOnly.Title.Value));
        var labelOnlyEn = new SiteViews(With(s, ("hero_cta_main_label", "ابدأ", null)), true).HeroMainCta()!;
        Assert.Equal(new SiteViews(s, true).HeaderCta(), labelOnlyEn); // Arabic-only text: /en keeps the header button
        var own = new SiteViews(With(s, ("hero_cta_main_link", PageKeys.About, null)), true).HeroMainCta()!;
        Assert.Equal(("/en/about", new SiteViews(s, true).Title(PageKeys.About)), (own.Href, own.Title));
        Assert.Equal(header, new SiteViews(s, false).Shell(PageKeys.Home, null).HeaderCta); // the header itself unchanged
    }

    [Fact]
    public async Task Cta_band_second_button_emptied_leaves_one_button_and_a_missing_row_means_the_default()
    {
        var s = await SnapshotAsync();
        Assert.Single(new SiteViews(With(s, ("cta_band_button2_link", "", null)), false).CtaBand().Buttons);
        var content = new Dictionary<string, TextPair>(s.Content);
        content.Remove("cta_band_button2_link");
        Assert.Equal(2, new SiteViews(s with { Content = content }, false).CtaBand().Buttons.Count);
        Assert.Equal(Url(PageKeys.About, false), new SiteViews(With(s, ("about_more_link", "", null)), false).AboutMore()?.Href); // empty = default
    }

    [Fact] // counts: 1–12 for the new keys, anything else → the default; icons: a known key, else the default
    public async Task Counts_and_icons_fall_back_to_the_default_when_invalid()
    {
        var s = await SnapshotAsync();
        int Hero(string value) => new SiteViews(With(s, (ContentKeys.HeroStatsLimit, value, null)), false).Count(ContentKeys.HeroStatsLimit);
        Assert.Equal(12, Hero("12"));
        Assert.Equal(1, Hero("1"));
        foreach (var bad in new[] { "13", "0", "-1", "abc", "" }) Assert.Equal(4, Hero(bad));
        Assert.Equal(20, ContentKeys.ParseCount("20", 6)); // home_news_limit keeps its 1–50 range (NC-0)

        string Icon(string value) => new SiteViews(With(s, ("vision_icon", value, null)), false).Icon("vision_icon");
        Assert.Equal(Icons.Svg("rocket"), Icon("rocket"));
        Assert.Equal(Icons.Svg("eye"), Icon("<script>"));
        Assert.Equal(Icons.Svg("eye"), Icon(""));
    }

    [Fact] // the logo alt: the uploaded logo's alt in the page language, else the site name
    public async Task Logo_alt_is_the_uploaded_alt_in_the_page_language()
    {
        var v = new SiteViews(await SnapshotAsync(), true);
        var logo = new MediaRef("k", "png", 10, 10, "شعار", "Logo");
        Assert.Equal("Logo", v.LogoAlt(logo).Value);
        Assert.Equal(v.Content.Text("site_name"), v.LogoAlt(logo with { AltEn = null })); // no English alt → site name, not Arabic
        Assert.Equal("شعار", new SiteViews(await SnapshotAsync(), false).LogoAlt(logo).Value);
    }

    [Fact] // social: custom rows only with a label and a known icon; known networks unchanged
    public async Task Custom_social_rows_need_a_label_and_a_known_icon()
    {
        var s = await SnapshotAsync() with
        {
            Socials = [new("linkedin", "https://l.example"), new("custom", "https://t.example", "Telegram", "globe"),
                new("custom", "https://x.example", "", "globe"), new("custom", "https://y.example", "Y", "nope"), new("unknown", "https://z.example")],
        };
        Assert.Equal([new SocialVm("linkedin", "https://l.example"), new SocialVm("custom", "https://t.example", "Telegram", "globe")],
            new SiteViews(s, false).Socials());
    }

    [Fact] // the privacy link exists while the policy is published and has an Arabic body
    public async Task Privacy_link_follows_the_body_and_publication()
    {
        var s = await SnapshotAsync();
        Assert.Equal(Url(PageKeys.Privacy, true), new SiteViews(s, true).PrivacyLink()?.Href);
        var empty = s with { Pages = new Dictionary<string, PageRow>(s.Pages) { [PageKeys.Privacy] = s.Pages[PageKeys.Privacy] with { BodyAr = " " } } };
        Assert.Null(new SiteViews(empty, false).PrivacyLink());
        Assert.Null(new SiteViews(Unpublished(s, PageKeys.Privacy), false).PrivacyLink());
    }

    // ── The Content editor (HTTP) ──

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string token, params (string, string)[] fields) =>
        await client.PostAsync("/Admin/Content/Edit", Form(token, fields));

    private Task<string?> StoredAsync(string key) =>
        f.DbAsync(db => db.SiteContents.Where(c => c.SectionKey == key).Select(c => c.ContentAr).SingleOrDefaultAsync());

    private async Task RestoreAsync(params (string Key, string? Value)[] rows) =>
        await f.DbAsync(async db =>
        {
            foreach (var (key, value) in rows)
            {
                var row = await db.SiteContents.SingleAsync(c => c.SectionKey == key);
                row.ContentAr = value ?? "";
                row.ContentEn = null;
            }
            return await db.SaveChangesAsync();
        });

    [Fact] // T5.8: the editor refuses javascript:, //host, /\host, data:, http: and an external link without its text
    public async Task Editor_refuses_unsafe_targets_and_external_links_without_text()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        var before = await StoredAsync("cta_band_button1_link");
        foreach (var bad in new[] { "javascript:alert(1)", "//evil.example", "/\\evil.example", "data:text/html,x", "http://example.com", "about" + "x" })
        {
            var r = await PostAsync(client, token, ("content[cta_band_button1_link].Ar", ContentKeys.ExternalLinkChoice),
                ("external[cta_band_button1_link]", bad), ("content[cta_band_button1_label].Ar", "نص"));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with the error
            Assert.Contains("https://", await r.Content.ReadAsStringAsync());
            // The same value typed straight into the field (no picker) is refused too.
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, ("content[cta_band_button1_link].Ar", bad))).StatusCode);
        }
        // An https address with no Arabic text (posted empty, stored empty): refused.
        var noText = await PostAsync(client, token, ("content[cta_band_button1_link].Ar", ContentKeys.ExternalLinkChoice),
            ("external[cta_band_button1_link]", "https://partner.example.org"), ("content[cta_band_button1_label].Ar", ""));
        Assert.Equal(HttpStatusCode.OK, noText.StatusCode);
        Assert.Equal(before, await StoredAsync("cta_band_button1_link")); // nothing saved
    }

    [Fact] // T5.8 end to end: an external CTA band button and a page target, saved, then on /about and /en/about
    public async Task Editor_saves_a_link_and_the_public_pages_render_it()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, token,
                ("content[cta_band_button1_link].Ar", ContentKeys.ExternalLinkChoice), ("external[cta_band_button1_link]", "https://partner.example.org/x"),
                ("content[cta_band_button1_label].Ar", "زر الشريك"), ("content[cta_band_button1_label].En", "Partner button"),
                ("content[cta_band_button2_link].Ar", PageKeys.Contact))).StatusCode);
            Assert.Equal("https://partner.example.org/x", await StoredAsync("cta_band_button1_link"));

            foreach (var (path, text, second) in new[] { ("/about", "زر الشريك", "/contact"), ("/en/about", "Partner button", "/en/contact") })
            {
                var band = Regex.Match(await f.NewClient().GetStringAsync(path), "<section class=\"cta-band\">.*?</section>", RegexOptions.Singleline).Value;
                Assert.Contains($"<a href=\"https://partner.example.org/x\" class=\"btn btn-accent\" rel=\"noopener noreferrer\">{text}</a>", band);
                Assert.Matches($"<a href=\"{second}\" class=\"btn btn-outline-light\">", band); // internal: no rel
            }

            // The editor shows the external choice selected with its address.
            var edit = await client.GetStringAsync("/Admin/Content/Edit");
            Assert.Contains("<option value=\"external\" selected=\"selected\">", Regex.Match(edit, "<select id=\"content-cta-band-button1-link\".*?</select>", RegexOptions.Singleline).Value);
            Assert.Contains("value=\"https://partner.example.org/x\"", edit);
        }
        finally
        {
            await RestoreAsync(("cta_band_button1_link", "network.join"), ("cta_band_button1_label", ""), ("cta_band_button2_link", "forum.registration"));
        }
        Assert.Contains($"href=\"{Url(PageKeys.NetworkJoin, false)}\" class=\"btn btn-accent\">", await f.NewClient().GetStringAsync("/about"));
    }

    [Fact] // counts: 1–12 (not 13); icons: whitelist only; 404 pages: checkboxes in route order, max 6, then the 404 page
    public async Task Editor_validates_counts_icons_and_the_404_list()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        try
        {
            foreach (var bad in new[] { "13", "0", "x" })
                Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, ($"content[{ContentKeys.HomeEventsLimit}].Ar", bad))).StatusCode);
            Assert.Equal("3", await StoredAsync(ContentKeys.HomeEventsLimit));
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, token, ($"content[{ContentKeys.HomeEventsLimit}].Ar", "12"))).StatusCode);
            Assert.Equal("12", await StoredAsync(ContentKeys.HomeEventsLimit));

            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, ("content[vision_icon].Ar", "<svg onload=x>"))).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, token, ("content[vision_icon].Ar", "rocket"))).StatusCode);
            Assert.Equal("rocket", await StoredAsync("vision_icon"));
            // The editor renders the shared icon grid (radios) with the stored icon checked; empty = the default.
            var editor = await client.GetStringAsync("/Admin/Content/Edit");
            Assert.Contains("<input type=\"radio\" class=\"sr-only\" name=\"content[vision_icon].Ar\" value=\"rocket\" checked=\"checked\">", editor);
            Assert.Contains("<input type=\"radio\" class=\"sr-only\" name=\"content[mission_icon].Ar\" value=\"target\" checked=\"checked\">", editor);
            Assert.DoesNotContain("<select id=\"content-vision-icon\"", editor);

            // Seven pages: refused; an unknown key: refused; none: refused.
            var seven = SiteRoutes.All.Take(7).Select(p => ("pagelist[notfound_pages]", p.Key)).Append(("pagelist-posted[notfound_pages]", "1")).ToArray();
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, seven)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, ("pagelist-posted[notfound_pages]", "1"), ("pagelist[notfound_pages]", "nope"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, token, ("pagelist-posted[notfound_pages]", "1"))).StatusCode);
            Assert.Equal("home,ecosystem,contact", await StoredAsync(ContentKeys.NotFoundPages));
            // Posted out of order: stored in the route table's order.
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, token, ("pagelist-posted[notfound_pages]", "1"),
                ("pagelist[notfound_pages]", PageKeys.Contact), ("pagelist[notfound_pages]", PageKeys.About))).StatusCode);
            Assert.Equal("about,contact", await StoredAsync(ContentKeys.NotFoundPages));

            // (The 404 page reads SiteViews.NotFoundLinks() once Dev 2 wires it; resolution is covered by the unit tests above.)
        }
        finally
        {
            await RestoreAsync((ContentKeys.HomeEventsLimit, "3"), ("vision_icon", "eye"), (ContentKeys.NotFoundPages, "home,ecosystem,contact"));
        }
    }

    [Fact] // Standing Check #5: the editor's POST refuses anonymous (→ login) and a missing antiforgery token (400)
    public async Task Editor_post_requires_admin_and_token()
    {
        var anon = f.NewClient();
        var r = await anon.PostAsync("/Admin/Content/Edit", Form(("content[vision_icon].Ar", "rocket")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Account/Login", r.Headers.Location!.ToString());
        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Content/Edit", Form(("content[vision_icon].Ar", "rocket")))).StatusCode);
        Assert.Equal("eye", await StoredAsync("vision_icon"));
    }
}
