using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The image inspector on its own: what is accepted, what is refused, what the stored file still contains.</summary>
public class ImageInspectorTests
{
    internal static byte[] Fixture(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "media", name));

    [Theory]
    [InlineData("photo-exif.jpg", ImageFormat.Jpeg, 64, 48)]
    [InlineData("og-1200x630.jpg", ImageFormat.Jpeg, 1200, 630)]
    [InlineData("text-chunk.png", ImageFormat.Png, 40, 30)]
    [InlineData("lossy.webp", ImageFormat.WebP, 32, 32)]
    [InlineData("lossless.webp", ImageFormat.WebP, 33, 17)]
    [InlineData("png-zip-polyglot.png", ImageFormat.Png, 24, 24)]
    public void Accepted_images_report_their_header_dimensions(string file, ImageFormat format, int width, int height)
    {
        Assert.Equal(ImageProblem.None, ImageInspector.Inspect(Fixture(file), out var info));
        Assert.Equal(new ImageInfo(format, width, height), info);
        // The rebuilt file passes the same inspection with the same result.
        var clean = ImageInspector.Sanitize(Fixture(file), format);
        Assert.Equal(ImageProblem.None, ImageInspector.Inspect(clean, out var again));
        Assert.Equal(info, again);
    }

    [Theory]
    [InlineData("image.gif", ImageProblem.Type)]
    [InlineData("image.svg", ImageProblem.Type)]
    [InlineData("page.html", ImageProblem.Type)]
    [InlineData("animated.webp", ImageProblem.Animated)]
    [InlineData("animated.png", ImageProblem.Animated)]
    [InlineData("huge-header.png", ImageProblem.Dimensions)]
    [InlineData("bad-crc.png", ImageProblem.Corrupt)]
    public void Refused_files(string file, ImageProblem expected) =>
        Assert.Equal(expected, ImageInspector.Inspect(Fixture(file), out _));

    [Fact]
    public void A_truncated_image_is_corrupt()
    {
        foreach (var file in new[] { "photo-exif.jpg", "text-chunk.png", "lossy.webp" })
        {
            var data = Fixture(file);
            Assert.Equal(ImageProblem.Corrupt, ImageInspector.Inspect(data.AsSpan(0, data.Length - 7), out _));
        }
    }

    [Fact] // EXIF (GPS, camera) and comments leave; the orientation stays so the photo is still upright
    public void Jpeg_metadata_is_stripped_and_the_orientation_kept()
    {
        var original = Fixture("photo-exif.jpg");
        Assert.Contains("TestCam", Encoding.Latin1.GetString(original));
        Assert.Contains("secret comment", Encoding.Latin1.GetString(original));

        var clean = ImageInspector.Sanitize(original, ImageFormat.Jpeg);
        var text = Encoding.Latin1.GetString(clean);
        Assert.DoesNotContain("TestCam", text);
        Assert.DoesNotContain("secret comment", text);
        var app1 = text.IndexOf("Exif\0\0", StringComparison.Ordinal);
        Assert.True(app1 > 0, "orientation block missing");
        Assert.Equal(6, ImageInspector.ExifOrientation(clean.AsSpan(app1)));
        Assert.True(clean.Length < original.Length);
    }

    [Fact] // text chunks and bytes after the image's end (an appended archive) are not kept
    public void Png_text_chunks_and_trailing_bytes_are_dropped()
    {
        var text = Encoding.Latin1.GetString(ImageInspector.Sanitize(Fixture("text-chunk.png"), ImageFormat.Png));
        Assert.DoesNotContain("<script>", text);
        Assert.DoesNotContain("tEXt", text);

        var polyglot = Fixture("png-zip-polyglot.png");
        Assert.Contains("PK\u0003\u0004", Encoding.Latin1.GetString(polyglot));
        var clean = ImageInspector.Sanitize(polyglot, ImageFormat.Png);
        Assert.DoesNotContain("PK\u0003\u0004", Encoding.Latin1.GetString(clean));
        Assert.Equal("IEND", Encoding.Latin1.GetString(clean, clean.Length - 8, 4));
    }

    [Theory]
    [InlineData("photo.JPG", "image/jpeg", ImageFormat.Jpeg)]
    [InlineData("photo.jpeg", "image/pjpeg", ImageFormat.Jpeg)]
    [InlineData("x.webp", "image/webp; charset=binary", ImageFormat.WebP)]
    public void Extension_and_declared_type_are_read_case_insensitively(string name, string type, ImageFormat format)
    {
        Assert.Equal(format, ImageInspector.FromExtension(name));
        Assert.Equal(format, ImageInspector.FromContentType(type));
    }

    [Theory]
    [InlineData("x.svg")]
    [InlineData("x.gif")]
    [InlineData("x.jpg.exe")]
    [InlineData("x")]
    public void Other_extensions_are_not_images(string name) => Assert.Null(ImageInspector.FromExtension(name));
}

/// <summary>Uploads through the admin forms, the /media endpoint, deletion and the janitor (one LocalDB fixture).</summary>
[Collection("App")]
public class MediaUploadTests(TestAppFactory f)
{
    private static ByteArrayContent FilePart(byte[] data, string contentType)
    {
        var part = new ByteArrayContent(data);
        part.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return part;
    }

    internal static MultipartFormDataContent Multipart(string token, (string Name, byte[] Data, string Type)? file, params (string Key, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        foreach (var (k, v) in fields) form.Add(new StringContent(v, Encoding.UTF8), k);
        form.Add(new StringContent(token), "__RequestVerificationToken");
        if (file is { } x) form.Add(FilePart(x.Data, x.Type), "Image", x.Name);
        return form;
    }

    private async Task<(HttpClient Client, string Token)> AdminAsync(string page)
    {
        var client = await f.SignedInClientAsync();
        return (client, await client.GetTokenAsync(page));
    }

    private static string[] StoredFiles() =>
        Directory.Exists(TestAppFactory.UploadsRoot)
            ? Directory.GetFiles(TestAppFactory.UploadsRoot, "*", SearchOption.AllDirectories)
            : [];

    private Task<HttpResponseMessage> CreateNewsAsync(HttpClient c, string token, string title, (string, byte[], string)? file, params (string, string)[] extra) =>
        c.PostAsync("/Admin/News/Create", Multipart(token, file, [("TitleAr", title), ("SummaryAr", "ملخص"), ("IsActive", "true"), .. extra]));

    [Fact] // a JPEG becomes a news image, served from /media with the hardened headers and without its metadata
    public async Task Upload_is_stored_under_a_random_key_and_served_hardened()
    {
        var (c, t) = await AdminAsync("/Admin/News/Create");
        var title = Unique("news-img");
        var response = await CreateNewsAsync(c, t, title, ("..\\..\\evil<>.jpg", ImageInspectorTests.Fixture("photo-exif.jpg"), "image/jpeg"),
            ("ImageAltAr", "وصف الصورة"), ("ImageAltEn", "Picture"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var row = await f.DbAsync(db => db.News.AsNoTracking().Include(n => n.Image).SingleAsync(n => n.TitleAr == title));
        var media = row.Image!;
        Assert.Matches("^[0-9a-f]{32}$", media.FileKey);
        Assert.Equal(("jpeg", 64, 48, "وصف الصورة", "Picture"), (media.Format, media.Width, media.Height, media.AltAr, media.AltEn));
        var path = Path.Combine(TestAppFactory.UploadsRoot, media.FileKey[..2], media.FileKey + ".jpg");
        Assert.True(File.Exists(path));
        Assert.DoesNotContain(StoredFiles(), p => p.Contains("evil", StringComparison.OrdinalIgnoreCase));

        var get = await f.NewClient().GetAsync("/media/" + media.FileKey + ".jpg");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("image/jpeg", get.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", get.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(MediaEndpoint.Csp, get.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal(MediaEndpoint.CacheControl, get.Headers.CacheControl!.ToString());
        Assert.False(get.Headers.Contains("Set-Cookie"));
        var bytes = await get.Content.ReadAsByteArrayAsync();
        Assert.Equal(media.Bytes, bytes.Length);
        Assert.DoesNotContain("TestCam", Encoding.Latin1.GetString(bytes));

        // The public detail page shows it with its size and alt text.
        var page = await f.NewClient().GetStringAsync("/news/" + row.Slug);
        Assert.Contains($"src=\"/media/{media.FileKey}.jpg\" width=\"64\" height=\"48\" alt=\"وصف الصورة\"", page);
    }

    public static TheoryData<string, string, string, string> RefusedUploads => new()
    {
        { "image.svg", "x.svg", "image/svg+xml", "JPG أو PNG أو WebP" },
        { "image.gif", "x.gif", "image/gif", "JPG أو PNG أو WebP" },
        { "page.html", "x.png", "image/png", "JPG أو PNG أو WebP" },
        { "text-chunk.png", "x.jpg", "image/jpeg", "غير متطابقة" },
        { "photo-exif.jpg", "x.jpg", "image/png", "غير متطابقة" },
        { "animated.webp", "x.webp", "image/webp", "المتحركة" },
        { "animated.png", "x.png", "image/png", "المتحركة" },
        { "huge-header.png", "x.png", "image/png", "8000" },
        { "bad-crc.png", "x.png", "image/png", "تالف" },
    };

    [Theory] // each refusal: the form comes back with the message, nothing is written (no row, no file)
    [MemberData(nameof(RefusedUploads))]
    public async Task Refused_uploads_write_nothing(string fixture, string fileName, string type, string message)
    {
        var (c, t) = await AdminAsync("/Admin/News/Create");
        var title = Unique("refused");
        var rowsBefore = await f.DbAsync(db => db.MediaFiles.CountAsync());
        var filesBefore = StoredFiles().Length;

        var response = await CreateNewsAsync(c, t, title, (fileName, ImageInspectorTests.Fixture(fixture), type));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(message, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.False(await f.DbAsync(db => db.News.AnyAsync(n => n.TitleAr == title)));
        Assert.Equal(rowsBefore, await f.DbAsync(db => db.MediaFiles.CountAsync()));
        Assert.Equal(filesBefore, StoredFiles().Length);
    }

    [Fact] // 5 MB + 1 byte is refused before it is read as an image
    public async Task Oversized_upload_is_refused()
    {
        var (c, t) = await AdminAsync("/Admin/News/Create");
        var big = new byte[MediaStore.MaxBytes + 1];
        ImageInspectorTests.Fixture("og-1200x630.jpg").CopyTo(big, 0);
        var title = Unique("big");
        var response = await CreateNewsAsync(c, t, title, ("big.jpg", big, "image/jpeg"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("5 ميغابايت", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.False(await f.DbAsync(db => db.News.AnyAsync(n => n.TitleAr == title)));
    }

    [Fact] // the upload endpoints are admin-only and need the antiforgery token
    public async Task Anonymous_and_tokenless_uploads_are_refused()
    {
        var anon = f.NewClient();
        var token = ExtractToken(await anon.GetStringAsync("/Account/Login"));
        var title = Unique("anon");
        var r1 = await anon.PostAsync("/Admin/News/Create", Multipart(token, ("a.jpg", ImageInspectorTests.Fixture("photo-exif.jpg"), "image/jpeg"), ("TitleAr", title)));
        Assert.Equal(HttpStatusCode.Redirect, r1.StatusCode);
        Assert.Contains("/Account/Login", r1.Headers.Location!.OriginalString);

        var (c, _) = await AdminAsync("/Admin/News/Create");
        var form = new MultipartFormDataContent { { new StringContent(title), "TitleAr" } };
        form.Add(FilePart(ImageInspectorTests.Fixture("photo-exif.jpg"), "image/jpeg"), "Image", "a.jpg");
        var r2 = await c.PostAsync("/Admin/News/Create", form);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.False(await f.DbAsync(db => db.News.AnyAsync(n => n.TitleAr == title)));
    }

    [Theory] // nothing but /media/{32 hex}.{jpg|png|webp} is served; no path reaches outside the folder
    [InlineData("/media/../appsettings.json")]
    [InlineData("/media/..%2F..%2Fappsettings.json")]
    [InlineData("/media/")]
    [InlineData("/media")]
    [InlineData("/media/0123456789abcdef0123456789abcdef.jpg.tmp")]
    [InlineData("/media/0123456789abcdef0123456789abcdef.svg")]
    [InlineData("/media/0123456789ABCDEF0123456789ABCDEF.jpg")]
    [InlineData("/media/0123456789abcdef0123456789abcdef.jpg")]
    [InlineData("/MEDIA/0123456789abcdef0123456789abcdef.jpg")]
    public async Task Media_paths_outside_the_pattern_are_404(string path)
    {
        var response = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // a stored file served under the extension of another format, or by POST, is refused
    public async Task Media_requires_the_stored_format_and_get()
    {
        var (c, t) = await AdminAsync("/Admin/News/Create");
        var title = Unique("fmt");
        await CreateNewsAsync(c, t, title, ("a.png", ImageInspectorTests.Fixture("text-chunk.png"), "image/png"));
        var key = await f.DbAsync(db => db.News.Where(n => n.TitleAr == title).Select(n => n.Image!.FileKey).SingleAsync());
        Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync($"/media/{key}.png")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{key}.jpg")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await f.NewClient().PostAsync($"/media/{key}.png", new StringContent(""))).StatusCode);
    }

    [Fact] // replacing an image deletes the old file; deleting the item deletes its image row and file
    public async Task Replaced_and_deleted_images_leave_the_disk()
    {
        var (c, t) = await AdminAsync("/Admin/News/Create");
        var title = Unique("del");
        await CreateNewsAsync(c, t, title, ("a.jpg", ImageInspectorTests.Fixture("photo-exif.jpg"), "image/jpeg"));
        var (id, first) = await f.DbAsync(db => db.News.Where(n => n.TitleAr == title).Select(n => new ValueTuple<int, string>(n.Id, n.Image!.FileKey)).SingleAsync());

        var token = await c.GetTokenAsync($"/Admin/News/Edit/{id}");
        var edit = await c.PostAsync("/Admin/News/Edit", Multipart(token, ("b.webp", ImageInspectorTests.Fixture("lossy.webp"), "image/webp"),
            ("Id", id.ToString()), ("TitleAr", title), ("IsActive", "true"), ("SortOrder", "0")));
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        var second = await f.DbAsync(db => db.News.Where(n => n.Id == id).Select(n => n.Image!.FileKey).SingleAsync());
        Assert.NotEqual(first, second);
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{first}.jpg")).StatusCode);
        Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.FileKey == first)));
        Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync($"/media/{second}.webp")).StatusCode);

        token = await c.GetTokenAsync("/Admin/News");
        var delete = await c.PostAsync("/Admin/News/Delete", Form(token, ("id", id.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{second}.webp")).StatusCode);
        Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.FileKey == second)));
        Assert.DoesNotContain(StoredFiles(), p => p.Contains(second, StringComparison.Ordinal));
    }

    [Fact] // "remove image" clears it (row and file); the item stays
    public async Task Remove_image_clears_it()
    {
        var (c, t) = await AdminAsync("/Admin/Events/Create");
        var title = Unique("ev-img");
        var create = await c.PostAsync("/Admin/Events/Create", Multipart(t, ("a.png", ImageInspectorTests.Fixture("text-chunk.png"), "image/png"),
            ("TitleAr", title), ("EventDay", "3"), ("EventMonthAr", "مايو"), ("IsActive", "true"), ("RegistrationOpen", "true")));
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        var (id, key) = await f.DbAsync(db => db.Events.Where(e => e.TitleAr == title).Select(e => new ValueTuple<int, string>(e.Id, e.Image!.FileKey)).SingleAsync());
        try
        {
            var token = await c.GetTokenAsync($"/Admin/Events/Edit/{id}");
            var edit = await c.PostAsync("/Admin/Events/Edit", Multipart(token, null,
                ("Id", id.ToString()), ("TitleAr", title), ("EventDay", "3"), ("EventMonthAr", "مايو"), ("IsActive", "true"),
                ("SortOrder", "1"), ("RemoveImage", "true")));
            Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
            Assert.Null(await f.DbAsync(db => db.Events.Where(e => e.Id == id).Select(e => e.ImageId).SingleAsync()));
            Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.FileKey == key)));
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{key}.png")).StatusCode);
        }
        finally
        {
            await f.DbAsync(async db => { db.Events.Remove(await db.Events.SingleAsync(e => e.Id == id)); return await db.SaveChangesAsync(); });
        }
    }

    [Fact] // a partner logo, its website and its group heading on the Forum Partners page; the home strip honours its switch
    public async Task Partner_logo_group_and_link_render()
    {
        var c = await f.SignedInClientAsync();
        var token = await c.GetTokenAsync("/Admin/Partners");
        var group = Unique("فئة");
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/PartnerGroups/Create", Form(token, ("NameAr", group), ("NameEn", "Gold")))).StatusCode);
        var groupId = await f.DbAsync(db => db.PartnerGroups.Where(g => g.NameAr == group).Select(g => g.Id).SingleAsync());
        var name = Unique("شريك");
        var create = await c.PostAsync("/Admin/Partners/Create", Multipart(token, ("logo.png", ImageInspectorTests.Fixture("text-chunk.png"), "image/png"),
            ("NameAr", name), ("NameEn", "Partner Co"), ("Url", "https://example.org/p"), ("PartnerGroupId", groupId.ToString()), ("ShowOnHome", "false")));
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        var (id, key) = await f.DbAsync(db => db.Partners.Where(p => p.NameAr == name).Select(p => new ValueTuple<int, string>(p.Id, p.Logo!.FileKey)).SingleAsync());
        try
        {
            var page = await f.NewClient().GetStringAsync("/en/forum/partners");
            Assert.Contains("<h3 class=\"partner-group-title\">Gold</h3>", page);
            Assert.Contains($"<a href=\"https://example.org/p\" rel=\"sponsored noopener\" target=\"_blank\">", page);
            Assert.Contains($"<img src=\"/media/{key}.png\" width=\"40\" height=\"30\" alt=\"Partner Co\"", page);
            Assert.DoesNotContain(key, await f.NewClient().GetStringAsync("/en"));

            // An unknown group id and a non-https website are refused, nothing changes.
            token = await c.GetTokenAsync("/Admin/Partners");
            var bad = await c.PostAsync("/Admin/Partners/Update", Multipart(token, null, ("Id", id.ToString()), ("NameAr", name), ("PartnerGroupId", "999999")));
            Assert.Equal(HttpStatusCode.Redirect, bad.StatusCode);
            bad = await c.PostAsync("/Admin/Partners/Update", Multipart(token, null, ("Id", id.ToString()), ("NameAr", name), ("Url", "javascript:alert(1)")));
            Assert.Equal(HttpStatusCode.Redirect, bad.StatusCode);
            Assert.Equal(("https://example.org/p", (int?)groupId), await f.DbAsync(db => db.Partners.Where(p => p.Id == id).Select(p => new ValueTuple<string?, int?>(p.Url, p.PartnerGroupId)).SingleAsync()));

            // Deleting the group keeps the partner (no group); deleting the partner removes its logo.
            await c.PostAsync("/Admin/PartnerGroups/Delete", Form(token, ("id", groupId.ToString())));
            Assert.Null(await f.DbAsync(db => db.Partners.Where(p => p.Id == id).Select(p => p.PartnerGroupId).SingleAsync()));
            await c.PostAsync("/Admin/Partners/Delete", Form(token, ("id", id.ToString())));
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{key}.png")).StatusCode);
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                db.Partners.RemoveRange(db.Partners.Where(p => p.Id == id));
                db.PartnerGroups.RemoveRange(db.PartnerGroups.Where(g => g.Id == groupId));
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact] // a page image becomes the page's share image; ≥ 1200 px wide → summary_large_image with its size
    public async Task Page_image_is_the_share_image()
    {
        var c = await f.SignedInClientAsync();
        var row = await f.DbAsync(db => db.Pages.AsNoTracking().SingleAsync(p => p.Key == "forum"));
        async Task SaveAsync((string, byte[], string)? file, bool remove)
        {
            var token = await c.GetTokenAsync("/Admin/Pages/Edit/forum");
            var r = await c.PostAsync("/Admin/Pages/Edit/forum", Multipart(token, file,
                ("TitleAr", row.TitleAr), ("TitleEn", row.TitleEn ?? ""), ("SummaryAr", row.SummaryAr), ("SummaryEn", row.SummaryEn ?? ""),
                ("IntroAr", row.IntroAr), ("IntroEn", row.IntroEn ?? ""), ("BodyAr", row.BodyAr), ("BodyEn", row.BodyEn ?? ""),
                ("EmptyTextAr", row.EmptyTextAr), ("EmptyTextEn", row.EmptyTextEn ?? ""), ("MetaTitleAr", row.MetaTitleAr),
                ("MetaTitleEn", row.MetaTitleEn ?? ""), ("MetaDescriptionAr", row.MetaDescriptionAr), ("MetaDescriptionEn", row.MetaDescriptionEn ?? ""),
                ("ShowInMenu", row.ShowInMenu ? "true" : "false"), ("RemoveImage", remove ? "true" : "false")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        }
        await SaveAsync(("og.jpg", ImageInspectorTests.Fixture("og-1200x630.jpg"), "image/jpeg"), false);
        try
        {
            var key = await f.DbAsync(db => db.Pages.Where(p => p.Key == "forum").Select(p => p.Image!.FileKey).SingleAsync());
            var html = await f.NewClient().GetStringAsync("/forum");
            Assert.Contains($"<meta property=\"og:image\" content=\"https://www.innovest-holding.com/media/{key}.jpg\">", html);
            Assert.Contains("<meta property=\"og:image:width\" content=\"1200\">", html);
            Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\">", html);
            // The ecosystem card of the forum shows it.
            Assert.Contains($"src=\"/media/{key}.jpg\"", await f.NewClient().GetStringAsync("/ecosystem"));
        }
        finally
        {
            await SaveAsync(null, true);
            // The admin save normalises the texts (trim, line ends): put the row back exactly as it was for the other tests.
            await f.DbAsync(async db =>
            {
                var p = await db.Pages.SingleAsync(x => x.Key == "forum");
                db.Entry(p).CurrentValues.SetValues(row);
                return await db.SaveChangesAsync();
            });
        }
        Assert.Null(await f.DbAsync(db => db.Pages.Where(p => p.Key == "forum").Select(p => p.ImageId).SingleAsync()));
    }

    [Fact] // the founder photo: its own form on the content editor, shown in the founder block
    public async Task Founder_photo_is_shown_with_the_founder_block()
    {
        var c = await f.SignedInClientAsync();
        var message = await f.DbAsync(db => db.SiteContents.SingleAsync(s => s.SectionKey == "founder_message"));
        await f.DbAsync(async db => { (await db.SiteContents.SingleAsync(s => s.SectionKey == "founder_message")).ContentAr = "كلمة المؤسس"; return await db.SaveChangesAsync(); });
        try
        {
            var token = await c.GetTokenAsync("/Admin/Content/Edit");
            var r = await c.PostAsync("/Admin/Content/FounderPhoto", Multipart(token, ("me.webp", ImageInspectorTests.Fixture("lossy.webp"), "image/webp"), ("ImageAltAr", "صورة المؤسس")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var key = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "founder_photo").Select(s => s.ContentAr).SingleAsync());
            Assert.Matches("^[0-9a-f]{32}$", key);
            Assert.Contains($"<img class=\"founder-photo\" src=\"/media/{key}.webp\" width=\"32\" height=\"32\" alt=\"صورة المؤسس\"", await f.NewClient().GetStringAsync("/about"));

            token = await c.GetTokenAsync("/Admin/Content/Edit");
            r = await c.PostAsync("/Admin/Content/FounderPhoto", Multipart(token, null, ("RemoveImage", "true")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync($"/media/{key}.webp")).StatusCode);
            Assert.DoesNotContain("founder-photo", await f.NewClient().GetStringAsync("/about"));
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                (await db.SiteContents.SingleAsync(s => s.SectionKey == "founder_message")).ContentAr = message.ContentAr;
                db.SiteContents.RemoveRange(db.SiteContents.Where(s => s.SectionKey == "founder_photo"));
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact] // the janitor removes orphans older than 24 h and stale .tmp files, never a file that has a row
    public async Task Sweep_removes_orphans_only()
    {
        var store = f.Services.GetRequiredService<MediaStore>();
        var orphan = Guid.NewGuid().ToString("N");
        var young = Guid.NewGuid().ToString("N");
        var tmp = Guid.NewGuid().ToString("N");
        var kept = Guid.NewGuid().ToString("N");
        string Put(string key, string ext, TimeSpan age)
        {
            var p = Path.Combine(store.Root, key[..2], key + ext);
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllBytes(p, ImageInspectorTests.Fixture("text-chunk.png"));
            File.SetLastWriteTimeUtc(p, f.Time.GetUtcNow().UtcDateTime - age);
            return p;
        }
        var pOrphan = Put(orphan, ".png", TimeSpan.FromHours(25));
        var pYoung = Put(young, ".png", TimeSpan.FromHours(2));
        var pTmp = Put(tmp, ".png.tmp", TimeSpan.FromHours(2));
        var pKept = Put(kept, ".png", TimeSpan.FromDays(3));
        await f.DbAsync(db =>
        {
            db.MediaFiles.Add(new MediaFile { FileKey = kept, Format = "png", Width = 40, Height = 30, Bytes = 1 });
            return db.SaveChangesAsync();
        });
        try
        {
            var deleted = await f.DbAsync(db => store.SweepAsync(db));
            Assert.True(deleted >= 2, $"{deleted} deleted");
            Assert.False(File.Exists(pOrphan));
            Assert.False(File.Exists(pTmp));
            Assert.True(File.Exists(pYoung));
            Assert.True(File.Exists(pKept));
        }
        finally
        {
            await f.DbAsync(async db => { db.MediaFiles.Remove(await db.MediaFiles.SingleAsync(m => m.FileKey == kept)); return await db.SaveChangesAsync(); });
            File.Delete(pYoung);
            File.Delete(pKept);
        }
    }
}
