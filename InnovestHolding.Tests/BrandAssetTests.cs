using System.Buffers.Binary;
using System.Net;
using System.Text;
using InnovestHolding.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>Builds header-valid PNGs of any size and colour type (the inspector never decodes pixels).</summary>
internal static class PngBuilder
{
    private static uint Crc(ReadOnlySpan<byte> data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in data) { c ^= b; for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return c ^ 0xFFFFFFFFu;
    }

    private static void Chunk(MemoryStream s, string type, byte[] data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        s.Write(len);
        var body = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(body);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(body));
        s.Write(crc);
    }

    /// <param name="colourType">2 = RGB, 3 = palette, 6 = RGBA, 4 = grey + alpha.</param>
    public static byte[] Png(int width, int height, byte colourType = 2, bool trns = false)
    {
        using var s = new MemoryStream();
        s.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)height);
        ihdr[8] = 8;
        ihdr[9] = colourType;
        Chunk(s, "IHDR", ihdr);
        if (colourType == 3) Chunk(s, "PLTE", [0, 0, 0]);
        if (trns) Chunk(s, "tRNS", [0]);
        Chunk(s, "IDAT", [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);
        Chunk(s, "IEND", []);
        return s.ToArray();
    }
}

/// <summary>The slot rules on their own (BrandImages.Check).</summary>
public class BrandImageRuleTests
{
    private static readonly BrandImage Favicon = BrandImages.Find(BrandImages.Favicon)!;
    private static readonly BrandImage Apple = BrandImages.Find(BrandImages.AppleTouchIcon)!;
    private static readonly BrandImage Og = BrandImages.Find(BrandImages.OgDefault)!;

    private static string? Png(BrandImage slot, byte[] data) => BrandImages.Check(slot, data, "x.png", "image/png");

    [Theory]
    [InlineData(32, 32, null)]
    [InlineData(96, 96, null)]
    [InlineData(512, 512, null)]
    [InlineData(64, 32, "غير مربعة")]
    [InlineData(16, 16, "صغيرة جداً")]
    [InlineData(31, 31, "صغيرة جداً")]
    [InlineData(513, 513, "كبيرة جداً")]
    public void Favicon_is_a_square_png_from_32_to_512(int w, int h, string? error)
    {
        var result = Png(Favicon, PngBuilder.Png(w, h, 6)); // a favicon may be transparent
        if (error is null) Assert.Null(result);
        else Assert.Contains(error, result);
    }

    [Theory] // PNG only (CTO decision: no SVG, no ICO, no JPEG or WebP)
    [InlineData("x.jpg", "image/jpeg")]
    [InlineData("x.webp", "image/webp")]
    [InlineData("x.svg", "image/svg+xml")]
    [InlineData("x.ico", "image/x-icon")]
    [InlineData("x.png", "image/jpeg")]
    public void Favicon_refuses_other_formats(string name, string type) =>
        Assert.Contains("المسموح: PNG فقط", BrandImages.Check(Favicon, PngBuilder.Png(64, 64), name, type));

    [Fact] // a JPEG body named .png is refused by the slot (signature), not stored
    public void Favicon_refuses_a_jpeg_body_named_png() =>
        Assert.Contains("PNG فقط", Png(Favicon, ImageInspectorTests.Fixture("og-1200x630.jpg")));

    [Fact]
    public void Favicon_over_512_kb_is_refused()
    {
        var big = new byte[512 * 1024 + 1];
        PngBuilder.Png(64, 64).CopyTo(big, 0);
        Assert.Contains("512 KB", Png(Favicon, big));
    }

    [Theory] // exactly 180×180, and no transparency (rejected, never flattened: nothing is re-encoded)
    [InlineData(180, 180, 2, false, null)]
    [InlineData(180, 180, 3, false, null)]
    [InlineData(180, 180, 6, false, "شفافية")]
    [InlineData(180, 180, 4, false, "شفافية")]
    [InlineData(180, 180, 3, true, "شفافية")]
    [InlineData(180, 180, 2, true, "شفافية")]
    [InlineData(192, 192, 2, false, "180×180 بالضبط")]
    [InlineData(120, 120, 2, false, "180×180 بالضبط")]
    public void Apple_touch_icon_is_an_opaque_180_png(int w, int h, byte colourType, bool trns, string? error)
    {
        var result = Png(Apple, PngBuilder.Png(w, h, colourType, trns));
        if (error is null) Assert.Null(result);
        else Assert.Contains(error, result);
    }

    [Fact]
    public void Share_image_rules()
    {
        Assert.Null(BrandImages.Check(Og, ImageInspectorTests.Fixture("og-1200x630.jpg"), "og.jpg", "image/jpeg"));
        Assert.Null(Png(Og, PngBuilder.Png(2400, 1260, 6)));
        Assert.Contains("الحد الأدنى 1200×630", Png(Og, PngBuilder.Png(300, 300)));
        Assert.Contains("الحد الأدنى 1200×630", Png(Og, PngBuilder.Png(1200, 600)));
        Assert.Contains("JPG أو PNG", BrandImages.Check(Og, ImageInspectorTests.Fixture("lossy.webp"), "x.webp", "image/webp"));
        // Advice (non-blocking): ratio and weight.
        Assert.Null(BrandImages.Advice(Og, 1200, 630, 100_000));
        Assert.Contains("1.91:1", BrandImages.Advice(Og, 1200, 1200, 100_000));
        Assert.Contains("600 KB", BrandImages.Advice(Og, 1200, 630, 700 * 1024));
    }

    [Theory]
    [InlineData("#ff0000", "#ff0000")]
    [InlineData("#AABBCC", "#aabbcc")]
    [InlineData("red", "#111145")]
    [InlineData("#fff", "#111145")]
    [InlineData("#11114g", "#111145")]
    [InlineData("\"><script>", "#111145")]
    [InlineData(null, "#111145")]
    public void Theme_color_is_hex_or_default(string? stored, string expected) =>
        Assert.Equal(expected, BrandImages.ThemeColor(stored));
}

/// <summary>The Brand screen end to end: uploads, the &lt;head&gt; of public pages and the login page, defaults.</summary>
[Collection("App")]
public class BrandAssetTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com";

    private const string DefaultIco = "<link rel=\"icon\" href=\"/favicon.ico\" sizes=\"any\">";

    private Task<int> ResetAsync() => f.DbAsync(async db =>
    {
        var keys = BrandImages.All.Select(s => s.Key).ToList();
        var rows = await db.SiteContents.Where(c => keys.Contains(c.SectionKey)).ToListAsync();
        var fileKeys = rows.Select(r => r.ContentAr).ToList();
        db.MediaFiles.RemoveRange(db.MediaFiles.Where(m => fileKeys.Contains(m.FileKey)));
        db.SiteContents.RemoveRange(rows);
        // The theme colour row (if any, e.g. written by a migration) goes back to the default rather than away.
        foreach (var theme in db.SiteContents.Where(c => c.SectionKey == BrandImages.ThemeColorKey))
            theme.ContentAr = BrandImages.DefaultThemeColor;
        return await db.SaveChangesAsync();
    });

    private Task<string?> StoredKeyAsync(string slot) =>
        f.DbAsync(db => db.SiteContents.Where(c => c.SectionKey == slot).Select(c => (string?)c.ContentAr).SingleOrDefaultAsync());

    private async Task<HttpResponseMessage> UploadAsync(HttpClient c, string slot, (string, byte[], string)? file, params (string, string)[] extra)
    {
        var token = await c.GetTokenAsync("/Admin/Brand");
        return await c.PostAsync("/Admin/Brand/Image", MediaUploadTests.Multipart(token, file, [("Key", slot), .. extra]));
    }

    private static void AssertDefaultHead(string html)
    {
        Assert.Contains("<meta name=\"theme-color\" content=\"#111145\">", html);
        Assert.Contains(DefaultIco, html);
        Assert.Matches("<link rel=\"icon\" href=\"/assets/brand/favicon.svg\\?v=[^\"]+\" type=\"image/svg\\+xml\">", html);
        Assert.Matches("<link rel=\"apple-touch-icon\" href=\"/assets/brand/apple-touch-icon.png\\?v=[^\"]+\">", html);
        Assert.DoesNotContain("/media/", Head(html)); // no uploaded file in the head
    }

    private static string Head(string html) => html[..html.IndexOf("</head>", StringComparison.Ordinal)];

    [Fact] // nothing uploaded: the head is exactly the bundled set, on public pages (ar, en) and the login page
    public async Task Defaults_are_unchanged_with_nothing_uploaded()
    {
        await ResetAsync();
        foreach (var path in new[] { "/", "/en", "/about", "/en/about" })
        {
            var html = await f.NewClient().GetStringAsync(path);
            AssertDefaultHead(html);
            Assert.Contains($"<meta property=\"og:image\" content=\"{Base}/assets/brand/og-default.jpg\">", html);
            Assert.Contains("<meta property=\"og:image:width\" content=\"1200\">", html);
        }
        var login = await f.NewClient().GetStringAsync("/Account/Login");
        AssertDefaultHead(login);
        Assert.Matches("<img src=\"/assets/brand/logo-on-dark.png\\?v=[^\"]+\" alt=\"[^\"]+\">", login);
        Assert.Contains("<title>تسجيل الدخول - ", login);
        Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync("/favicon.ico")).StatusCode);
    }

    [Fact] // upload, replace (new URL), remove (bundled back) — the favicon link of every page follows
    public async Task Favicon_upload_replace_and_remove()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, BrandImages.Favicon, ("f.png", PngBuilder.Png(96, 96, 6), "image/png"))).StatusCode);
            var first = await StoredKeyAsync(BrandImages.Favicon);
            Assert.Matches("^[0-9a-f]{32}$", first);
            foreach (var path in new[] { "/", "/en", "/Account/Login" })
            {
                var html = await f.NewClient().GetStringAsync(path);
                Assert.Contains($"<link rel=\"icon\" href=\"/media/{first}.png\" type=\"image/png\" sizes=\"96x96\">", html);
                Assert.Contains(DefaultIco, html); // /favicon.ico stays linked
                Assert.DoesNotContain("favicon.svg", html);
            }
            Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync($"/media/{first}.png")).StatusCode);

            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, BrandImages.Favicon, ("g.png", PngBuilder.Png(192, 192), "image/png"))).StatusCode);
            var second = await StoredKeyAsync(BrandImages.Favicon);
            Assert.NotEqual(first, second);
            var after = await f.NewClient().GetStringAsync("/en");
            Assert.Contains($"href=\"/media/{second}.png\" type=\"image/png\" sizes=\"192x192\"", after);
            Assert.DoesNotContain(first!, after);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{first}.png")).StatusCode); // replaced file deleted

            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, BrandImages.Favicon, null, ("RemoveImage", "true"))).StatusCode);
            Assert.Null(await StoredKeyAsync(BrandImages.Favicon));
            AssertDefaultHead(await f.NewClient().GetStringAsync("/"));
        }
        finally
        {
            await ResetAsync();
        }
    }

    [Theory] // refused files: the screen comes back (200) with the reason, nothing is stored (no row, no file)
    [InlineData(BrandImages.Favicon, "f.png", "image/png", 64, 32, (byte)2, "غير مربعة")]
    [InlineData(BrandImages.Favicon, "f.png", "image/png", 16, 16, (byte)2, "صغيرة جداً")]
    [InlineData(BrandImages.Favicon, "f.jpg", "image/jpeg", 64, 64, (byte)2, "PNG فقط")]
    [InlineData(BrandImages.AppleTouchIcon, "a.png", "image/png", 180, 180, (byte)6, "شفافية")]
    [InlineData(BrandImages.AppleTouchIcon, "a.png", "image/png", 192, 192, (byte)2, "180×180 بالضبط")]
    [InlineData(BrandImages.OgDefault, "o.png", "image/png", 300, 300, (byte)2, "الحد الأدنى 1200×630")]
    public async Task Refused_uploads_store_nothing(string slot, string name, string type, int w, int h, byte colourType, string message)
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var rowsBefore = await f.DbAsync(db => db.MediaFiles.CountAsync());
        var r = await UploadAsync(c, slot, (name, PngBuilder.Png(w, h, colourType), type));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains(message, WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
        Assert.Equal(rowsBefore, await f.DbAsync(db => db.MediaFiles.CountAsync()));
        Assert.Null(await StoredKeyAsync(slot));
        AssertDefaultHead(await f.NewClient().GetStringAsync("/"));
    }

    [Fact] // an SVG favicon is refused (CTO decision: PNG only, nothing to sanitise)
    public async Task Svg_favicon_is_refused()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var r = await UploadAsync(c, BrandImages.Favicon, ("x.svg", ImageInspectorTests.Fixture("image.svg"), "image/svg+xml"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("PNG فقط", WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
        Assert.Null(await StoredKeyAsync(BrandImages.Favicon));
    }

    [Fact] // an opaque 180×180 PNG becomes the apple-touch icon; a replacement changes its URL
    public async Task Apple_touch_icon_upload_and_replace()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, BrandImages.AppleTouchIcon, ("a.png", PngBuilder.Png(180, 180), "image/png"))).StatusCode);
            var first = await StoredKeyAsync(BrandImages.AppleTouchIcon);
            Assert.Contains($"<link rel=\"apple-touch-icon\" href=\"/media/{first}.png\">", await f.NewClient().GetStringAsync("/"));
            Assert.Equal(HttpStatusCode.Redirect, (await UploadAsync(c, BrandImages.AppleTouchIcon, ("b.png", PngBuilder.Png(180, 180, 3), "image/png"))).StatusCode);
            var second = await StoredKeyAsync(BrandImages.AppleTouchIcon);
            Assert.NotEqual(first, second);
            var en = await f.NewClient().GetStringAsync("/en");
            Assert.Contains($"<link rel=\"apple-touch-icon\" href=\"/media/{second}.png\">", en);
            Assert.DoesNotContain("apple-touch-icon.png", en);
        }
        finally
        {
            await ResetAsync();
        }
    }

    [Fact] // the uploaded share image: absolute og:image with its size on pages without their own image, and the
           // JSON-LD image of an article without one; a replacement changes the URL
    public async Task Default_share_image_upload_and_replace()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        try
        {
            var r = await UploadAsync(c, BrandImages.OgDefault, ("og.jpg", ImageInspectorTests.Fixture("og-1200x630.jpg"), "image/jpeg"),
                ("ImageAltAr", "صورة المشاركة"), ("ImageAltEn", "Share picture"));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var first = await StoredKeyAsync(BrandImages.OgDefault);
            var url = $"{Base}/media/{first}.jpg";
            foreach (var (path, alt) in new[] { ("/", "صورة المشاركة"), ("/en", "Share picture"), ("/about", "صورة المشاركة"), ("/en/about", "Share picture") })
            {
                var html = await f.NewClient().GetStringAsync(path);
                Assert.Contains($"<meta property=\"og:image\" content=\"{url}\">", html);
                Assert.Contains("<meta property=\"og:image:width\" content=\"1200\">", html);
                Assert.Contains("<meta property=\"og:image:height\" content=\"630\">", html);
                Assert.Contains($"<meta property=\"og:image:alt\" content=\"{alt}\">", html);
                Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\">", html);
                Assert.DoesNotContain("og-default.jpg", html);
            }
            Assert.Null(await f.DbAsync(db => db.News.Where(n => n.Slug == "news-1").Select(n => n.ImageId).SingleAsync()));
            Assert.Contains($"\"image\":[\"{url}\"]", await f.NewClient().GetStringAsync("/news/news-1"));

            r = await UploadAsync(c, BrandImages.OgDefault, ("og2.png", PngBuilder.Png(1200, 630), "image/png"));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var second = await StoredKeyAsync(BrandImages.OgDefault);
            Assert.NotEqual(first, second);
            var after = await f.NewClient().GetStringAsync("/en");
            Assert.Contains($"<meta property=\"og:image\" content=\"{Base}/media/{second}.png\">", after);
            Assert.DoesNotContain(first!, after);
        }
        finally
        {
            await ResetAsync();
        }
        Assert.Contains($"<meta property=\"og:image\" content=\"{Base}/assets/brand/og-default.jpg\">", await f.NewClient().GetStringAsync("/"));
    }

    [Fact] // theme colour: a valid hex is used; anything else is refused and the default stays
    public async Task Theme_color_is_validated()
    {
        await ResetAsync();
        var existed = await f.DbAsync(db => db.SiteContents.AnyAsync(x => x.SectionKey == BrandImages.ThemeColorKey));
        var c = await f.SignedInClientAsync();
        try
        {
            var token = await c.GetTokenAsync("/Admin/Brand");
            var bad = await c.PostAsync("/Admin/Brand/ThemeColor", Form(token, ("ThemeColor", "red")));
            Assert.Equal(HttpStatusCode.OK, bad.StatusCode);
            Assert.Contains("اللون غير صالح", WebUtility.HtmlDecode(await bad.Content.ReadAsStringAsync()));
            Assert.Contains("<meta name=\"theme-color\" content=\"#111145\">", await f.NewClient().GetStringAsync("/"));

            token = await c.GetTokenAsync("/Admin/Brand");
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/Brand/ThemeColor", Form(token, ("ThemeColor", "#FF6600")))).StatusCode);
            Assert.Contains("<meta name=\"theme-color\" content=\"#ff6600\">", await f.NewClient().GetStringAsync("/en"));
            Assert.Contains("<meta name=\"theme-color\" content=\"#ff6600\">", await f.NewClient().GetStringAsync("/Account/Login"));

            token = await c.GetTokenAsync("/Admin/Brand");
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/Brand/ThemeColor", Form(token, ("ThemeColor", "")))).StatusCode);
            Assert.Contains("<meta name=\"theme-color\" content=\"#111145\">", await f.NewClient().GetStringAsync("/"));
        }
        finally
        {
            await ResetAsync();
            // Leave the shared database as it was (the migration comparison reads it): no row if there was none.
            if (!existed)
                await f.DbAsync(async db => { db.SiteContents.RemoveRange(db.SiteContents.Where(x => x.SectionKey == BrandImages.ThemeColorKey)); return await db.SaveChangesAsync(); });
        }
    }

    [Fact] // the Content editor's image form cannot bypass the brand rules: brand keys are refused there and not listed
    public async Task Content_editor_does_not_take_brand_keys()
    {
        await ResetAsync();
        var c = await f.SignedInClientAsync();
        var page = await c.GetStringAsync("/Admin/Content/Edit");
        Assert.DoesNotContain("image-" + BrandImages.Favicon, page);
        Assert.Contains("image-logo_on_dark", page);
        var token = ExtractToken(page);
        var r = await c.PostAsync("/Admin/Content/SiteImage",
            MediaUploadTests.Multipart(token, ("f.png", PngBuilder.Png(64, 32, 6), "image/png"), ("Key", BrandImages.Favicon)));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Null(await StoredKeyAsync(BrandImages.Favicon));
    }

    [Fact] // admin only, antiforgery required, unknown slot refused
    public async Task Brand_screen_is_admin_only_and_needs_the_token()
    {
        await ResetAsync();
        var anon = f.NewClient();
        var get = await anon.GetAsync("/Admin/Brand");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Contains("/Account/Login", get.Headers.Location!.OriginalString);
        var token = ExtractToken(await anon.GetStringAsync("/Account/Login"));
        var post = await anon.PostAsync("/Admin/Brand/Image", MediaUploadTests.Multipart(token, ("f.png", PngBuilder.Png(64, 64), "image/png"), ("Key", BrandImages.Favicon)));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Contains("/Account/Login", post.Headers.Location!.OriginalString);
        var color = await anon.PostAsync("/Admin/Brand/ThemeColor", Form(token, ("ThemeColor", "#000000")));
        Assert.Equal(HttpStatusCode.Redirect, color.StatusCode);

        var c = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Admin/Brand")).StatusCode);
        var form = new MultipartFormDataContent { { new StringContent(BrandImages.Favicon), "Key" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/Admin/Brand/Image", form)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/Admin/Brand/ThemeColor", Form(("ThemeColor", "#000000")))).StatusCode);

        // A key that is not a brand slot (e.g. a text key) is refused; nothing is written to it.
        var before = await f.DbAsync(db => db.SiteContents.Where(x => x.SectionKey == "site_name").Select(x => x.ContentAr).SingleAsync());
        var r = await UploadAsync(c, "site_name", ("f.png", PngBuilder.Png(64, 64), "image/png"));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Equal(before, await f.DbAsync(db => db.SiteContents.Where(x => x.SectionKey == "site_name").Select(x => x.ContentAr).SingleAsync()));
        Assert.Null(await StoredKeyAsync(BrandImages.Favicon));
    }
}
