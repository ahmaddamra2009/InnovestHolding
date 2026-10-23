using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 final fix round, admin part: Hassan F4 (no dead light-logo alt), F5 (no section switch that is always refused),
/// F6 (customised footer drops unpublished pages; Pages/Edit hint), Yasser L-C5F-1 (custom social cap) and I-C5F-1
/// (no Unicode format characters in links), QA C5-4/5/6/7/8. Every test restores what it changed.
/// </summary>
[Collection("App")]
public class C5FixAdminTests(TestAppFactory f)
{
    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static string FormOf(string html, string marker)
    {
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, marker);
        return html[start..html.IndexOf("</form>", start, StringComparison.Ordinal)];
    }

    // ── F4: the light logo is never drawn, so its form has no alt pair and a posted alt is not stored ──

    [Fact]
    public async Task Light_logo_form_has_no_alt_pair_and_a_posted_alt_is_not_stored()
    {
        var client = await f.SignedInClientAsync();
        var html = await (await client.GetAsync("/Admin/Content/Edit")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("name=\"ImageAltAr\"", FormOf(html, "id=\"image-logo_on_light\""));
        Assert.DoesNotContain("name=\"ImageAltEn\"", FormOf(html, "id=\"image-logo_on_light\""));
        Assert.Contains("name=\"ImageAltAr\"", FormOf(html, "id=\"image-logo_on_dark\""));     // control: a drawn logo keeps it
        Assert.Contains("name=\"ImageAltAr\"", FormOf(html, "id=\"image-hero_image\""));

        string? fileKey = null;
        try
        {
            var token = await client.GetTokenAsync("/Admin/Content/Edit");
            var r = await client.PostAsync("/Admin/Content/SiteImage", MediaUploadTests.Multipart(token,
                ("logo.webp", ImageInspectorTests.Fixture("lossy.webp"), "image/webp"),
                ("Key", SiteImages.LogoOnLight), ("ImageAltAr", "بديل"), ("ImageAltEn", "Alt")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            fileKey = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == SiteImages.LogoOnLight).Select(s => s.ContentAr).SingleAsync());
            var media = await f.DbAsync(db => db.MediaFiles.AsNoTracking().SingleAsync(m => m.FileKey == fileKey));
            Assert.True(string.IsNullOrEmpty(media.AltAr), media.AltAr);
            Assert.True(string.IsNullOrEmpty(media.AltEn), media.AltEn);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var rows = await db.SiteContents.Where(s => s.SectionKey == SiteImages.LogoOnLight).ToListAsync();
                var files = rows.Select(r => r.ContentAr).ToList();
                db.MediaFiles.RemoveRange(db.MediaFiles.Where(m => files.Contains(m.FileKey)));
                db.SiteContents.RemoveRange(rows);
                return await db.SaveChangesAsync();
            });
        }
    }

    // ── F5 + C5-4: every offered section switch succeeds; fixed sections show «ثابت»; the card returns to Pages/Edit ──

    private async Task WithAllSectionsRestoredAsync(Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.PageSections.AsNoTracking().Select(r => new { r.Id, r.IsVisible, r.SortOrder }).ToListAsync());
        try { await body(); }
        finally
        {
            await f.DbAsync(async db =>
            {
                foreach (var row in await db.PageSections.ToListAsync())
                    if (saved.FirstOrDefault(s => s.Id == row.Id) is { } s) { row.IsVisible = s.IsVisible; row.SortOrder = s.SortOrder; }
                return await db.SaveChangesAsync();
            });
        }
    }

    private static List<string> OfferedToggles(string html, string page) =>
        Regex.Matches(html, $"action=\"/Admin/PageSections/Toggle/{Regex.Escape(page)}\"[^>]*>(?:\\s*<input type=\"hidden\" name=\"from\" value=\"pages\">)?\\s*<input type=\"hidden\" name=\"section\" value=\"([a-z_]+)\">")
            .Select(m => m.Groups[1].Value).ToList();

    [Fact]
    public async Task Every_offered_section_switch_succeeds_and_fixed_sections_have_none()
    {
        var client = await f.SignedInClientAsync();
        var offeredTotal = 0;
        await WithAllSectionsRestoredAsync(async () =>
        {
            foreach (var page in PageSectionRegistry.Pages.Keys)
            {
                var html = await (await client.GetAsync($"/Admin/PageSections/Index/{page}")).Content.ReadAsStringAsync();
                var token = ExtractToken(html);
                var offered = OfferedToggles(html, page);
                var defs = PageSectionRegistry.Of(page);
                // Default data: everything visible. A switch exactly for the hideable sections of a page with ≥ 2 visible.
                var expected = defs.Count > 1 ? defs.Where(d => d.CanHide).Select(d => d.Key).OrderBy(k => k, StringComparer.Ordinal).ToList() : new List<string>();
                Assert.Equal(expected, offered.OrderBy(k => k, StringComparer.Ordinal).ToList());
                Assert.Equal(defs.Count(d => !d.CanHide), Regex.Matches(html, "class=\"badge-active section-fixed\"").Count);
                foreach (var section in offered)
                {
                    var before = await f.DbAsync(db => db.PageSections.Where(r => r.PageKey == page && r.SectionKey == section).Select(r => r.IsVisible).SingleAsync());
                    var r = await client.PostAsync($"/Admin/PageSections/Toggle/{page}", Form(token, ("section", section)));
                    Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
                    Assert.NotEqual(before, await f.DbAsync(db => db.PageSections.Where(r => r.PageKey == page && r.SectionKey == section).Select(r => r.IsVisible).SingleAsync()));
                    await client.PostAsync($"/Admin/PageSections/Toggle/{page}", Form(token, ("section", section)));   // back
                    offeredTotal++;
                }
            }
        });
        Assert.True(offeredTotal >= 20, $"only {offeredTotal} switches exercised");

        // The TV hub: its only section is fixed — no switch on the screen nor in Pages/Edit, «ثابت», and a POST is refused.
        var tv = Decode(await (await client.GetAsync("/Admin/PageSections/Index/tv")).Content.ReadAsStringAsync());
        Assert.Empty(OfferedToggles(tv, "tv"));
        Assert.Contains("class=\"badge-active section-fixed\" title=\"المحتوى الرئيسي للصفحة: يظهر دائماً ولا يُخفى.\">ثابت</span>", tv);
        var tvEdit = await (await client.GetAsync("/Admin/Pages/Edit/tv")).Content.ReadAsStringAsync();
        Assert.Contains("id=\"blocks\"", tvEdit);
        Assert.DoesNotContain("/Admin/PageSections/Toggle/tv", tvEdit);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/PageSections/Toggle/tv", Form(ExtractToken(tvEdit), ("section", "units")))).StatusCode);
    }

    [Fact]
    public async Task The_last_visible_section_shows_its_state_without_a_switch()
    {
        var client = await f.SignedInClientAsync();
        await WithAllSectionsRestoredAsync(async () =>
        {
            await f.DbAsync(async db =>
            {
                (await db.PageSections.SingleAsync(r => r.PageKey == PageKeys.NetworkInvestors && r.SectionKey == "investor_services")).IsVisible = false;
                return await db.SaveChangesAsync();
            });
            var html = await (await client.GetAsync($"/Admin/PageSections/Index/{PageKeys.NetworkInvestors}")).Content.ReadAsStringAsync();
            Assert.Equal(["investor_services"], OfferedToggles(html, PageKeys.NetworkInvestors));   // the hidden one can be shown
            Assert.Contains("class=\"badge-active section-last\"", html);
            // The server agrees: hiding the last visible one is refused (nothing changes).
            await client.PostAsync($"/Admin/PageSections/Toggle/{PageKeys.NetworkInvestors}", Form(ExtractToken(html), ("section", "join_cta")));
            Assert.True(await f.DbAsync(db => db.PageSections.Where(r => r.PageKey == PageKeys.NetworkInvestors && r.SectionKey == "join_cta").Select(r => r.IsVisible).SingleAsync()));
        });
    }

    [Fact] // QA C5-4: the card inside Pages/Edit returns to Pages/Edit (anchored); unsaved edits are guarded by a confirm
    public async Task Sections_card_in_Pages_edit_returns_there()
    {
        var client = await f.SignedInClientAsync();
        var html = await (await client.GetAsync("/Admin/Pages/Edit/forum")).Content.ReadAsStringAsync();
        var token = ExtractToken(html);
        Assert.Contains("<input type=\"hidden\" name=\"from\" value=\"pages\">", html);
        Assert.Contains("#blocks form.section-action", html);
        Assert.Contains("data-unsaved=\"", html);
        var screen = await (await client.GetAsync("/Admin/PageSections/Index/forum")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("name=\"from\"", screen);   // the screen itself posts no from

        await WithAllSectionsRestoredAsync(async () =>
        {
            var r = await client.PostAsync("/Admin/PageSections/Toggle/forum", Form(token, ("section", "cta_band"), ("from", "pages")));
            Assert.Equal("/Admin/Pages/Edit/forum#section-cta_band", r.Headers.Location!.OriginalString);
            r = await client.PostAsync("/Admin/PageSections/Move/forum", Form(token, ("section", "events"), ("direction", "-1"), ("from", "pages")));
            Assert.Equal("/Admin/Pages/Edit/forum#section-events", r.Headers.Location!.OriginalString);
            r = await client.PostAsync("/Admin/PageSections/Toggle/forum", Form(token, ("section", "cta_band"), ("from", "https://evil.example")));
            Assert.Equal("/Admin/PageSections/Index/forum#section-cta_band", r.Headers.Location!.OriginalString);
        });
        await WithContentAsync("forum_events_limit", async () =>
        {
            var r = await client.PostAsync("/Admin/PageSections/SetLimit", Form(token, ("key", "forum_events_limit"), ("value", "3"), ("from", "pages")));
            Assert.Equal("/Admin/Pages/Edit/forum#section-events", r.Headers.Location!.OriginalString);
        });
    }

    private async Task WithContentAsync(string key, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.SiteContents.AsNoTracking().Where(s => s.SectionKey == key).Select(s => s.ContentAr).ToListAsync());
        try { await body(); }
        finally
        {
            await f.DbAsync(async db =>
            {
                var rows = await db.SiteContents.Where(s => s.SectionKey == key).ToListAsync();
                if (saved.Count == 0) db.SiteContents.RemoveRange(rows);
                else foreach (var row in rows) row.ContentAr = saved[0];
                return await db.SaveChangesAsync();
            });
        }
    }

    // ── F6: the customised footer drops a link to an unpublished page; Pages/Edit says ShowInFooter is automatic-only ──

    [Fact]
    public async Task Customised_footer_drops_links_to_unpublished_pages_and_Pages_edit_says_so()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Nav?tab=footer");
        async Task<List<string>> HrefsAsync() =>
            FooterBuilder.Build(new SiteViews(await f.Services.GetRequiredService<ISiteContentCache>().GetAsync(), false))
                .Columns.SelectMany(c => c.Links).Select(l => l.Href).ToList();

        // Automatic footer: no hint (control for the hint).
        var auto = await (await client.GetAsync("/Admin/Pages/Edit/news")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("id=\"footer-auto-only\"", auto);
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Nav/CustomiseFooter", Form(token))).StatusCode);
            Assert.True(await f.DbAsync(db => db.FooterLinks.AnyAsync(l => l.PageKey == PageKeys.News)));
            Assert.Contains("/news", await HrefsAsync());

            await SetPublishedAsync(PageKeys.News, false);
            var hrefs = await HrefsAsync();
            Assert.DoesNotContain("/news", hrefs);
            Assert.Contains("/contact", hrefs);   // control: the other links stay
            var rendered = await (await f.NewClient().GetAsync("/")).Content.ReadAsStringAsync();
            var footer = rendered[rendered.IndexOf("<footer class=\"site-footer\">", StringComparison.Ordinal)..];
            Assert.DoesNotContain("href=\"/news\"", footer);
            Assert.Contains("href=\"/contact\"", footer);

            var edit = Decode(await (await client.GetAsync("/Admin/Pages/Edit/news")).Content.ReadAsStringAsync());
            Assert.Contains("aria-describedby=\"footer-auto-only\"", edit);
            Assert.Contains("id=\"footer-auto-only\"", edit);
            Assert.Contains("هذا الخيار يعمل في الفوتر التلقائي فقط", edit);
            Assert.Contains("<a href=\"/Admin/Nav?tab=footer\">«القائمة والفوتر › أعمدة الفوتر»</a>", edit);
        }
        finally
        {
            await SetPublishedAsync(PageKeys.News, true);
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Nav/ResetFooter", Form(token))).StatusCode);
        }
        Assert.Equal(0, await f.DbAsync(db => db.FooterColumns.CountAsync()));
    }

    private Task<int> SetPublishedAsync(string key, bool published) => f.DbAsync(async db =>
    {
        (await db.Pages.SingleAsync(p => p.Key == key)).IsPublished = published;
        return await db.SaveChangesAsync();
    });

    // ── L-C5F-1: at most 10 custom social networks ──

    [Fact]
    public async Task Custom_social_networks_are_capped_at_ten()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/SocialLinks");
        var prefix = Unique("cap")[..12];
        var existing = await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == SocialLink.Custom));
        Assert.True(existing < SocialLinksController.MaxCustom);
        try
        {
            await f.DbAsync(async db =>
            {
                for (var i = existing; i < SocialLinksController.MaxCustom - 1; i++)
                    db.SocialLinks.Add(new SocialLink { Network = SocialLink.Custom, Url = "https://example.com/" + i, Label = prefix + i, IconKey = "globe", SortOrder = 900 + i, IsActive = true });
                return await db.SaveChangesAsync();
            });
            // The 10th is accepted (control: the cap is not below 10).
            await client.PostAsync("/Admin/SocialLinks/Create", Form(token, ("Network", "custom"), ("Url", "https://example.com/10"), ("Label", prefix + "ten"), ("IconKey", "globe")));
            Assert.Equal(SocialLinksController.MaxCustom, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == SocialLink.Custom)));

            // The 11th is refused, with the Arabic message; the form no longer offers «شبكة أخرى».
            var r = await client.PostAsync("/Admin/SocialLinks/Create", Form(token, ("Network", "custom"), ("Url", "https://example.com/11"), ("Label", prefix + "eleven"), ("IconKey", "globe")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(SocialLinksController.MaxCustom, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == SocialLink.Custom)));
            var html = Decode(await (await client.GetAsync("/Admin/SocialLinks")).Content.ReadAsStringAsync());
            Assert.Contains("الحد الأقصى 10 شبكات أخرى. احذف واحدة قبل إضافة غيرها.", html);
            Assert.DoesNotContain("<option value=\"custom\">", html);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                db.SocialLinks.RemoveRange(db.SocialLinks.Where(s => s.Network == SocialLink.Custom && s.Label!.StartsWith(prefix)));
                return await db.SaveChangesAsync();
            });
        }
        var after = Decode(await (await client.GetAsync("/Admin/SocialLinks")).Content.ReadAsStringAsync());
        Assert.Contains("<option value=\"custom\">", after);
    }

    // ── I-C5F-1: no Unicode format (Cf) character anywhere in a link ──

    public static readonly TheoryData<string> FormatChars = ["‮", "‪", "⁦", "​", "‌", "‍", "‎", "‏", "﻿", "­", "؜"];

    [Theory]
    [MemberData(nameof(FormatChars))]
    public void Links_with_a_format_character_are_refused(string cf)
    {
        Assert.True(SiteUrl.IsHttpsUrl("https://evil.example/x"));   // control
        foreach (var url in new[] { $"https://evil.example{cf}/x", $"https://evil{cf}.example/x", $"https://evil.example/x{cf}y", $"https://evil.example/?q={cf}", $"https://evil.example/#a{cf}" })
        {
            Assert.False(SiteUrl.IsHttpsUrl(url), url);
            Assert.False(SafeHrefAttribute.IsSafe(url), url);
            Assert.False(ContentKeys.IsExternalLink(url), url);
        }
        Assert.False(SafeHrefAttribute.IsSafe($"/about{cf}"));
        Assert.False(SiteUrl.TryNormalize($"https://www.example.com{cf}", out _));
        Assert.True(SiteUrl.TryNormalize("https://www.example.com", out _));   // control
    }

    [Fact] // the rule reaches the admin forms: a social link URL with U+202E is not saved
    public async Task A_social_url_with_a_bidi_override_is_not_saved()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/SocialLinks");
        var label = Unique("bidi")[..14];
        try
        {
            await client.PostAsync("/Admin/SocialLinks/Create", Form(token, ("Network", "custom"), ("Url", "https://evil.example‮/x"), ("Label", label), ("IconKey", "globe")));
            Assert.Equal(0, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Label == label)));
            await client.PostAsync("/Admin/SocialLinks/Create", Form(token, ("Network", "custom"), ("Url", "https://evil.example/x"), ("Label", label), ("IconKey", "globe")));
            Assert.Equal(1, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Label == label)));   // control
        }
        finally
        {
            await f.DbAsync(async db => { db.SocialLinks.RemoveRange(db.SocialLinks.Where(s => s.Label == label)); return await db.SaveChangesAsync(); });
        }
    }

    // ── QA C5-6 / C5-7 / C5-8 / C5-5 ──

    [Fact]
    public async Task Nav_row_switches_have_distinct_tooltips()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Nav");
        var title = Unique("tip")[..12];
        await client.PostAsync("/Admin/Nav/Create", Form(token, ("TitleAr", title), ("Href", "https://example.com/t")));
        var id = await f.DbAsync(db => db.NavItems.Where(n => n.TitleAr == title).Select(n => n.Id).SingleAsync());
        try
        {
            var html = Decode(await (await client.GetAsync("/Admin/Nav")).Content.ReadAsStringAsync());
            Assert.Contains($"title=\"تبديل ظهور «{title}» في الموقع (القائمة والفوتر)\"", html);
            Assert.Contains($"title=\"تبديل ظهور «{title}» في القائمة العلوية فقط\"", html);
        }
        finally
        {
            await client.PostAsync("/Admin/Nav/Delete", Form(token, ("id", id.ToString())));
        }
    }

    [Fact]
    public async Task Sections_screen_is_named_for_all_pages()
    {
        var client = await f.SignedInClientAsync();
        var html = Decode(await (await client.GetAsync("/Admin/PageSections")).Content.ReadAsStringAsync());
        Assert.DoesNotContain("أقسام الصفحة الرئيسية", html);
        Assert.Contains("<span class=\"admin-card-title\">أقسام الصفحات: ", html);
        Assert.Matches("href=\"/Admin/PageSections\"[^>]*>[\\s\\S]{0,400}?أقسام الصفحات", html);   // the sidebar entry
        var other = Decode(await (await client.GetAsync("/Admin/PageSections/Index/forum")).Content.ReadAsStringAsync());
        Assert.Contains("<span class=\"admin-card-title\">أقسام الصفحات: ", other);
    }

    [Fact]
    public async Task Site_texts_search_matches_the_group_name()
    {
        var client = await f.SignedInClientAsync();
        var html = Decode(await (await client.GetAsync("/Admin/SiteTexts?tab=all&q=404")).Content.ReadAsStringAsync());
        Assert.DoesNotContain("لا توجد نتائج", html);
        Assert.Contains("texts[Ui_NotFoundTitle].Ar", html);
        Assert.Contains("texts[Msg_ErrorReference].Ar", html);
        // Control: a word that is nowhere still finds nothing.
        var none = Decode(await (await client.GetAsync("/Admin/SiteTexts?tab=all&q=zzqqxx")).Content.ReadAsStringAsync());
        Assert.Contains("لا توجد نتائج", none);
    }

    [Fact] // C5-5 is CSS: a text tripwire only — the 375 px measurement is the browser pass's (Stage 9)
    public async Task Sticky_action_bar_wraps()
    {
        var css = await f.NewClient().GetStringAsync("/css/admin.css");
        Assert.Matches(@"\.sticky-actions \{[^}]*flex-wrap: wrap;", css);
    }
}
