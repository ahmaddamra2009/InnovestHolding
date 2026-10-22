using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;

namespace InnovestHolding.Tests;

/// <summary>The language comes from the URL only.</summary>
[Collection("App")]
public class CultureRoutingTests(TestAppFactory f)
{
    private static async Task<string> Html(HttpResponseMessage r) => await r.Content.ReadAsStringAsync();

    [Fact]
    public async Task Root_is_arabic_rtl_and_en_is_english_ltr()
    {
        var ar = await f.NewClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, ar.StatusCode);
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\">", await Html(ar));

        var en = await f.NewClient().GetAsync("/en");
        Assert.Equal(HttpStatusCode.OK, en.StatusCode);
        var html = await Html(en);
        Assert.Contains("<html lang=\"en\" dir=\"ltr\">", html);
        Assert.Contains(">Home</a>", html);                 // nav item English
        Assert.Contains("href=\"/\" hreflang=\"ar\"", html);  // the switch is a real link back to Arabic
    }

    [Theory]
    [InlineData("/ar", "/")]
    [InlineData("/ar/", "/")]
    [InlineData("/en/", "/en")]
    [InlineData("/EN", "/en")]
    [InlineData("/Home", "/")]
    [InlineData("/Home/Index", "/")]
    [InlineData("/en/Index", "/en")]
    [InlineData("/ar?utm_source=x", "/?utm_source=x")]
    [InlineData("/en/?a=1", "/en?a=1")]
    public async Task Aliases_are_301_to_the_canonical_url(string path, string location)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, r.StatusCode);
        Assert.Equal(location, r.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/fr")]
    [InlineData("/ar/x")]
    [InlineData("/en/Account/Login")]
    [InlineData("/en/Admin/Dashboard")]
    [InlineData("/en/Privacy")]
    public async Task Unknown_language_paths_are_404(string path) =>
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(path)).StatusCode);

    [Fact]
    public async Task A_post_to_an_alias_is_not_redirected()
    {
        var r = await f.NewClient().PostAsync("/ar", Form());
        Assert.NotEqual(HttpStatusCode.MovedPermanently, r.StatusCode);
    }

    [Fact]
    public async Task Accept_language_and_culture_cookie_cannot_change_the_page()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c%3Den%7Cuic%3Den");
        var html = await Html(await f.NewClient().SendAsync(request));
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\">", html);

        using var request2 = new HttpRequestMessage(HttpMethod.Get, "/?culture=en&ui-culture=en");
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\">", await Html(await f.NewClient().SendAsync(request2)));
    }

    [Fact] // admin runs under Arabic with the Gregorian calendar and ASCII digits
    public async Task Admin_dates_are_gregorian_utc_markup()
    {
        var client = await f.SignedInClientAsync();
        var email = Unique("time") + "@example.com";
        await f.DbAsync(async db =>
        {
            db.Registrations.Add(new Registration { FullName = "QA", Email = email, Interest = "x", CreatedAt = new DateTime(2099, 3, 4, 5, 6, 7) });
            return await db.SaveChangesAsync();
        });
        try
        {
            var html = await Html(await client.GetAsync("/Admin/Registrations"));
            Assert.Contains("<time class=\"utc-time\" datetime=\"2099-03-04T05:06:07Z\">2099/03/04 05:06 UTC</time>", html);
            Assert.Contains("local-time.js", html);
        }
        finally
        {
            await f.DbAsync(db => db.Registrations.Where(r => r.Email == email).ExecuteDeleteAsync());
        }
    }
}

/// <summary>The public forms in both languages.</summary>
[Collection("App")]
public class LocalizedFormTests(TestAppFactory f)
{
    private async Task<(HttpClient, string)> ClientAsync(string page, string? ip = null)
    {
        var c = f.NewClient(ip);
        return (c, await c.GetTokenAsync(page));
    }

    private static (string, string)[] Contact(string email, string name = "QA") =>
        [("FullName", name), ("Email", email), ("Subject", "s"), ("Message", "m")];

    private static async Task<string> MessageOf(HttpResponseMessage r) =>
        (await r.JsonAsync()).GetProperty("message").GetString()!;

    [Fact]
    public async Task Validation_and_success_messages_follow_the_page_language_and_culture_is_stored()
    {
        var email = Unique("loc") + "@example.com";
        var (ar, tAr) = await ClientAsync("/");
        var (en, tEn) = await ClientAsync("/en");

        var arInvalid = await ar.PostAsync("/Home/Contact", Form(tAr, Contact(email, name: "")));
        Assert.Equal(Msg.Ar(MessageKeys.Required, Msg.Ar(F.FullName)), await MessageOf(arInvalid));
        var enInvalid = await en.PostAsync("/en/Contact", Form(tEn, Contact(email, name: "")));
        Assert.Equal(Msg.En(MessageKeys.Required, Msg.En(F.FullName)), await MessageOf(enInvalid));
        Assert.Equal("The Full name field is required", await MessageOf(enInvalid)); // literal: the resx really says it

        Assert.Equal(Msg.Ar(MessageKeys.ContactSuccess), await MessageOf(await ar.PostAsync("/Home/Contact", Form(tAr, Contact(email)))));
        Assert.Equal(Msg.En(MessageKeys.ContactSuccess), await MessageOf(await en.PostAsync("/en/Contact", Form(tEn, Contact(email)))));

        var cultures = await f.DbAsync(db => db.ContactMessages.Where(m => m.Email == email).OrderBy(m => m.Id).Select(m => m.Culture).ToListAsync());
        Assert.Equal(["ar", "en"], cultures);
    }

    [Fact]
    public async Task Framework_binding_errors_are_localized()
    {
        var (en, t) = await ClientAsync("/en");
        var r = await en.PostAsync("/en/Register", Form(t, ("FullName", "QA"), ("Email", Unique("b") + "@example.com"), ("InterestOptionId", "abc")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.InvalidValue, Msg.En(F.Interest)), await MessageOf(r));
    }

    [Fact]
    public async Task Rate_limit_message_is_english_on_the_english_endpoint()
    {
        var (en, t) = await ClientAsync("/en");
        var email = Unique("rl-en") + "@example.com";
        for (var i = 0; i < 5; i++) await en.PostAsync("/en/Contact", Form(t, Contact(email)));
        var sixth = await en.PostAsync("/en/Contact", Form(t, Contact(email)));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.TooManyRequests), await MessageOf(sixth));
    }

    [Fact] // /en does not double the budget
    public async Task Arabic_and_english_endpoints_share_one_rate_limit_bucket()
    {
        var ip = NewIp();
        var (ar, tAr) = await ClientAsync("/", ip);
        var (en, tEn) = await ClientAsync("/en", ip);
        var email = Unique("rl-shared") + "@example.com";
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/Home/Contact", Form(tAr, Contact(email)))).StatusCode);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await en.PostAsync("/en/Contact", Form(tEn, Contact(email)))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await en.PostAsync("/en/Contact", Form(tEn, Contact(email)))).StatusCode);
    }

    [Fact]
    public async Task Interest_must_be_an_active_option_and_is_stored_as_id_plus_arabic_name()
    {
        var inactiveId = await f.DbAsync(async db =>
        {
            var o = new InterestOption { NameAr = "خيار معطل " + Guid.NewGuid().ToString("N")[..6], IsActive = false, SortOrder = 99 };
            db.InterestOptions.Add(o);
            await db.SaveChangesAsync();
            return o.Id;
        });
        try
        {
            var (en, t) = await ClientAsync("/en");
            async Task<HttpResponseMessage> Post(string id, string email) =>
                await en.PostAsync("/en/Register", Form(t, ("FullName", "QA"), ("Email", email), ("InterestOptionId", id)));

            var unknown = await Post("999999", Unique("i1") + "@example.com");
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
            Assert.Equal(Msg.En(MessageKeys.InterestInvalid), await MessageOf(unknown));
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(inactiveId.ToString(), Unique("i2") + "@example.com")).StatusCode);
            var (missingClient, mt) = await ClientAsync("/en");
            Assert.Equal(HttpStatusCode.BadRequest, (await missingClient.PostAsync("/en/Register",
                Form(mt, ("FullName", "QA"), ("Email", Unique("i3") + "@example.com")))).StatusCode);

            var email = Unique("i4") + "@example.com";
            Assert.Equal(HttpStatusCode.OK, (await Post(f.InterestId, email)).StatusCode);
            var row = await f.DbAsync(db => db.Registrations.AsNoTracking().SingleAsync(r => r.Email == email));
            var option = await f.DbAsync(db => db.InterestOptions.AsNoTracking().SingleAsync(o => o.Id == int.Parse(f.InterestId)));
            Assert.Equal(option.Id, row.InterestOptionId);
            Assert.Equal(option.NameAr, row.Interest);
            Assert.Equal("en", row.Culture);
            Assert.True(row.CreatedAt > DateTime.UtcNow.AddMinutes(-5) && row.CreatedAt < DateTime.UtcNow.AddMinutes(1), "CreatedAt is UTC");
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                db.InterestOptions.Remove(await db.InterestOptions.SingleAsync(o => o.Id == inactiveId));
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task English_page_renders_options_and_forms_in_english_posting_to_en_endpoints()
    {
        var html = await (await f.NewClient().GetAsync("/en")).Content.ReadAsStringAsync();
        Assert.Contains("action=\"/en/Register\"", html);
        Assert.Contains("action=\"/en/Contact\"", html);
        Assert.Contains(">Training &amp; Education</option>", html);
        Assert.Contains("data-msg-busy=\"Registering...\"", html);
        var ar = await (await f.NewClient().GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("action=\"/Home/Register\"", ar);
        Assert.Contains("data-msg-busy=\"" + Msg.Ar(MessageKeys.Ui.FormRegistering) + "\"", ar); // Arabic is not entity-encoded
    }
}

/// <summary>Content from the database, fallback and cache invalidation.</summary>
[Collection("App")]
public class HomeContentTests(TestAppFactory f)
{
    private Task<string> PageAsync(string path) => f.NewClient().GetStringAsync(path);

    [Fact]
    public async Task Empty_english_falls_back_to_arabic_marked_rtl_and_set_english_is_used()
    {
        var ar = "عنصر-" + Guid.NewGuid().ToString("N")[..8];
        var id = await f.DbAsync(async db =>
        {
            var n = new NavItem { TitleAr = ar, Href = "#contact", SortOrder = 950, IsActive = true };
            db.NavItems.Add(n);
            await db.SaveChangesAsync(); // the interceptor drops the home cache
            return n.Id;
        });
        try
        {
            var en = await PageAsync("/en");
            Assert.Contains($"lang=\"ar\" dir=\"rtl\">{ar}</a>", en);
            Assert.DoesNotContain($"lang=\"ar\" dir=\"rtl\">{ar}", await PageAsync("/")); // no marker on the Arabic page

            await f.DbAsync(async db =>
            {
                (await db.NavItems.SingleAsync(n => n.Id == id)).TitleEn = "QA English";
                return await db.SaveChangesAsync();
            });
            en = await PageAsync("/en");
            Assert.Contains(">QA English</a>", en);
            Assert.DoesNotContain(ar, en);
        }
        finally
        {
            await f.DbAsync(async db => { db.NavItems.Remove(await db.NavItems.SingleAsync(n => n.Id == id)); return await db.SaveChangesAsync(); });
        }
    }

    [Fact]
    public async Task Admin_save_is_visible_on_the_next_request_in_both_languages()
    {
        var original = await f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == "hero_badge"));
        await PageAsync("/"); await PageAsync("/en"); // warm the cache
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        var ar = "شارة " + Guid.NewGuid().ToString("N")[..8];
        var en = "Badge " + Guid.NewGuid().ToString("N")[..8];
        try
        {
            var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ("content[hero_badge].Ar", ar), ("content[hero_badge].En", en)));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Contains(ar, await PageAsync("/"));
            Assert.Contains(en, await PageAsync("/en"));
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.SiteContents.SingleAsync(s => s.SectionKey == "hero_badge");
                row.ContentAr = original.ContentAr; row.ContentEn = original.ContentEn;
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Admin_value_with_markup_is_rendered_encoded()
    {
        var original = await f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == "news_title"));
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        try
        {
            var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ("content[news_title].Ar", "<script>alert(1)</script> {<img src=x>}")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var html = await PageAsync("/");
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.DoesNotContain("<img src=x>", html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; <span class=\"gold gold-underline\">&lt;img src=x&gt;</span>", html);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.SiteContents.SingleAsync(s => s.SectionKey == "news_title");
                row.ContentAr = original.ContentAr; row.ContentEn = original.ContentEn;
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Section_with_no_active_items_and_its_nav_link_are_not_rendered()
    {
        var active = await f.DbAsync(db => db.Faqs.Where(q => q.IsActive).Select(q => q.Id).ToListAsync());
        try
        {
            await f.DbAsync(async db => { foreach (var q in db.Faqs) q.IsActive = false; return await db.SaveChangesAsync(); });
            foreach (var path in new[] { "/", "/en" })
            {
                var html = await PageAsync(path);
                Assert.DoesNotContain("id=\"faq\"", html);
                Assert.DoesNotContain("href=\"#faq\"", html);
                Assert.Contains("id=\"news\"", html); // positive control
            }
        }
        finally
        {
            await f.DbAsync(async db => { foreach (var q in db.Faqs.Where(q => active.Contains(q.Id))) q.IsActive = true; return await db.SaveChangesAsync(); });
        }
        Assert.Contains("id=\"faq\"", await PageAsync("/"));
    }

    [Fact]
    public async Task Every_registry_key_exists_exactly_once_and_seeded_english_reaches_the_page()
    {
        var keys = await f.DbAsync(db => db.SiteContents.AsNoTracking().Select(s => s.SectionKey).ToListAsync());
        Assert.Empty(ContentKeys.All.Select(k => k.Key).Except(keys));
        Assert.Equal(keys.Count, keys.Distinct().Count());
        var en = await PageAsync("/en");
        Assert.Contains("Welcome to Innovest Holding", en);
        Assert.Contains("Frequently <span class=\"gold gold-underline\">Asked</span> Questions", en);
        Assert.DoesNotContain("hero-title-en", en); // hero_kicker has no English: hidden on /en
        Assert.Contains("hero-title-en", await PageAsync("/"));
    }
}

/// <summary>SEO head, robots, sitemap, noindex and the admin URL rules.</summary>
[Collection("App")]
public class SeoTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com"; // the production domain, seeded by the migration

    private Task<string> PageAsync(string path) => f.NewClient().GetStringAsync(path);

    private async Task SetContentAsync(string key, string value)
    {
        await f.DbAsync(async db =>
        {
            (await db.SiteContents.SingleAsync(s => s.SectionKey == key)).ContentAr = value;
            return await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Both_pages_carry_reciprocal_hreflang_and_a_self_canonical()
    {
        var ar = await PageAsync("/");
        var en = await PageAsync("/en");
        Assert.Contains($"<link rel=\"canonical\" href=\"{Base}/\">", ar);
        Assert.Contains($"<link rel=\"canonical\" href=\"{Base}/en\">", en);
        foreach (var html in new[] { ar, en })
        {
            Assert.Contains($"<link rel=\"alternate\" hreflang=\"ar\" href=\"{Base}/\">", html);
            Assert.Contains($"<link rel=\"alternate\" hreflang=\"en\" href=\"{Base}/en\">", html);
            Assert.Contains($"<link rel=\"alternate\" hreflang=\"x-default\" href=\"{Base}/\">", html);
            Assert.Single(Regex.Matches(html, "<h1[ >]"));
        }
        Assert.Contains("<meta property=\"og:locale\" content=\"en_US\">", en);
        Assert.Contains("<title>Investment &amp; Innovation Bridges | Innovest Holding</title>", en);
    }

    [Fact]
    public async Task Robots_and_sitemap_use_the_stored_base_url()
    {
        var robots = await PageAsync("/robots.txt");
        Assert.Contains("Disallow: /Admin\n", robots);
        Assert.Contains("Disallow: /admin\n", robots);
        Assert.DoesNotContain("/Account", robots); // Login stays crawlable so its noindex can be read
        Assert.Contains($"Sitemap: {Base}/sitemap.xml", robots);

        var r = await f.NewClient().GetAsync("/sitemap.xml");
        Assert.Equal("application/xml", r.Content.Headers.ContentType?.MediaType);
        var xml = XDocument.Parse(await r.Content.ReadAsStringAsync());
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xh = "http://www.w3.org/1999/xhtml";
        var urls = xml.Root!.Elements(sm + "url").ToList();
        Assert.Equal([$"{Base}/", $"{Base}/en"], urls.Select(u => u.Element(sm + "loc")!.Value));
        Assert.All(urls, u => Assert.Equal(3, u.Elements(xh + "link").Count()));
    }

    [Fact]
    public async Task Unset_base_url_outside_development_drops_absolute_tags_and_the_sitemap()
    {
        try
        {
            await SetContentAsync(SiteUrl.ContentKey, "");
            var html = await PageAsync("/");
            Assert.DoesNotContain("rel=\"canonical\"", html);
            Assert.DoesNotContain("hreflang=\"x-default\"", html);
            Assert.DoesNotContain("og:url", html);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/sitemap.xml")).StatusCode);
            Assert.DoesNotContain("Sitemap:", await PageAsync("/robots.txt"));
            // A forged Host header cannot supply one (Testing is not Development).
            using var req = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
            req.Headers.Host = "evil.example";
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().SendAsync(req)).StatusCode);
        }
        finally
        {
            await SetContentAsync(SiteUrl.ContentKey, Base);
        }
        Assert.Contains("rel=\"canonical\"", await PageAsync("/"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/en")]
    public async Task Public_pages_have_no_noindex(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.False(r.Headers.Contains("X-Robots-Tag"));
        Assert.DoesNotContain("noindex", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_admin_and_json_responses_are_noindex()
    {
        var login = await f.NewClient().GetAsync("/Account/Login");
        Assert.Equal("noindex, nofollow", string.Join(",", login.Headers.GetValues("X-Robots-Tag")));
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\">", await login.Content.ReadAsStringAsync());

        var admin = await (await f.SignedInClientAsync()).GetAsync("/Admin/Dashboard");
        Assert.Equal("noindex, nofollow", string.Join(",", admin.Headers.GetValues("X-Robots-Tag")));
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\">", await admin.Content.ReadAsStringAsync());

        var css = await f.NewClient().GetAsync("/css/style.css");
        Assert.False(css.Headers.Contains("X-Robots-Tag"));
    }

    private static JsonElement JsonLd(string html)
    {
        var m = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(m.Success, "no JSON-LD");
        return JsonDocument.Parse(m.Groups[1].Value).RootElement.Clone();
    }

    [Fact]
    public async Task Json_ld_parses_omits_empty_values_and_has_no_sameAs_while_social_urls_are_empty()
    {
        var ld = JsonLd(await PageAsync("/en"));
        Assert.Equal("Organization", ld.GetProperty("@type").GetString());
        Assert.Equal("Innovest Holding", ld.GetProperty("name").GetString());
        Assert.Equal($"{Base}/en", ld.GetProperty("url").GetString());
        Assert.False(ld.TryGetProperty("sameAs", out _));
        void NoEmpty(JsonElement e)
        {
            foreach (var p in e.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.String) Assert.False(string.IsNullOrWhiteSpace(p.Value.GetString()), p.Name);
                if (p.Value.ValueKind == JsonValueKind.Object) NoEmpty(p.Value);
            }
        }
        NoEmpty(ld);
    }

    [Fact]
    public async Task A_closing_script_tag_in_content_stays_inside_the_json_ld()
    {
        var original = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "site_name").Select(s => s.ContentAr).SingleAsync());
        const string evil = "QA</script><script>alert(1)</script>";
        try
        {
            await SetContentAsync("site_name", evil);
            var html = await PageAsync("/");
            Assert.DoesNotContain("</script><script>alert(1)", html);
            Assert.Equal(evil, JsonLd(html).GetProperty("name").GetString());
        }
        finally
        {
            await SetContentAsync("site_name", original);
        }
    }

    [Theory]
    [InlineData("http://example.org")]
    [InlineData("https://example.org/path")]
    [InlineData("https://example.org/?q=1")]
    [InlineData("javascript:alert(1)")]
    public async Task Admin_refuses_an_invalid_base_url(string value)
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ($"content[{SiteUrl.ContentKey}].Ar", value)));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("class=\"form-errors", await r.Content.ReadAsStringAsync());
        Assert.Equal(Base, await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == SiteUrl.ContentKey).Select(s => s.ContentAr).SingleAsync()));
    }

    [Fact]
    public async Task Admin_refuses_unbalanced_highlight_and_javascript_links()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        var before = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "faq_title").Select(s => s.ContentAr).SingleAsync());
        var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ("content[faq_title].Ar", "{a} {b}")));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(before, await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "faq_title").Select(s => s.ContentAr).SingleAsync()));

        var platform = await f.DbAsync(db => db.Platforms.AsNoTracking().FirstAsync());
        token = await client.GetTokenAsync("/Admin/Platforms");
        r = await client.PostAsync("/Admin/Platforms/Update", Form(token, ("Id", platform.Id.ToString()), ("TitleAr", platform.TitleAr), ("LinkUrl", "javascript:alert(1)")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Null(await f.DbAsync(db => db.Platforms.Where(p => p.Id == platform.Id).Select(p => p.LinkUrl).SingleAsync()));

        var social = await f.DbAsync(db => db.SocialLinks.AsNoTracking().FirstAsync());
        token = await client.GetTokenAsync("/Admin/SocialLinks");
        foreach (var bad in new[] { "http://example.org", "javascript:alert(1)", "//evil.example" })
        {
            r = await client.PostAsync("/Admin/SocialLinks/Update", Form(token, ("Id", social.Id.ToString()), ("Url", bad), ("IsActive", "true")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Null(await f.DbAsync(db => db.SocialLinks.Where(s => s.Id == social.Id).Select(s => s.Url).SingleAsync()));
        }
    }

    [Fact]
    public async Task Social_link_with_an_https_url_is_rendered_and_listed_in_sameAs()
    {
        var social = await f.DbAsync(db => db.SocialLinks.AsNoTracking().SingleAsync(s => s.Network == "linkedin"));
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/SocialLinks");
        try
        {
            var r = await client.PostAsync("/Admin/SocialLinks/Update",
                Form(token, ("Id", social.Id.ToString()), ("Url", "https://www.linkedin.com/company/qa"), ("SortOrder", "1"), ("IsActive", "true")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var html = await PageAsync("/");
            Assert.Contains("href=\"https://www.linkedin.com/company/qa\" class=\"social-link\" aria-label=\"LinkedIn\"", html);
            Assert.Equal("https://www.linkedin.com/company/qa", JsonLd(html).GetProperty("sameAs")[0].GetString());
        }
        finally
        {
            await f.DbAsync(async db => { (await db.SocialLinks.SingleAsync(s => s.Id == social.Id)).Url = null; return await db.SaveChangesAsync(); });
        }
    }
}

/// <summary>New admin screens: over-posting, pagination, Partners.Update.</summary>
[Collection("App")]
public class NewAdminScreenTests(TestAppFactory f)
{
    [Fact]
    public async Task Interest_option_create_ignores_over_posted_fields_and_requires_arabic()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/InterestOptions");
        var name = "خيار " + Guid.NewGuid().ToString("N")[..8];
        try
        {
            var r = await client.PostAsync("/Admin/InterestOptions/Create", Form(token,
                ("NameAr", name), ("NameEn", "Option"), ("Id", "999999"), ("CreatedAt", "2000-01-01"), ("IsActive", "false"), ("SortOrder", "-7")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var row = await f.DbAsync(db => db.InterestOptions.AsNoTracking().SingleAsync(o => o.NameAr == name));
            Assert.NotEqual(999999, row.Id);
            Assert.True(row.IsActive);
            Assert.True(row.SortOrder > 0);
            Assert.True(row.CreatedAt > DateTime.UtcNow.AddMinutes(-5));

            var empty = await client.PostAsync("/Admin/InterestOptions/Create", Form(token, ("NameAr", ""), ("NameEn", "Only English")));
            Assert.Equal(HttpStatusCode.Redirect, empty.StatusCode);
            Assert.Equal(0, await f.DbAsync(db => db.InterestOptions.CountAsync(o => o.NameEn == "Only English")));
        }
        finally
        {
            await f.DbAsync(db => db.InterestOptions.Where(o => o.NameAr == name).ExecuteDeleteAsync());
        }
    }

    [Fact]
    public async Task Partner_english_name_can_be_added_to_an_existing_partner()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Partners");
        var name = "شريك " + Guid.NewGuid().ToString("N")[..8];
        var id = await f.DbAsync(async db => { var p = new Partner { NameAr = name, SortOrder = 900 }; db.Partners.Add(p); await db.SaveChangesAsync(); return p.Id; });
        try
        {
            var r = await client.PostAsync("/Admin/Partners/Update", Form(token, ("Id", id.ToString()), ("NameAr", name), ("NameEn", "QA Partner")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal("QA Partner", await f.DbAsync(db => db.Partners.Where(p => p.Id == id).Select(p => p.NameEn).SingleAsync()));
            Assert.Contains(">QA Partner</span>", await f.NewClient().GetStringAsync("/en"));
        }
        finally
        {
            await f.DbAsync(async db => { db.Partners.Remove(await db.Partners.SingleAsync(p => p.Id == id)); return await db.SaveChangesAsync(); });
        }
    }

    [Fact]
    public async Task Registrations_are_paged_fifty_per_page_and_a_page_out_of_range_is_clamped()
    {
        var marker = Unique("page");
        await f.DbAsync(async db =>
        {
            for (var i = 0; i < 55; i++)
                db.Registrations.Add(new Registration { FullName = "QA", Email = $"{marker}-{i}@example.com", Interest = "x", CreatedAt = DateTime.MaxValue.AddYears(-1).AddMinutes(-i) });
            return await db.SaveChangesAsync();
        });
        try
        {
            var client = await f.SignedInClientAsync();
            var p1 = await client.GetStringAsync("/Admin/Registrations");
            var p2 = await client.GetStringAsync("/Admin/Registrations?page=2");
            Assert.Equal(50, Regex.Matches(p1, Regex.Escape(marker)).Count);
            Assert.Equal(5, Regex.Matches(p2, Regex.Escape(marker)).Count);
            Assert.Contains("?page=2", p1);
            var far = await client.GetAsync("/Admin/Registrations?page=99999");
            Assert.Equal(HttpStatusCode.OK, far.StatusCode);
        }
        finally
        {
            await f.DbAsync(db => db.Registrations.Where(r => r.Email.StartsWith(marker)).ExecuteDeleteAsync());
        }
    }
}

/// <summary>Static-asset compression and caching, and the home-page news cap.</summary>
[Collection("App")]
public class StaticAssetAndNewsCapTests(TestAppFactory f)
{
    private async Task<HttpResponseMessage> GetAsync(string path, string? acceptEncoding = "br, gzip")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (acceptEncoding is not null) request.Headers.Add("Accept-Encoding", acceptEncoding);
        return await f.NewClient().SendAsync(request);
    }

    [Theory]
    [InlineData("/css/style.css")]
    [InlineData("/js/main.js")]
    public async Task Static_css_and_js_are_compressed(string path)
    {
        var r = await GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains(r.Content.Headers.ContentEncoding, e => e is "br" or "gzip");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/en")]
    [InlineData("/Account/Login")]
    public async Task Html_pages_are_never_compressed(string path)
    {
        var r = await GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Empty(r.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Form_json_is_never_compressed()
    {
        var c = f.NewClient();
        var token = await c.GetTokenAsync("/");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Home/Contact")
        {
            Content = Form(token, ("FullName", ""), ("Email", "x@example.com"), ("Subject", "s"), ("Message", "m"))
        };
        request.Headers.Add("Accept-Encoding", "br, gzip");
        var r = await c.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Empty(r.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Only_versioned_static_urls_are_cached_as_immutable()
    {
        var html = await f.NewClient().GetStringAsync("/");
        var versioned = Regex.Match(html, "/css/style[.]css[?]v=[A-Za-z0-9_-]+").Value;
        Assert.NotEmpty(versioned); // the layout really emits a content hash
        Assert.Equal("public, max-age=31536000, immutable", (await GetAsync(versioned)).Headers.CacheControl?.ToString());
        var plain = (await GetAsync("/css/style.css")).Headers.CacheControl?.ToString() ?? "";
        Assert.DoesNotContain("immutable", plain);
        Assert.DoesNotContain("max-age=31536000", plain);
    }

    [Fact]
    public async Task Home_shows_at_most_the_admin_set_number_of_news()
    {
        var original = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == ContentKeys.HomeNewsLimit).Select(s => s.ContentAr).SingleAsync());
        Assert.Equal("6", original); // delivered by the migration
        static int Cards(string html) => Regex.Matches(html, "class=\"news-card ").Count;
        Assert.Equal(3, Cards(await f.NewClient().GetStringAsync("/"))); // positive control: 3 active seed news < 6
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Content/Edit", Form(token, ($"content[{ContentKeys.HomeNewsLimit}].Ar", "2")))).StatusCode);
            Assert.Equal(2, Cards(await f.NewClient().GetStringAsync("/")));
            Assert.Equal(2, Cards(await f.NewClient().GetStringAsync("/en")));
            foreach (var bad in new[] { "0", "51", "abc", "-1" })
            {
                var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ($"content[{ContentKeys.HomeNewsLimit}].Ar", bad)));
                Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with an error, not saved
            }
            Assert.Equal("2", await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == ContentKeys.HomeNewsLimit).Select(s => s.ContentAr).SingleAsync()));
        }
        finally
        {
            await f.DbAsync(async db => { (await db.SiteContents.SingleAsync(s => s.SectionKey == ContentKeys.HomeNewsLimit)).ContentAr = original; return await db.SaveChangesAsync(); });
        }
    }
}

/// <summary>Saving the content editor must not rewrite a value whose text did not change (line-break form only).</summary>
[Collection("App")]
public class ContentLineBreakTests(TestAppFactory f)
{
    [Fact]
    public async Task Unchanged_multiline_value_is_not_rewritten_whatever_its_line_breaks()
    {
        const string key = "about_years_label";
        var cr = ((char)13).ToString();
        var lf = ((char)10).ToString();
        var stamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var original = await f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == key));
        async Task Set(string ar) => await f.DbAsync(async db =>
        {
            var row = await db.SiteContents.SingleAsync(s => s.SectionKey == key);
            row.ContentAr = ar; row.ContentEn = null; row.UpdatedAt = stamp;
            return await db.SaveChangesAsync();
        });
        Task<SiteContent> Read() => f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == key));

        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        async Task Post(string ar) => Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Content/Edit",
            Form(token, ($"content[{key}].Ar", ar), ($"content[{key}].En", "")))).StatusCode);
        try
        {
            var storedCrlf = "a1" + cr + lf + "b2";
            await Set(storedCrlf);
            await Post("a1" + cr + lf + "b2");  // what a browser posts for an untouched textarea
            await Post("a1" + lf + "b2");
            var row = await Read();
            Assert.Equal(storedCrlf, row.ContentAr); // byte-identical: not rewritten
            Assert.Equal(stamp, row.UpdatedAt);      // not marked as edited

            await Post("a1" + cr + lf + "c3");       // a real change is saved, in the LF form
            row = await Read();
            Assert.Equal("a1" + lf + "c3", row.ContentAr);
            Assert.NotEqual(stamp, row.UpdatedAt);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var row = await db.SiteContents.SingleAsync(s => s.SectionKey == key);
                row.ContentAr = original.ContentAr; row.ContentEn = original.ContentEn; row.UpdatedAt = original.UpdatedAt;
                return await db.SaveChangesAsync();
            });
        }
    }
}

/// <summary>
/// Navigation markup that the browser behaviour depends on (the behaviour itself is checked in a browser): the mobile
/// platforms trigger is a disclosure button wired to its sub-list, and the page script's smooth-scroll handler is not
/// attached to it (otherwise a tap closed the drawer instead of opening the sub-list).
/// </summary>
[Collection("App")]
public class NavigationMarkupTests(TestAppFactory f)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/en")]
    public async Task Mobile_platforms_trigger_is_a_disclosure_button_for_its_sub_list(string path)
    {
        var html = await f.NewClient().GetStringAsync(path);
        var trigger = Regex.Match(html, "<a [^>]*class=\"nav-link mobile-dropdown-trigger\"[^>]*>");
        Assert.True(trigger.Success, "no mobile dropdown trigger");
        Assert.Contains("role=\"button\"", trigger.Value);
        Assert.Contains("aria-expanded=\"false\"", trigger.Value);
        var controls = Regex.Match(trigger.Value, "aria-controls=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(controls);
        Assert.Contains($"<div class=\"mobile-dropdown-items\" id=\"{controls}\">", html);
    }

    [Fact]
    public async Task Page_script_keeps_the_trigger_out_of_the_smooth_scroll_handler()
    {
        var js = await f.NewClient().GetStringAsync("/js/main.js");
        Assert.Contains("querySelectorAll('a[href^=\"#\"]:not(.mobile-dropdown-trigger)')", js);
        Assert.Contains("trigger.setAttribute('aria-expanded', open ? 'true' : 'false')", js);
    }

    [Fact]
    public async Task Header_does_not_clip_the_desktop_dropdown_vertically()
    {
        var css = await f.NewClient().GetStringAsync("/css/style.css");
        var header = Regex.Match(css, "#main-header [{][^}]*[}]").Value;
        Assert.Contains("overflow-y: visible;", header);
        Assert.DoesNotContain("overflow: hidden;", header); // the declaration (the comment names it)
        Assert.Contains(".nav-dropdown:focus-within .dropdown-menu", css); // keyboard users reach the sub-items too
    }
}
