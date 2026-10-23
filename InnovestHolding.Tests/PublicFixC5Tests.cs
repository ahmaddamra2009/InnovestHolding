using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 final round, public part: Noor F1–F4 (SEO-C5-VERIFY), Hassan F1/F2/F8 (REVIEW-C5 FINAL), Yasser L-C5F-2,
/// Nader C5-1/C5-3. Every test restores what it changed.
/// </summary>
[Collection("App")]
public class PublicFixC5Tests(TestAppFactory f)
{
    private async Task<string> PageAsync(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadAsStringAsync();
    }

    private static string JsonLd(string html) =>
        Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;

    /// <summary>Every string value of <paramref name="name"/> anywhere in the JSON-LD (decoded: the encoder writes + as +).</summary>
    private static List<string> LdValues(string ld, string name)
    {
        var found = new List<string>();
        void Walk(System.Text.Json.JsonElement e)
        {
            if (e.ValueKind == System.Text.Json.JsonValueKind.Object)
                foreach (var p in e.EnumerateObject())
                {
                    if (p.Name == name && p.Value.ValueKind == System.Text.Json.JsonValueKind.String) found.Add(p.Value.GetString()!);
                    Walk(p.Value);
                }
            else if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var x in e.EnumerateArray()) Walk(x);
        }
        using var doc = System.Text.Json.JsonDocument.Parse(ld);
        Walk(doc.RootElement);
        return found;
    }

    private static bool Indexed(string html) => !html.Contains("noindex", StringComparison.Ordinal);

    private Task SetSectionAsync(string page, string section, bool visible) => f.DbAsync(async db =>
    {
        (await db.PageSections.SingleAsync(r => r.PageKey == page && r.SectionKey == section)).IsVisible = visible;
        return await db.SaveChangesAsync();
    });

    private Task SetPublishedAsync(string key, bool published) => f.DbAsync(async db =>
    {
        (await db.Pages.SingleAsync(p => p.Key == key)).IsPublished = published;
        return await db.SaveChangesAsync();
    });

    private Task<string?> StoredAsync(string key) =>
        f.DbAsync(db => db.SiteContents.Where(c => c.SectionKey == key).Select(c => c.ContentAr).SingleOrDefaultAsync());

    private Task SetContentAsync(string key, string? ar, string? en = null) => f.DbAsync(async db =>
    {
        var row = await db.SiteContents.SingleAsync(s => s.SectionKey == key);
        row.ContentAr = ar ?? "";
        row.ContentEn = en;
        return await db.SaveChangesAsync();
    });

    private async Task WithContentRestoredAsync(string key, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.SiteContents.AsNoTracking().SingleAsync(s => s.SectionKey == key));
        try { await body(); }
        finally { await SetContentAsync(key, saved.ContentAr, saved.ContentEn); }
    }

    // ── Noor F1: JSON-LD follows the admin's section switch ──

    [Theory]
    [InlineData("/ecosystem")]
    [InlineData("/en/ecosystem")]
    public async Task Ecosystem_ItemList_is_not_emitted_while_the_units_section_is_hidden(string path)
    {
        var before = JsonLd(await PageAsync(path));
        Assert.Contains("\"ItemList\"", before); // positive control: shown → described
        await SetSectionAsync(PageKeys.Ecosystem, "units", false);
        try
        {
            var html = await PageAsync(path);
            Assert.DoesNotContain("class=\"unit-card", html); // the cards are gone from <main> …
            var ld = JsonLd(html);
            Assert.DoesNotContain("\"ItemList\"", ld); // … and from the structured data
            Assert.Contains("\"CollectionPage\"", ld); // the page node itself stays
        }
        finally { await SetSectionAsync(PageKeys.Ecosystem, "units", true); }
        Assert.Equal(before, JsonLd(await PageAsync(path)));
    }

    // ── Noor F2: admin-typed external links carry rel="noopener noreferrer" ──

    [Fact]
    public void External_rel_rule()
    {
        Assert.Equal("noopener noreferrer", SafeHrefAttribute.RelFor("https://partner.example.org/x"));
        Assert.Equal("noopener noreferrer", SafeHrefAttribute.RelFor(" HTTP://partner.example.org"));
        foreach (var internalHref in new[] { "/about", "/en/contact", "#news", "about/team", "mailto:a@example.com", "tel:+201", "", null })
            Assert.Null(SafeHrefAttribute.RelFor(internalHref));
    }

    [Fact]
    public async Task Nav_extra_links_and_service_learn_more_links_to_other_sites_get_rel_internal_ones_do_not()
    {
        var platform = await f.DbAsync(db => db.Platforms.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new { p.Id, p.LinkUrl }).FirstAsync());
        var external = "https://partner.example.org/c5-" + Guid.NewGuid().ToString("N")[..8];
        var navIds = new List<int>();
        try
        {
            await f.DbAsync(async db =>
            {
                var ext = new NavItem { TitleAr = "رابط خارجي C5", TitleEn = "External C5", Href = external, SortOrder = 900, ShowInHeader = true };
                var inn = new NavItem { TitleAr = "رابط داخلي C5", TitleEn = "Internal C5", Href = "/about#c5-internal", SortOrder = 901, ShowInHeader = true };
                db.NavItems.AddRange(ext, inn);
                (await db.Platforms.SingleAsync(p => p.Id == platform.Id)).LinkUrl = external + "/service";
                var n = await db.SaveChangesAsync();
                navIds.AddRange([ext.Id, inn.Id]);
                return n;
            });
            foreach (var path in new[] { "/", "/en" })
            {
                var html = await PageAsync(path);
                // header + drawer: both anchors of the external link carry rel; the internal one has none
                Assert.Equal(2, Regex.Matches(html, $"<a href=\"{Regex.Escape(external)}\" class=\"nav-link\" rel=\"noopener noreferrer\"").Count);
                Assert.Equal(2, Regex.Matches(html, "<a href=\"/about#c5-internal\" class=\"nav-link\" lang").Count + Regex.Matches(html, "<a href=\"/about#c5-internal\" class=\"nav-link\">").Count);
                Assert.DoesNotContain("href=\"/about#c5-internal\" class=\"nav-link\" rel=", html);
                Assert.Contains($"<a href=\"{external}/service\" class=\"unit-link\" rel=\"noopener noreferrer\">", html);
            }

            // NC-0: an internal «learn more» renders exactly as before (no rel attribute).
            await f.DbAsync(async db => { (await db.Platforms.SingleAsync(p => p.Id == platform.Id)).LinkUrl = "/contact"; return await db.SaveChangesAsync(); });
            Assert.Contains("<a href=\"/contact\" class=\"unit-link\">", await PageAsync("/"));
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                db.NavItems.RemoveRange(db.NavItems.Where(n => navIds.Contains(n.Id)));
                (await db.Platforms.SingleAsync(p => p.Id == platform.Id)).LinkUrl = platform.LinkUrl;
                return await db.SaveChangesAsync();
            });
        }
    }

    // ── Noor F3: contact_phone / org_address_country ──

    [Theory]
    [InlineData("+20 01141410808", "+201141410808")] // the seed: trunk 0 after the country code dropped
    [InlineData("00201141410808", "+201141410808")]   // 00 → +
    [InlineData("+20 114-141-0808", "+201141410808")]
    [InlineData("+201141410808", "+201141410808")]
    [InlineData("+2001141410808", "+201141410808")]
    [InlineData("+966 50 123 4567", "+966501234567")]
    [InlineData("+44 020 7946 0958", "+442079460958")]
    [InlineData("+39 06 698 8 1234", "+390669881234")]  // Italy keeps its leading 0
    [InlineData("+1 415-555-0100", "+14155550100")]
    [InlineData("01141410808", null)]                 // no country code: not normalisable
    [InlineData("+20 0", null)]
    [InlineData("abc", null)]
    [InlineData("12", null)]
    [InlineData("+20 (114) 1410808", null)]
    [InlineData("+0 1141410808", null)]
    [InlineData("", null)]
    public void Phone_to_E164(string typed, string? expected) => Assert.Equal(expected, ContactFormats.ToE164(typed));

    [Theory]
    [InlineData("+20 01141410808", true)]
    [InlineData("0114 141 0808", true)]
    [InlineData("1234567", true)]
    [InlineData("123456", false)]        // 6 digits
    [InlineData("1234567890123456", false)] // 16 digits
    [InlineData("abc", false)]
    [InlineData("+20 114 abc", false)]
    [InlineData("(02) 1234567", false)]
    [InlineData("++201141410808", false)]
    [InlineData("٠١١٤١٤١٠٨٠٨", false)] // Arabic-Indic digits: ASCII only
    public void Phone_rule(string typed, bool ok) => Assert.Equal(ok, ContactFormats.IsPhone(typed));

    [Theory]
    [InlineData("EG", "EG")]
    [InlineData("eg", "EG")]
    [InlineData(" sa ", "SA")]
    [InlineData("Egypt", null)]
    [InlineData("E1", null)]
    [InlineData("XX", null)]
    [InlineData("EGY", null)]
    public void Country_rule(string typed, string? expected) => Assert.Equal(expected, ContactFormats.CountryCode(typed));

    [Fact]
    public async Task Editor_refuses_a_bad_phone_or_country_and_stores_the_country_upper_case()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Content/Edit");
        await WithContentRestoredAsync("contact_phone", () => WithContentRestoredAsync("org_address_country", async () =>
        {
            var phone = await StoredAsync("contact_phone");
            foreach (var bad in new[] { "abc", "12", "+20 (114) 1410808", "+20 114 141 0808 99999" })
            {
                var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ("content[contact_phone].Ar", bad)));
                Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with the message
                Assert.Contains("رقم هاتف غير صالح", await r.Content.ReadAsStringAsync());
                Assert.Equal(phone, await StoredAsync("contact_phone"));
            }
            var country = await StoredAsync("org_address_country");
            foreach (var bad in new[] { "Egypt", "E1", "XX" })
            {
                var r = await client.PostAsync("/Admin/Content/Edit", Form(token, ("content[org_address_country].Ar", bad)));
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                Assert.Contains("ISO 3166", await r.Content.ReadAsStringAsync());
                Assert.Equal(country, await StoredAsync("org_address_country"));
            }
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Content/Edit",
                Form(token, ("content[org_address_country].Ar", "eg"), ("content[contact_phone].Ar", "00201141410808")))).StatusCode);
            Assert.Equal("EG", await StoredAsync("org_address_country"));
            Assert.Equal("00201141410808", await StoredAsync("contact_phone")); // stored as typed
        }));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/en/contact")]
    public async Task Structured_data_carries_the_E164_phone_while_the_page_shows_it_as_typed(string path)
    {
        await WithContentRestoredAsync("contact_phone", () => WithContentRestoredAsync("org_address_country", async () =>
        {
            await SetContentAsync("contact_phone", "+20 01141410808");
            await SetContentAsync("org_address_country", "EG");
            var html = await PageAsync(path);
            var ld = JsonLd(html);
            Assert.Equal(["+201141410808", "+201141410808"], LdValues(ld, "telephone")); // Organization + ContactPoint
            Assert.Contains("+20 01141410808", WebUtility.HtmlDecode(html.Replace(ld, ""))); // the visible text keeps the admin's form
            Assert.Equal(["EG"], LdValues(ld, "addressCountry"));

            await SetContentAsync("contact_phone", "01141410808"); // no country code → no telephone at all
            await SetContentAsync("org_address_country", "Egypt"); // written past the editor (old data) → omitted
            html = await PageAsync(path);
            ld = JsonLd(html);
            Assert.Empty(LdValues(ld, "telephone"));
            Assert.Empty(LdValues(ld, "addressCountry"));
            Assert.Contains("01141410808", html);
            Assert.Contains("\"ContactPoint\"", ld); // the email still makes a contact point
        }));
    }

    // ── Noor F4: an English page needs English text to be indexable ──

    [Fact]
    public async Task English_page_without_English_intro_or_body_is_noindex_out_of_the_sitemap_and_unpaired()
    {
        // Shipped content: privacy and TV have English text → indexable in both languages, paired.
        foreach (var path in new[] { "/en/privacy", "/en/tv", "/privacy", "/tv" })
            Assert.True(Indexed(await PageAsync(path)), path);
        var sitemap = await f.NewClient().GetStringAsync("/sitemap.xml");
        Assert.Contains("/en/privacy</loc>", sitemap);
        Assert.Contains("/en/tv</loc>", sitemap);

        var saved = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.Privacy));
        Assert.False(string.IsNullOrWhiteSpace(saved.MetaTitleEn)); // the English meta stays set: only the text is missing
        try
        {
            await f.DbAsync(async db => { var p = await db.Pages.SingleAsync(x => x.Key == PageKeys.Privacy); p.IntroEn = null; p.BodyEn = " "; return await db.SaveChangesAsync(); });
            var en = await f.NewClient().GetAsync("/en/privacy");
            Assert.Equal(HttpStatusCode.OK, en.StatusCode);
            Assert.Contains("noindex", string.Join(",", en.Headers.GetValues("X-Robots-Tag")));
            var enHtml = await en.Content.ReadAsStringAsync();
            Assert.Contains("<meta name=\"robots\" content=\"noindex, follow\">", enHtml);
            Assert.DoesNotContain("rel=\"alternate\" hreflang", enHtml);
            var ar = await PageAsync("/privacy");
            Assert.True(Indexed(ar));
            Assert.DoesNotContain("rel=\"alternate\" hreflang", ar); // no pair while one side is not indexable
            sitemap = await f.NewClient().GetStringAsync("/sitemap.xml");
            Assert.DoesNotContain("/en/privacy</loc>", sitemap);
            Assert.Contains("/privacy</loc>", sitemap);

            // English text back (intro alone is enough) → indexable again.
            await f.DbAsync(async db => { (await db.Pages.SingleAsync(x => x.Key == PageKeys.Privacy)).IntroEn = "C5 English intro"; return await db.SaveChangesAsync(); });
            Assert.True(Indexed(await PageAsync("/en/privacy")));
        }
        finally
        {
            await f.DbAsync(async db => { var p = await db.Pages.SingleAsync(x => x.Key == PageKeys.Privacy); p.IntroEn = saved.IntroEn; p.BodyEn = saved.BodyEn; return await db.SaveChangesAsync(); });
        }
    }

    [Fact] // NC-0 of F4 on the migrated content: no page loses its English indexability through the new rule
    public async Task Every_shipped_page_with_English_meta_has_English_text()
    {
        var pages = await f.DbAsync(db => db.Pages.AsNoTracking().ToListAsync());
        Assert.True(pages.Count >= 20, $"checked {pages.Count} pages");
        var affected = pages.Where(p => SiteRoutes.IsKey(p.Key) && p.Key != PageKeys.Home
                                        && SiteRoutes.Get(p.Key).Kind != PageKind.List
                                        && !string.IsNullOrWhiteSpace(p.MetaTitleEn) && !string.IsNullOrWhiteSpace(p.MetaDescriptionEn)
                                        && (!string.IsNullOrWhiteSpace(p.IntroAr) || !string.IsNullOrWhiteSpace(p.BodyAr))
                                        && string.IsNullOrWhiteSpace(p.IntroEn) && string.IsNullOrWhiteSpace(p.BodyEn))
            .Select(p => p.Key).ToList();
        Assert.Empty(affected);
    }

    // ── Hassan F1: the register badge icon follows register_icon ──

    private const string UserPlusBadge =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"8.5\" cy=\"7\" r=\"4\"/><line x1=\"20\" y1=\"8\" x2=\"20\" y2=\"14\"/><line x1=\"23\" y1=\"11\" x2=\"17\" y2=\"11\"/></svg>";

    private static string RegisterBadge(string html)
    {
        var start = html.IndexOf("<div class=\"registration-content", StringComparison.Ordinal);
        Assert.True(start >= 0, "the page has the register block");
        var end = html.IndexOf("</h2>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/en")]
    public async Task Register_badge_renders_the_admin_icon_and_defaults_to_the_old_glyph(string path)
    {
        Assert.Contains(UserPlusBadge, RegisterBadge(await PageAsync(path))); // NC-0: default = byte-identical glyph
        await WithContentRestoredAsync("register_icon", async () =>
        {
            await SetContentAsync("register_icon", "calendar");
            var badge = RegisterBadge(await PageAsync(path));
            Assert.Contains(Icons.Svg("calendar")!, badge);
            Assert.DoesNotContain(UserPlusBadge, badge);
            await SetContentAsync("register_icon", "<script>x</script>"); // not a whitelist key → the default
            Assert.Contains(UserPlusBadge, RegisterBadge(await PageAsync(path)));
        });
    }

    // ── Hassan F2 / Nader C5-1: the privacy link next to every consent box ──

    public static readonly TheoryData<string> FormPages = new()
    {
        "/forum/registration", "/en/forum/registration", "/angel-network/join", "/en/angel-network/join",
        "/tv/sponsorship", "/en/tv/sponsorship", "/angel-network/opportunities", "/en/angel-network/opportunities",
    };

    private static string ConsentBox(string html)
    {
        var start = html.IndexOf("<div class=\"form-consent\">", StringComparison.Ordinal);
        Assert.True(start >= 0, "the page renders a consent box");
        var end = html.IndexOf("<input type=\"text\" name=\"Website\"", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [Theory]
    [MemberData(nameof(FormPages))]
    public async Task Consent_box_links_the_privacy_policy_in_a_new_tab(string path)
    {
        var english = path.StartsWith("/en/", StringComparison.Ordinal);
        var href = english ? "/en/privacy" : "/privacy";
        var box = ConsentBox(await PageAsync(path));
        // Shipped consent text has no {privacy} token → the link follows the label, outside it.
        Assert.Matches($"</label>\\s*<a href=\"{href}\" class=\"consent-privacy\" target=\"_blank\" rel=\"noopener\"( lang=\"ar\" dir=\"rtl\")?>"
                       + (english ? "Privacy policy" : "سياسة الخصوصية")
                       + "<span class=\"visually-hidden\"> " + (english ? "\\(opens in a new tab\\)" : "\\(يفتح في نافذة جديدة\\)") + "</span></a>", box);
        Assert.Contains(english ? "I agree that Innovest Holding may contact me" : "أوافق على أن تتواصل معي", box);
    }

    [Fact]
    public async Task Privacy_token_places_the_link_inside_the_consent_text_and_no_link_while_the_policy_is_empty()
    {
        await WithContentRestoredAsync(ContentKeys.FormsConsent, async () =>
        {
            await SetContentAsync(ContentKeys.FormsConsent, "أوافق على معالجة بياناتي وفق {privacy} فقط.", "I agree to my data being processed under the {privacy} only.");
            foreach (var (path, before, after, href) in new[]
            {
                ("/angel-network/join", "أوافق على معالجة بياناتي وفق ", " فقط.", "/privacy"),
                ("/en/angel-network/join", "I agree to my data being processed under the ", " only.", "/en/privacy"),
            })
            {
                var box = ConsentBox(await PageAsync(path));
                Assert.DoesNotContain("{privacy}", box);
                Assert.Contains($"{before}<a href=\"{href}\" class=\"consent-privacy\" target=\"_blank\" rel=\"noopener\"", box);
                Assert.Contains($"</a>{after}</span><span class=\"req\"", box); // inside the label
                Assert.DoesNotContain("form-consent-text", box);
            }

            // Policy body emptied → no link anywhere; the token shows the page title as plain text.
            var saved = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == PageKeys.Privacy));
            try
            {
                await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.Privacy)).BodyAr = ""; return await db.SaveChangesAsync(); });
                var box = ConsentBox(await PageAsync("/en/tv/sponsorship"));
                Assert.DoesNotContain("<a ", box);
                Assert.Contains("under the Privacy policy only.", box);

                // No token and no policy: the consent box is exactly the C4 markup.
                await SetContentAsync(ContentKeys.FormsConsent, "أوافق.", "I agree.");
                Assert.Equal("<div class=\"form-consent\">\n            <input type=\"checkbox\" id=\"sponsor-consent\" name=\"consent\" value=\"true\" required>\n"
                             + "            <label for=\"sponsor-consent\"><span>I agree.</span><span class=\"req\" aria-hidden=\"true\">*</span></label>\n          </div>",
                    Regex.Replace(ConsentBox(await PageAsync("/en/tv/sponsorship")).Replace("\r\n", "\n"), "\\s+$", ""));
            }
            finally
            {
                await f.DbAsync(async db => { (await db.Pages.SingleAsync(p => p.Key == PageKeys.Privacy)).BodyAr = saved.BodyAr; return await db.SaveChangesAsync(); });
            }
        });
    }

    // ── Hassan F8 / Yasser L-C5F-2: a POST to a form whose section is hidden is 404 ──

    public static readonly TheoryData<string, string, string, string> HiddenForms = new()
    {
        // page, section hidden, endpoint, page to fetch the token from
        { PageKeys.Home, "register", "/forms/register", "/" },
        { PageKeys.Home, "register", "/en/forms/register", "/en" },
        { PageKeys.Home, "register", "/Home/Register", "/" },
        { PageKeys.Home, "contact", "/Home/Contact", "/" },
        { PageKeys.NetworkOpportunities, "interest_form", "/forms/opportunity-interest", "/angel-network/opportunities" },
        { PageKeys.NetworkOpportunities, "interest_form", "/en/forms/opportunity-interest", "/en/angel-network/opportunities" },
    };

    private Task<int> RowsAsync(string email) => f.DbAsync(async db =>
        await db.Registrations.CountAsync(r => r.Email == email)
        + await db.ContactMessages.CountAsync(r => r.Email == email)
        + await db.OpportunityInterests.CountAsync(r => r.Email == email));

    private static FormUrlEncodedContent Body(string token, string email) => Form(token,
        ("FullName", "QA Hidden"), ("Email", email), ("Subject", "QA"), ("Message", "QA hidden section"), ("Consent", "true"));

    [Theory]
    [MemberData(nameof(HiddenForms))]
    public async Task Post_to_a_form_whose_section_is_hidden_is_404_and_writes_nothing(string page, string section, string endpoint, string tokenPage)
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync(tokenPage);
        var email = Unique("hidden") + "@example.com";
        await SetSectionAsync(page, section, false);
        try
        {
            var r = await client.PostAsync(endpoint, Body(token, email));
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal(0, await RowsAsync(email));
        }
        finally { await SetSectionAsync(page, section, true); }

        // Shown again: the same POST reaches the action (accepted or refused by validation — never 404).
        Assert.NotEqual(HttpStatusCode.NotFound, (await client.PostAsync(endpoint, Body(token, Unique("shown") + "@example.com"))).StatusCode);
    }

    [Fact] // the contact form is also on the Contact page: hiding the home section alone leaves /forms/contact open
    public async Task Contact_form_closes_only_when_no_page_shows_it()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        await SetSectionAsync(PageKeys.Home, "contact", false);
        try
        {
            Assert.NotEqual(HttpStatusCode.NotFound, (await client.PostAsync("/forms/contact", Body(token, Unique("c1") + "@example.com"))).StatusCode);
            Assert.NotEqual(HttpStatusCode.NotFound, (await client.PostAsync("/en/forms/contact", Body(token, Unique("c2") + "@example.com"))).StatusCode);
            await SetPublishedAsync(PageKeys.Contact, false);
            var email = Unique("c3") + "@example.com";
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/forms/contact", Body(token, email))).StatusCode);
            Assert.Equal(0, await RowsAsync(email));
        }
        finally
        {
            await SetPublishedAsync(PageKeys.Contact, true);
            await SetSectionAsync(PageKeys.Home, "contact", true);
        }
    }

    // ── Nader C5-3: the social icon row wraps (8 networks at 375 px) ──

    [Fact]
    public void Social_icon_row_wraps()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InnovestHolding.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var css = File.ReadAllText(Path.Combine(dir!.FullName, "wwwroot", "css", "style.css"));
        // Only the footer, only in the one-column phone layout (<= 576px): other places and widths keep today's layout.
        var phone = css[css.IndexOf("@media (max-width: 576px) {", StringComparison.Ordinal)..];
        phone = phone[..phone.IndexOf("\n}", StringComparison.Ordinal)];
        Assert.Contains("  footer .social-links { flex-wrap: wrap; }", phone);
        var rule = Regex.Match(css, @"\n\.social-links \{([^}]*)\}").Groups[1].Value;
        Assert.DoesNotContain("wrap", rule);
        Assert.Contains("gap: 10px;", rule); // 7 icons = 7×40 + 6×10 = 340px ≤ 375 − 2×16 (container padding): one row
    }

    // ── Nader C5-3b: the same row in the contact block (/contact and the home contact section) ──

    [Fact]
    public void Contact_social_icon_row_wraps_and_does_not_widen_the_grid()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InnovestHolding.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var css = File.ReadAllText(Path.Combine(dir!.FullName, "wwwroot", "css", "pages.css")).Replace("\r\n", "\n");
        // Only in the one-column phone layout (<= 576px), like the footer: up to 7 icons keep today's single row.
        const string phoneStart = "@media (max-width: 576px) {\n  .contact-social .social-links { flex-wrap: wrap; }\n  .contact-grid > .contact-info { min-width: 0; }\n}";
        Assert.Contains(phoneStart, css);
        var outside = css.Replace(phoneStart, "");
        Assert.DoesNotContain(".contact-social .social-links", outside); // no unconditional rule: wider screens unchanged
        Assert.DoesNotContain(".contact-info { min-width", outside);
        // The selector matches the markup: .contact-info is a direct child of .contact-grid and holds the social row.
        var view = File.ReadAllText(Path.Combine(dir.FullName, "Views", "Shared", "Blocks", "_ContactBlock.cshtml")).Replace("\r\n", "\n");
        Assert.Matches("<div class=\"contact-grid\">\n  <div class=\"contact-info[ \"]", view);
        var info = view[view.IndexOf("<div class=\"contact-info", StringComparison.Ordinal)..view.IndexOf("<div class=\"contact-form-wrap", StringComparison.Ordinal)];
        Assert.Contains("<div class=\"contact-social\">", info);
        Assert.Contains("<div class=\"social-links\">", info);
    }
}
