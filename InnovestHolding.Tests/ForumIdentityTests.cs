using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The forum identity on its own: scope, palette, contrast rule, value validation (F9).</summary>
public class ForumIdentityUnitTests
{
    public static IEnumerable<object[]> ForumKeys() =>
        new[] { PageKeys.Forum, PageKeys.ForumEvents, PageKeys.ForumSpeakers, PageKeys.ForumPartners, PageKeys.ForumRegistration }
            .Select(k => new object[] { k });

    [Theory, MemberData(nameof(ForumKeys))]
    public void Forum_pages_are_forum_pages(string key) => Assert.True(ForumIdentity.IsForumPage(key));

    [Fact] // every other page of the route table, the 404 shell and nothing at all are not
    public void Other_pages_are_not_forum_pages()
    {
        var others = SiteRoutes.All.Select(p => p.Key).Where(k => k != PageKeys.Forum && !k.StartsWith("forum.", StringComparison.Ordinal)).ToList();
        Assert.True(others.Count >= 14, $"searched {others.Count} keys");
        Assert.All(others, k => Assert.False(ForumIdentity.IsForumPage(k), k));
        Assert.False(ForumIdentity.IsForumPage("404"));
        Assert.False(ForumIdentity.IsForumPage(null));
        // Detail pages use their list page as shell key: the forum's are under the forum, the others are not.
        Assert.All(SiteRoutes.Details, d => Assert.Equal(d.Key.StartsWith("forum.", StringComparison.Ordinal), ForumIdentity.IsForumPage(d.Parent)));
    }

    [Fact] // the bundled palette passes every contrast rule (Layla's ratios)
    public void Default_palette_passes_contrast()
    {
        Assert.Empty(ForumIdentity.ContrastFailures(new Dictionary<string, string?>()));
        Assert.Equal(5.28, Math.Round(ForumIdentity.Contrast("#ffffff", "#d21e33"), 2), 2);
        Assert.True(ForumIdentity.Contrast("#ffffff", "#bd932e") < 3); // white on gold: refused by design
    }

    [Fact] // a pair under its minimum is reported with its measured ratio
    public void Low_contrast_pair_is_reported()
    {
        var failures = ForumIdentity.ContrastFailures(new Dictionary<string, string?> { [ForumIdentity.Primary] = "#bd932e" });
        var f = Assert.Single(failures, x => x.Pair.Foreground == ForumIdentity.OnPrimary && x.Pair.Background == ForumIdentity.Primary);
        Assert.True(f.Ratio < 4.5);
    }

    [Fact] // wwwroot/css/forum.css and ForumIdentity.Colors carry the same defaults (hex and r, g, b)
    public void Css_defaults_equal_the_code_defaults()
    {
        var css = File.ReadAllText(Path.Combine(ProjectDir(), "wwwroot", "css", "forum.css"));
        foreach (var c in ForumIdentity.Colors)
        {
            Assert.Contains($"{c.Variable}: {c.Default};", css);
            var (r, g, b) = ForumIdentity.Rgb(c.Default);
            Assert.Contains($"{c.Variable}-rgb: {r}, {g}, {b};", css);
        }
    }

    [Theory] // read-side check: only #RRGGBB reaches the CSS
    [InlineData("#fff")]
    [InlineData("red")]
    [InlineData("#12345g")]
    [InlineData("#123456;}")]
    [InlineData("url(x)")]
    [InlineData("#123456\nbody{}")]
    [InlineData("")]
    public void Invalid_stored_colour_is_ignored(string stored) =>
        Assert.Null(ForumIdentity.StoredColor(new Dictionary<string, TextPair> { ["k"] = new(stored, null) }, "k"));

    [Fact]
    public void Valid_stored_colour_is_normalised() =>
        Assert.Equal("#d21e33", ForumIdentity.StoredColor(new Dictionary<string, TextPair> { ["k"] = new(" #D21E33 ", null) }, "k"));

    [Fact] // fonts: keys of the fixed table only, never a family name
    public void Font_must_be_a_listed_key()
    {
        Assert.NotNull(ForumIdentity.FindFont("cairo"));
        Assert.Null(ForumIdentity.FindFont("Cairo"));
        Assert.Null(ForumIdentity.FindFont("x;}body{background:url(https://evil/)}"));
    }

    internal static string ProjectDir()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "InnovestHolding.csproj"))) return d.FullName;
        throw new InvalidOperationException("project directory not found");
    }
}

/// <summary>The forum identity end to end: themed pages, the stylesheet endpoint, the admin screen, logos, SEO (F9).</summary>
[Collection("App")]
public class ForumIdentityTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com";
    private const string ThemeLink = "<link rel=\"stylesheet\" href=\"/forum-theme.css?v=";

    private static readonly string[] ForumPaths =
        ["/forum", "/forum/events", "/forum/speakers", "/forum/partners", "/forum/registration"];

    private static readonly string[] OtherPaths =
        ["/", "/about", "/ecosystem", "/angel-network", "/angel-network/join", "/tv", "/tv/episodes", "/news", "/contact",
         "/privacy", "/no-such-page", "/forum/events/no-such-event"];

    private static IEnumerable<string> BothLanguages(IEnumerable<string> paths) =>
        paths.SelectMany(p => new[] { p, p == "/" ? "/en" : "/en" + p });

    private Task ResetAsync() => f.DbAsync(async db =>
    {
        var keys = ForumIdentity.SettingKeys.Concat(ForumIdentity.Images.Select(i => i.Key)).ToList();
        var rows = await db.SiteContents.Where(c => keys.Contains(c.SectionKey)).ToListAsync();
        var fileKeys = rows.Select(r => r.ContentAr).ToList();
        db.MediaFiles.RemoveRange(db.MediaFiles.Where(m => fileKeys.Contains(m.FileKey)));
        db.SiteContents.RemoveRange(rows);
        return await db.SaveChangesAsync();
    });

    private Task SetRowAsync(string key, string value) => f.DbAsync(async db =>
    {
        var row = await db.SiteContents.FirstOrDefaultAsync(c => c.SectionKey == key);
        if (row is null) db.SiteContents.Add(new SiteContent { SectionKey = key, ContentAr = value, UpdatedAt = DateTime.UtcNow });
        else row.ContentAr = value;
        return await db.SaveChangesAsync();
    });

    private Task<string?> StoredAsync(string key) =>
        f.DbAsync(db => db.SiteContents.Where(c => c.SectionKey == key).Select(c => (string?)c.ContentAr).SingleOrDefaultAsync());

    private static string Head(string html) => html[..html.IndexOf("</head>", StringComparison.Ordinal)];

    private static string Header(string html)
    {
        var start = html.IndexOf("<header id=\"main-header\"", StringComparison.Ordinal);
        return html[start..html.IndexOf("</header>", start, StringComparison.Ordinal)];
    }

    private static JsonElement Graph(string html)
    {
        var m = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(m.Success, "no JSON-LD");
        return JsonDocument.Parse(m.Groups[1].Value).RootElement.Clone().GetProperty("@graph");
    }

    private async Task<string> ThemeCssAsync(string pagePath)
    {
        var html = await f.NewClient().GetStringAsync(pagePath);
        var href = Regex.Match(html, "href=\"(/forum-theme\\.css\\?v=[0-9a-f]{16})\"").Groups[1].Value;
        Assert.NotEmpty(href);
        return await f.NewClient().GetStringAsync(href);
    }

    private async Task<HttpResponseMessage> PostColorsAsync(HttpClient c, params (string, string)[] fields)
    {
        var token = await c.GetTokenAsync("/Admin/ForumIdentity");
        return await c.PostAsync("/Admin/ForumIdentity/Colors", Form(token, fields));
    }

    private async Task<HttpResponseMessage> UploadAsync(HttpClient c, string slot, (string, byte[], string)? file, params (string, string)[] extra)
    {
        var token = await c.GetTokenAsync("/Admin/ForumIdentity");
        return await c.PostAsync("/Admin/ForumIdentity/Image", MediaUploadTests.Multipart(token, file, [("Key", slot), .. extra]));
    }

    [Fact] // the class, the stylesheet and the sub-brand: on every forum page (ar, en), on no other page
    public async Task Theme_is_on_forum_pages_only()
    {
        await ResetAsync();
        var forum = BothLanguages(ForumPaths).ToList();
        foreach (var path in forum)
        {
            var html = await f.NewClient().GetStringAsync(path);
            Assert.Contains("<body class=\"has-solid-header theme-forum\">", html);
            Assert.Contains(ThemeLink, Head(html));
            Assert.Contains("class=\"forum-subbrand\"", Header(html));
            Assert.Contains("class=\"logo-area\"", Header(html)); // the company logo (and its home link) stays
        }
        var others = BothLanguages(OtherPaths).ToList();
        foreach (var path in others)
        {
            var html = await f.NewClient().GetAsync(path) is var r ? await r.Content.ReadAsStringAsync() : "";
            Assert.DoesNotContain("theme-forum", html);
            Assert.DoesNotContain("forum-theme.css", html);
            Assert.DoesNotContain("forum-subbrand", html);
        }
        Assert.Equal(10, forum.Count);
        Assert.Equal(24, others.Count);
    }

    [Fact] // event and speaker detail pages are forum pages too; their organizer is the forum
    public async Task Forum_detail_pages_are_themed_and_carry_the_forum_node()
    {
        await ResetAsync();
        var ev = new Event
        {
            Slug = Unique("f9ev").ToLowerInvariant(), TitleAr = "فعالية", TitleEn = "Event", SummaryEn = "Summary", DescriptionAr = "وصف",
            StartsAt = new DateTimeOffset(2031, 3, 1, 10, 0, 0, TimeSpan.FromHours(3)), EventDay = 1, EventMonthAr = "مارس",
            LocationAr = "قاعة", LocationEn = "Hall", RegistrationOpen = true, IsActive = true, SortOrder = 950,
        };
        var sp = new Speaker
        {
            Slug = Unique("f9sp").ToLowerInvariant(), NameAr = "متحدث", NameEn = "Speaker", JobTitleAr = "مدير", JobTitleEn = "Director",
            BioAr = "سيرة", BioEn = "Bio", IsActive = true,
        };
        await f.DbAsync(db => { db.AddRange(ev, sp); return db.SaveChangesAsync(); });
        try
        {
            foreach (var (path, english) in new[]
                     {
                         ("/forum/events/" + ev.Slug, false), ("/en/forum/events/" + ev.Slug, true),
                         ("/forum/speakers/" + sp.Slug, false), ("/en/forum/speakers/" + sp.Slug, true),
                     })
            {
                var html = await f.NewClient().GetStringAsync(path);
                Assert.Contains("theme-forum", html);
                Assert.Contains(ThemeLink, html);
                var forumId = Base + (english ? "/en/forum" : "/forum") + "#organization";
                var org = Graph(html).EnumerateArray().Single(n => n.TryGetProperty("@id", out var id) && id.GetString() == forumId);
                Assert.Equal(Base + "/#organization", org.GetProperty("parentOrganization").GetProperty("@id").GetString());
                Assert.Equal(Base + ForumAssets.LogoOnLight, org.GetProperty("logo").GetString());
                if (path.Contains("/events/"))
                {
                    var node = Graph(html).EnumerateArray().Single(n => n.GetProperty("@type").GetString() == "Event");
                    Assert.Equal(forumId, node.GetProperty("organizer").GetProperty("@id").GetString());
                }
            }
            // A news item stays the company's.
            var news = await f.NewClient().GetStringAsync("/news");
            Assert.DoesNotContain("forum#organization", news);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                db.Remove((await db.Events.FindAsync(ev.Id))!);
                db.Remove((await db.Speakers.FindAsync(sp.Id))!);
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact] // the endpoint: GET/HEAD, text/css utf-8, immutable, nosniff; no value set = the static rules only; POST refused
    public async Task Stylesheet_endpoint_headers_and_defaults()
    {
        await ResetAsync();
        var r = await f.NewClient().GetAsync("/forum-theme.css");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/css; charset=utf-8", r.Content.Headers.ContentType!.ToString());
        Assert.Equal("public, max-age=31536000, immutable", r.Headers.CacheControl!.ToString());
        Assert.Equal("nosniff", string.Join(",", r.Headers.GetValues("X-Content-Type-Options")));
        var css = await r.Content.ReadAsStringAsync();
        var file = await File.ReadAllTextAsync(Path.Combine(ForumIdentityUnitTests.ProjectDir(), "wwwroot", "css", "forum.css"));
        Assert.Equal(file, css); // nothing generated
        Assert.Equal(css, await f.NewClient().GetStringAsync("/forum-theme.css?v=anything&lang=en")); // the query changes nothing

        var head = await f.NewClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, "/forum-theme.css"));
        Assert.Equal(HttpStatusCode.OK, head.StatusCode);
        var post = await f.NewClient().PostAsync("/forum-theme.css", Form());
        Assert.NotEqual(HttpStatusCode.OK, post.StatusCode);
    }

    [Fact] // admin: anonymous → login; no token → 400; nothing written in either case
    public async Task Admin_screen_requires_admin_and_token()
    {
        await ResetAsync();
        var anon = await f.NewClient().GetAsync("/Admin/ForumIdentity");
        Assert.Equal(HttpStatusCode.Redirect, anon.StatusCode);
        Assert.Contains("/Account/Login", anon.Headers.Location!.OriginalString);
        var anonPost = await f.NewClient().PostAsync("/Admin/ForumIdentity/Colors", Form((("Colors[" + ForumIdentity.Primary + "]"), "#000000")));
        Assert.True(anonPost.StatusCode == HttpStatusCode.BadRequest
                    || (anonPost.StatusCode == HttpStatusCode.Redirect && anonPost.Headers.Location!.OriginalString.Contains("/Account/Login")),
            anonPost.StatusCode.ToString());

        var c = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Admin/ForumIdentity")).StatusCode);
        foreach (var action in new[] { "Colors", "Reset", "Image" })
        {
            var r = await c.PostAsync("/Admin/ForumIdentity/" + action, Form((("Colors[" + ForumIdentity.Primary + "]"), "#000000"), ("Key", ForumIdentity.LogoOnDark)));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
        Assert.Null(await StoredAsync(ForumIdentity.Primary));
    }

    [Theory] // refused values: the screen comes back (200) with the reason, nothing is written
    [InlineData(ForumIdentity.Primary, "#fff", "#RRGGBB")]
    [InlineData(ForumIdentity.Primary, "red", "#RRGGBB")]
    [InlineData(ForumIdentity.Gold, "#123456;}body{", "#RRGGBB")]
    [InlineData(ForumIdentity.Primary, "#bd932e", "حد التباين")]   // white text on gold
    [InlineData(ForumIdentity.OnPrimary, "#ff8a95", "حد التباين")] // pink text on red
    [InlineData(ForumIdentity.Text, "#f1e8d2", "حد التباين")]      // cream text on white
    public async Task Refused_colours_write_nothing(string key, string value, string message)
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var r = await PostColorsAsync(c, ($"Colors[{key}]", value), ($"Colors[{ForumIdentity.Ink}]", "#000000"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains(message, WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
        Assert.Null(await StoredAsync(key));
        Assert.Null(await StoredAsync(ForumIdentity.Ink)); // the valid field of the same form is not written either
    }

    [Fact] // an unknown font key is refused
    public async Task Unknown_font_is_refused()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var r = await PostColorsAsync(c, ("FontEn", "Comic Sans"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("اختر خطاً من القائمة", WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
        Assert.Null(await StoredAsync(ForumIdentity.FontEnKey));
    }

    [Fact] // a valid save: stored lower-case, rendered as variables in a NEW stylesheet URL; other pages untouched; reset
    public async Task Saved_colours_render_as_variables_and_reset_restores_defaults()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            var before = await f.NewClient().GetStringAsync("/forum");
            var home = await f.NewClient().GetStringAsync("/about");
            var r = await PostColorsAsync(c, ($"Colors[{ForumIdentity.Primary}]", "#E0182F"), ($"Colors[{ForumIdentity.Surface}]", ""),
                ("FontEn", "montserrat"), ("Colors[evil]", "#000000"));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal("#e0182f", await StoredAsync(ForumIdentity.Primary));
            Assert.Null(await StoredAsync(ForumIdentity.Surface)); // empty = default: no row
            Assert.Null(await StoredAsync("evil"));                 // an unknown posted key is ignored

            foreach (var path in new[] { "/forum", "/en/forum/events" })
            {
                var css = await ThemeCssAsync(path);
                Assert.Contains("body.theme-forum{--forum-primary:#e0182f;--forum-primary-rgb:224, 24, 47;}", css);
                Assert.Contains("html[lang=\"en\"] body.theme-forum{--font-family:'Montserrat', 'Cairo', system-ui, sans-serif;}", css);
            }
            var link = Regex.Match(before, "forum-theme\\.css\\?v=([0-9a-f]+)").Groups[1].Value;
            Assert.DoesNotContain(link, await f.NewClient().GetStringAsync("/forum")); // the version changed
            Assert.Equal(Regex.Replace(home, "value=\"[^\"]{40,}\"", ""), Regex.Replace(await f.NewClient().GetStringAsync("/about"), "value=\"[^\"]{40,}\"", ""));

            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/ForumIdentity/Reset", Form(await c.GetTokenAsync("/Admin/ForumIdentity")))).StatusCode);
            Assert.Null(await StoredAsync(ForumIdentity.Primary));
            Assert.Null(await StoredAsync(ForumIdentity.FontEnKey));
            Assert.DoesNotContain("body.theme-forum{", await ThemeCssAsync("/forum"));
        }
        finally
        {
            await ResetAsync();
        }
    }

    [Fact] // a bad row written behind the admin's back (SQL, migration) never reaches the CSS
    public async Task Bad_stored_values_are_not_rendered()
    {
        await ResetAsync();
        try
        {
            await SetRowAsync(ForumIdentity.Primary, "#fff;}body{background:url(https://evil.example/)}");
            await SetRowAsync(ForumIdentity.FontArKey, "x;}body{color:red}");
            await SetRowAsync(ForumIdentity.Gold, "#ABCDEF");
            f.Services.GetRequiredService<ISiteContentCache>().Invalidate();
            var css = await ThemeCssAsync("/forum");
            Assert.DoesNotContain("evil", css);
            Assert.DoesNotContain("color:red", css);
            Assert.Contains("body.theme-forum{--forum-gold:#abcdef;--forum-gold-rgb:171, 205, 239;}", css);
        }
        finally
        {
            await ResetAsync();
            f.Services.GetRequiredService<ISiteContentCache>().Invalidate();
        }
    }

    [Fact] // the forum logo replaces the bundled symbol + name on forum pages only; alt in the page language; removal restores
    public async Task Forum_logo_swap()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            var html = await f.NewClient().GetStringAsync("/forum");
            Assert.Matches("<img src=\"/assets/forum/forum-symbol-80h\\.png\\?v=[^\"]+\" alt=\"[^\"]+\" class=\"forum-symbol\" width=\"152\" height=\"80\">", Header(html));
            Assert.Contains("class=\"forum-wordmark\"", Header(html));

            var r = await UploadAsync(c, ForumIdentity.LogoOnDark, ("l.png", PngBuilder.Png(240, 64, 6), "image/png"),
                ("ImageAltAr", "شعار المنتدى"), ("ImageAltEn", "Forum logo"));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var key = await StoredAsync(ForumIdentity.LogoOnDark);
            Assert.Matches("^[0-9a-f]{32}$", key);
            foreach (var (path, alt) in new[] { ("/forum/partners", "شعار المنتدى"), ("/en/forum/partners", "Forum logo") })
            {
                var header = Header(await f.NewClient().GetStringAsync(path));
                Assert.Contains($"<img src=\"/media/{key}.png\" alt=\"{WebUtility.HtmlEncode(alt)}\"", header);
                Assert.Contains("width=\"240\" height=\"64\"", header);
                Assert.DoesNotContain("forum-symbol", header);
            }
            Assert.DoesNotContain(key!, await f.NewClient().GetStringAsync("/about"));

            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, ForumIdentity.LogoOnDark, null, ("RemoveImage", "true"))).StatusCode);
            Assert.Null(await StoredAsync(ForumIdentity.LogoOnDark));
            Assert.Contains("forum-symbol", Header(await f.NewClient().GetStringAsync("/forum")));
        }
        finally
        {
            await ResetAsync();
        }
    }

    [Theory] // slot rules: no SVG, the formats and sizes of each slot; refused = nothing stored
    [InlineData(ForumIdentity.LogoOnDark, "x.svg", "image/svg+xml", "svg")]
    [InlineData(ForumIdentity.LogoOnDark, "x.jpg", "image/jpeg", "jpg")]
    [InlineData(ForumIdentity.LogoOnDark, "l.png", "image/png", "tiny")]
    [InlineData(ForumIdentity.LogoOnLight, "l.png", "image/png", "short")]
    [InlineData(ForumIdentity.OgDefault, "o.png", "image/png", "small-og")]
    public async Task Refused_forum_images_store_nothing(string slot, string name, string type, string kind)
    {
        await ResetAsync();
        var data = kind switch
        {
            "svg" => ImageInspectorTests.Fixture("image.svg"),
            "jpg" => ImageInspectorTests.Fixture("og-1200x630.jpg"),
            "tiny" => PngBuilder.Png(60, 20),
            "short" => PngBuilder.Png(400, 80),
            _ => PngBuilder.Png(600, 315),
        };
        var c = await f.SignedInClientAsync();
        var files = await f.DbAsync(db => db.MediaFiles.CountAsync());
        var r = await UploadAsync(c, slot, (name, data, type));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("field-error", await r.Content.ReadAsStringAsync());
        Assert.Null(await StoredAsync(slot));
        Assert.Equal(files, await f.DbAsync(db => db.MediaFiles.CountAsync()));
    }

    [Fact] // a forum key is refused by the Brand and Content editors, a brand key by this screen
    public async Task Slots_stay_on_their_own_screen()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var token = await c.GetTokenAsync("/Admin/Brand");
        await c.PostAsync("/Admin/Brand/Image", MediaUploadTests.Multipart(token, ("l.png", PngBuilder.Png(240, 64), "image/png"), ("Key", ForumIdentity.LogoOnDark)));
        token = await c.GetTokenAsync("/Admin/Content/Edit");
        await c.PostAsync("/Admin/Content/SiteImage", MediaUploadTests.Multipart(token, ("l.png", PngBuilder.Png(240, 64), "image/png"), ("Key", ForumIdentity.LogoOnDark)));
        Assert.Null(await StoredAsync(ForumIdentity.LogoOnDark));
        await UploadAsync(c, BrandImages.Favicon, ("f.png", PngBuilder.Png(96, 96), "image/png"));
        Assert.Null(await StoredAsync(BrandImages.Favicon));
    }

    [Fact] // light logo → forum JSON-LD logo; hero → CSS background under the overlay; share image → og:image on forum pages only
    public async Task Light_logo_hero_and_share_image()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, ForumIdentity.LogoOnLight, ("l.png", PngBuilder.Png(200, 200), "image/png"))).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, ForumIdentity.HeroImage, ("h.jpg", ImageInspectorTests.Fixture("og-1200x630.jpg"), "image/jpeg"))).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, ForumIdentity.OgDefault, ("o.jpg", ImageInspectorTests.Fixture("og-1200x630.jpg"), "image/jpeg"),
                ("ImageAltAr", ""), ("ImageAltEn", "Forum share"))).StatusCode);
            var light = await StoredAsync(ForumIdentity.LogoOnLight);
            var hero = await StoredAsync(ForumIdentity.HeroImage);
            var og = await StoredAsync(ForumIdentity.OgDefault);

            var html = await f.NewClient().GetStringAsync("/forum/speakers");
            var org = Graph(html).EnumerateArray().Single(n => n.TryGetProperty("@id", out var id) && id.GetString() == Base + "/forum#organization");
            Assert.Equal($"{Base}/media/{light}.png", org.GetProperty("logo").GetString());
            Assert.Contains($"<meta property=\"og:image\" content=\"{Base}/media/{og}.jpg\">", html);
            Assert.Contains("<meta property=\"og:image:alt\" content=\"", html);
            Assert.Contains($"<meta property=\"og:image:alt\" content=\"Forum share\">", await f.NewClient().GetStringAsync("/en/forum/speakers"));
            Assert.Contains($"url(\"/media/{hero}.jpg\")", await ThemeCssAsync("/forum"));

            // Not on the company's pages.
            var about = await f.NewClient().GetStringAsync("/about");
            Assert.DoesNotContain(og!, about);
            Assert.Contains($"<meta property=\"og:image\" content=\"{Base}/assets/brand/og-default.jpg\">", about);
        }
        finally
        {
            await ResetAsync();
        }
    }
}
