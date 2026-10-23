using System.ComponentModel.DataAnnotations;
using System.Net;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Models;
using InnovestHolding.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// F10 admin (T9): «النسخ السابقة» (editions CRUD, photos: upload one per POST, cap 12, alt, order, delete), the
/// month-only event mode, the PartnerGroups «يظهر في شريط المنتدى» switch, the list labels and the sidebar.
/// Every edition and event created here is hidden (IsActive=false), so the public pages other tests read are unchanged.
/// </summary>
[Collection("App")]
public class ForumAdminTests(TestAppFactory f)
{
    private const string Index = "/Admin/ForumEditions";

    private static byte[] Jpeg => ImageInspectorTests.Fixture("photo-exif.jpg");

    private async Task<(HttpClient Client, string Token)> AdminAsync(string page = Index)
    {
        var client = await f.SignedInClientAsync();
        return (client, await client.GetTokenAsync(page));
    }

    private static string[] StoredFiles() =>
        Directory.Exists(TestAppFactory.UploadsRoot)
            ? Directory.GetFiles(TestAppFactory.UploadsRoot, "*", SearchOption.AllDirectories)
            : [];

    /// <summary>Creates a hidden edition through the admin and returns its id (from the redirect to its edit page).</summary>
    private static async Task<int> CreateEditionAsync(HttpClient c, string token, string title, string year = "2024")
    {
        var r = await c.PostAsync(Index + "/Create", Form(token, ("Year", year), ("TitleAr", title), ("IsActive", "false")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var location = r.Headers.Location!.OriginalString;
        Assert.StartsWith("/Admin/ForumEditions/Edit/", location);
        return int.Parse(location["/Admin/ForumEditions/Edit/".Length..].Split('#')[0], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Task<HttpResponseMessage> AddPhotoAsync(HttpClient c, string token, int editionId, (string, byte[], string)? file, params (string, string)[] extra) =>
        c.PostAsync(Index + "/AddPhoto", MediaUploadTests.Multipart(token, file, [("EditionId", editionId.ToString()), .. extra]));

    private Task<List<ForumEditionPhoto>> PhotosAsync(int editionId) =>
        f.DbAsync(db => db.ForumEditionPhotos.AsNoTracking().Include(p => p.Image).Where(p => p.ForumEditionId == editionId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    /// <summary>The page the PRG redirect leads to (TempData shows there once), decoded.</summary>
    private static async Task<string> FollowAsync(HttpClient c, HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return WebUtility.HtmlDecode(await c.GetStringAsync(r.Headers.Location!.OriginalString.Split('#')[0]));
    }

    // ── Check #5: anonymous → login, no token → 400, on every new action ──

    public static TheoryData<string> NewPosts =>
    [
        "/Admin/ForumEditions/Create", "/Admin/ForumEditions/Update", "/Admin/ForumEditions/Toggle/1",
        "/Admin/ForumEditions/Move/1", "/Admin/ForumEditions/Delete/1", "/Admin/ForumEditions/AddPhoto",
        "/Admin/ForumEditions/UpdatePhoto", "/Admin/ForumEditions/MovePhoto/1", "/Admin/ForumEditions/DeletePhoto/1",
        "/Admin/PartnerGroups/Create", "/Admin/PartnerGroups/Update",
    ];

    [Theory]
    [MemberData(nameof(NewPosts))]
    public async Task Anonymous_post_with_a_valid_token_goes_to_login(string path)
    {
        var anon = f.NewClient();
        var token = await anon.GetTokenAsync("/Account/Login");
        var title = Unique("anon-edition");
        var r = await anon.PostAsync(path, Form(token, ("Year", "2024"), ("TitleAr", title), ("NameAr", title), ("EditionId", "1"), ("direction", "1")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);
        Assert.False(await f.DbAsync(db => db.ForumEditions.AnyAsync(e => e.TitleAr == title)));
        Assert.False(await f.DbAsync(db => db.PartnerGroups.AnyAsync(g => g.NameAr == title)));
    }

    [Theory]
    [MemberData(nameof(NewPosts))]
    public async Task Signed_in_post_without_a_token_is_400(string path)
    {
        var (c, _) = await AdminAsync();
        var title = Unique("tokenless");
        var r = await c.PostAsync(path, Form(("Year", "2024"), ("TitleAr", title), ("NameAr", title), ("EditionId", "1"), ("direction", "1")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False(await f.DbAsync(db => db.ForumEditions.AnyAsync(e => e.TitleAr == title)));
        Assert.False(await f.DbAsync(db => db.PartnerGroups.AnyAsync(g => g.NameAr == title)));
    }

    [Fact] // the GET screens are admin-only too
    public async Task Anonymous_gets_of_the_editions_screens_go_to_login()
    {
        var id = await f.DbAsync(db => db.ForumEditions.Select(e => e.Id).FirstAsync());
        foreach (var path in new[] { Index, $"{Index}/Edit/{id}" })
        {
            var r = await f.NewClient().GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);
        }
    }

    // ── Editions CRUD ──

    [Fact] // list (migration rows included), create → edit page, update every field, toggle, delete
    public async Task Edition_create_update_toggle_delete()
    {
        var (c, t) = await AdminAsync();
        var list = await c.GetStringAsync(Index);
        Assert.Contains("منتدى إنوفست 2025", list); // a row of the migration AddForumContent

        var title = Unique("نسخة");
        var id = await CreateEditionAsync(c, t, title, "2022");
        var created = await f.DbAsync(db => db.ForumEditions.AsNoTracking().SingleAsync(e => e.Id == id));
        Assert.Equal(((short)2022, title, "", false, (string?)null), (created.Year, created.TitleAr, created.EditionLabelAr, created.IsActive, created.TitleEn));
        Assert.Equal(await f.DbAsync(db => db.ForumEditions.MaxAsync(e => e.SortOrder)), created.SortOrder); // at the end

        var edit = await c.GetStringAsync($"{Index}/Edit/{id}");
        Assert.Contains(title, edit);
        var r = await c.PostAsync(Index + "/Update", Form(t, ("Id", id.ToString()), ("Year", "2021"), ("EditionLabelAr", "النسخة الأولى"),
            ("EditionLabelEn", "First Edition"), ("TitleAr", title), ("TitleEn", "Title EN"), ("PlaceAr", "القاهرة"), ("PlaceEn", "Cairo"),
            ("TextAr", "فقرة أولى\r\n\r\nفقرة ثانية"), ("TextEn", " "), ("IsActive", "false")));
        Assert.Contains("تم حفظ النسخة", await FollowAsync(c, r));
        var updated = await f.DbAsync(db => db.ForumEditions.AsNoTracking().SingleAsync(e => e.Id == id));
        Assert.Equal(((short)2021, "النسخة الأولى", "First Edition", "Title EN", "القاهرة", "Cairo", "فقرة أولى\n\nفقرة ثانية", (string?)null),
            (updated.Year, updated.EditionLabelAr, updated.EditionLabelEn, updated.TitleEn, updated.PlaceAr, updated.PlaceEn, updated.TextAr, updated.TextEn));

        await c.PostAsync($"{Index}/Toggle/{id}", Form(t));
        Assert.True(await f.DbAsync(db => db.ForumEditions.Where(e => e.Id == id).Select(e => e.IsActive).SingleAsync()));
        await c.PostAsync($"{Index}/Toggle/{id}", Form(t));
        Assert.False(await f.DbAsync(db => db.ForumEditions.Where(e => e.Id == id).Select(e => e.IsActive).SingleAsync()));

        r = await c.PostAsync($"{Index}/Delete/{id}", Form(t));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.False(await f.DbAsync(db => db.ForumEditions.AnyAsync(e => e.Id == id)));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"{Index}/Edit/{id}")).StatusCode);
    }

    [Theory] // invalid input writes nothing: year out of range, no title, a too-long label
    [InlineData("1999", "x", "")]
    [InlineData("2101", "x", "")]
    [InlineData("abc", "x", "")]
    [InlineData("2024", "", "")]
    [InlineData("2024", "x", "0123456789012345678901234567890123456789012345678901234567890")] // 61 > 60
    public async Task Invalid_edition_is_refused(string year, string titleSuffix, string label)
    {
        var (c, t) = await AdminAsync();
        var title = titleSuffix.Length == 0 ? "" : Unique("bad");
        var before = await f.DbAsync(db => db.ForumEditions.CountAsync());
        var r = await c.PostAsync(Index + "/Create", Form(t, ("Year", year), ("TitleAr", title), ("EditionLabelAr", label), ("IsActive", "false")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.EndsWith("/Admin/ForumEditions", r.Headers.Location!.OriginalString);
        Assert.Equal(before, await f.DbAsync(db => db.ForumEditions.CountAsync()));

        // The same on Update: the form comes back with the error, the row is unchanged.
        var id = await CreateEditionAsync(c, t, Unique("ok"));
        r = await c.PostAsync(Index + "/Update", Form(t, ("Id", id.ToString()), ("Year", year), ("TitleAr", title), ("EditionLabelAr", label)));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("form-errors", await r.Content.ReadAsStringAsync());
        Assert.Equal(2024, await f.DbAsync(db => db.ForumEditions.Where(e => e.Id == id).Select(e => (int)e.Year).SingleAsync()));
    }

    [Fact] // ▲▼ of the editions: the edges write nothing, a bad direction or an unknown id is 404
    public async Task Edition_reorder_and_bounds()
    {
        var (c, t) = await AdminAsync();
        var a = await CreateEditionAsync(c, t, Unique("a"));
        var b = await CreateEditionAsync(c, t, Unique("b")); // b is last
        async Task<List<int>> Order() => await f.DbAsync(db => db.ForumEditions.OrderBy(e => e.SortOrder).ThenBy(e => e.Id).Select(e => e.Id).ToListAsync());

        var before = await Order();
        Assert.Equal(b, before[^1]);
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync($"{Index}/Move/{b}", Form(t, ("direction", "1")))).StatusCode);
        Assert.Equal(before, await Order()); // last row ▼: unchanged
        var first = before[0];
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync($"{Index}/Move/{first}", Form(t, ("direction", "-1")))).StatusCode);
        Assert.Equal(before, await Order()); // first row ▲: unchanged

        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/Move/{b}", Form(t, ("direction", "2")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/Move/{b}", Form(t, ("direction", "0")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/Move/999999", Form(t, ("direction", "1")))).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync($"{Index}/Move/{b}", Form(t, ("direction", "-1")))).StatusCode);
        var after = await Order();
        Assert.True(after.IndexOf(b) < after.IndexOf(a));
        Assert.Equal(Enumerable.Range(1, after.Count), await f.DbAsync(db => db.ForumEditions.OrderBy(e => e.SortOrder).Select(e => e.SortOrder).ToListAsync()));
    }

    // ── Photos ──

    [Fact] // one photo per POST, stored as a MediaFile with its alt texts, shown on the edit page; then alt, order, delete
    public async Task Photo_upload_alt_order_delete()
    {
        var (c, t) = await AdminAsync();
        var id = await CreateEditionAsync(c, t, Unique("photos"));
        var r = await AddPhotoAsync(c, t, id, ("a.jpg", Jpeg, "image/jpeg"), ("ImageAltAr", "وصف أ"), ("ImageAltEn", "Alt A"));
        Assert.Contains("تمت إضافة الصورة", await FollowAsync(c, r));
        await AddPhotoAsync(c, t, id, ("b.png", ImageInspectorTests.Fixture("text-chunk.png"), "image/png"));
        var photos = await PhotosAsync(id);
        Assert.Equal(2, photos.Count);
        Assert.Equal(("وصف أ", "Alt A"), (photos[0].Image!.AltAr, photos[0].Image!.AltEn));
        Assert.Equal(("", (string?)null), (photos[1].Image!.AltAr, photos[1].Image!.AltEn));
        var edit = await c.GetStringAsync($"{Index}/Edit/{id}");
        Assert.Contains($"/media/{photos[0].Image!.FileKey}.jpg", edit);
        Assert.Contains($"/media/{photos[1].Image!.FileKey}.png", edit);

        // Alt texts of photo 2.
        r = await c.PostAsync(Index + "/UpdatePhoto", Form(t, ("EditionId", id.ToString()), ("PhotoId", photos[1].Id.ToString()),
            ("ImageAltAr", " وصف ب "), ("ImageAltEn", "Alt B")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var alt = await f.DbAsync(db => db.MediaFiles.AsNoTracking().SingleAsync(m => m.Id == photos[1].ImageId));
        Assert.Equal(("وصف ب", "Alt B"), (alt.AltAr, alt.AltEn));

        // ▲ on the second photo makes it the cover; ▲ again (now first) changes nothing; ±2 is 404.
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync($"{Index}/MovePhoto/{photos[1].Id}", Form(t, ("editionId", id.ToString()), ("direction", "-1")))).StatusCode);
        Assert.Equal(new[] { photos[1].Id, photos[0].Id }, (await PhotosAsync(id)).Select(p => p.Id));
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync($"{Index}/MovePhoto/{photos[1].Id}", Form(t, ("editionId", id.ToString()), ("direction", "-1")))).StatusCode);
        Assert.Equal(new[] { photos[1].Id, photos[0].Id }, (await PhotosAsync(id)).Select(p => p.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/MovePhoto/{photos[1].Id}", Form(t, ("editionId", id.ToString()), ("direction", "2")))).StatusCode);

        // Delete one photo: its row, its MediaFile and its file leave.
        var path = Path.Combine(TestAppFactory.UploadsRoot, photos[0].Image!.FileKey[..2], photos[0].Image!.FileKey + ".jpg");
        Assert.True(File.Exists(path));
        r = await c.PostAsync($"{Index}/DeletePhoto/{photos[0].Id}", Form(t, ("editionId", id.ToString())));
        Assert.Contains("تم حذف الصورة", await FollowAsync(c, r));
        Assert.Single(await PhotosAsync(id));
        Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.Id == photos[0].ImageId)));
        Assert.False(File.Exists(path));

        // Delete the edition: the last photo's row, MediaFile and file leave with it.
        var lastPath = Path.Combine(TestAppFactory.UploadsRoot, photos[1].Image!.FileKey[..2], photos[1].Image!.FileKey + ".png");
        Assert.True(File.Exists(lastPath));
        await c.PostAsync($"{Index}/Delete/{id}", Form(t));
        Assert.False(await f.DbAsync(db => db.ForumEditionPhotos.AnyAsync(p => p.ForumEditionId == id)));
        Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.Id == photos[1].ImageId)));
        Assert.False(File.Exists(lastPath));
    }

    [Fact] // a photo id must belong to the posted edition: alt, move and delete of another edition's photo are 404
    public async Task Foreign_photo_ids_are_404()
    {
        var (c, t) = await AdminAsync();
        var mine = await CreateEditionAsync(c, t, Unique("mine"));
        var other = await CreateEditionAsync(c, t, Unique("other"));
        await AddPhotoAsync(c, t, other, ("a.jpg", Jpeg, "image/jpeg"), ("ImageAltAr", "أصلي"));
        var photo = (await PhotosAsync(other)).Single();

        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(Index + "/UpdatePhoto",
            Form(t, ("EditionId", mine.ToString()), ("PhotoId", photo.Id.ToString()), ("ImageAltAr", "مخترق")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/MovePhoto/{photo.Id}", Form(t, ("editionId", mine.ToString()), ("direction", "1")))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Index}/DeletePhoto/{photo.Id}", Form(t, ("editionId", mine.ToString())))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AddPhotoAsync(c, t, 999999, ("a.jpg", Jpeg, "image/jpeg"))).StatusCode);

        var after = (await PhotosAsync(other)).Single();
        Assert.Equal(("أصلي", photo.ImageId), (after.Image!.AltAr, after.ImageId));
    }

    public static TheoryData<string, string, string, string> RefusedUploads => new()
    {
        { "image.svg", "x.svg", "image/svg+xml", "JPG أو PNG أو WebP" },
        { "image.gif", "x.gif", "image/gif", "JPG أو PNG أو WebP" },
        { "page.html", "x.png", "image/png", "JPG أو PNG أو WebP" },
        { "text-chunk.png", "x.jpg", "image/jpeg", "غير متطابقة" },
        { "big", "big.jpg", "image/jpeg", "5 ميغابايت" },
    };

    [Theory] // SVG, wrong type, mismatch, size: refused with the message, nothing written (no row, no MediaFile, no file)
    [MemberData(nameof(RefusedUploads))]
    public async Task Refused_photo_uploads_write_nothing(string fixture, string fileName, string type, string message)
    {
        var (c, t) = await AdminAsync();
        var id = await CreateEditionAsync(c, t, Unique("refused"));
        var data = fixture == "big" ? new byte[MediaStore.MaxBytes + 1] : ImageInspectorTests.Fixture(fixture);
        if (fixture == "big") Jpeg.CopyTo(data, 0);
        var rows = await f.DbAsync(db => db.MediaFiles.CountAsync());
        var files = StoredFiles().Length;

        var r = await AddPhotoAsync(c, t, id, (fileName, data, type));

        Assert.Contains(message, await FollowAsync(c, r));
        Assert.Empty(await PhotosAsync(id));
        Assert.Equal(rows, await f.DbAsync(db => db.MediaFiles.CountAsync()));
        Assert.Equal(files, StoredFiles().Length);
    }

    [Fact] // no file chosen: refused, nothing written
    public async Task Photo_without_a_file_is_refused()
    {
        var (c, t) = await AdminAsync();
        var id = await CreateEditionAsync(c, t, Unique("nofile"));
        var r = await AddPhotoAsync(c, t, id, null);
        Assert.Contains("اختر صورة لرفعها", await FollowAsync(c, r));
        Assert.Empty(await PhotosAsync(id));
    }

    [Fact] // the 13th photo is refused server-side (nothing written) and the upload form is gone at 12
    public async Task Thirteenth_photo_is_refused()
    {
        var (c, t) = await AdminAsync();
        var id = await CreateEditionAsync(c, t, Unique("cap"));
        for (var i = 0; i < ForumEdition.MaxPhotos; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await AddPhotoAsync(c, t, id, ($"p{i}.jpg", Jpeg, "image/jpeg"))).StatusCode);
        Assert.Equal(ForumEdition.MaxPhotos, (await PhotosAsync(id)).Count);
        var edit = await c.GetStringAsync($"{Index}/Edit/{id}");
        Assert.DoesNotContain("/Admin/ForumEditions/AddPhoto", edit);

        var rows = await f.DbAsync(db => db.MediaFiles.CountAsync());
        var files = StoredFiles().Length;
        var r = await AddPhotoAsync(c, t, id, ("p13.jpg", Jpeg, "image/jpeg"));
        Assert.Contains("احذف صورة قبل إضافة غيرها", await FollowAsync(c, r)); // the server message, not the page hint
        Assert.Equal(ForumEdition.MaxPhotos, (await PhotosAsync(id)).Count);
        Assert.Equal(rows, await f.DbAsync(db => db.MediaFiles.CountAsync()));
        Assert.Equal(files, StoredFiles().Length);

        await c.PostAsync($"{Index}/Delete/{id}", Form(t)); // leaves no files behind for the other tests
        Assert.Equal(files - ForumEdition.MaxPhotos, StoredFiles().Length);
    }

    // ── Events: month-only ──

    [Theory] // the 1st of the month 00:00 to the last day 23:59 at the offset; February in leap and common years
    [InlineData("2028-02", "+02:00", "2028-02-01T00:00:00+02:00", "2028-02-29T23:59:00+02:00")]
    [InlineData("2027-02", "+02:00", "2027-02-01T00:00:00+02:00", "2027-02-28T23:59:00+02:00")]
    [InlineData("2100-02", "+03:00", "2100-02-01T00:00:00+03:00", "2100-02-28T23:59:00+03:00")] // 2100 is not a leap year
    [InlineData("2000-02", "+00:00", "2000-02-01T00:00:00+00:00", "2000-02-29T23:59:00+00:00")] // 2000 is
    [InlineData("2026-12", "-05:00", "2026-12-01T00:00:00-05:00", "2026-12-31T23:59:00-05:00")]
    [InlineData("2026-04", "+02:00", "2026-04-01T00:00:00+02:00", "2026-04-30T23:59:00+02:00")]
    public void Month_only_dates(string month, string offset, string starts, string ends)
    {
        var input = new EventInput { DateIsMonth = true, Month = month, UtcOffset = offset, StartsAtLocal = new DateTime(2020, 5, 5, 10, 0, 0) };
        Assert.Equal(DateTimeOffset.Parse(starts, System.Globalization.CultureInfo.InvariantCulture), input.StartsAt());
        Assert.Equal(DateTimeOffset.Parse(ends, System.Globalization.CultureInfo.InvariantCulture), input.EndsAt());
        Assert.Equal(TimeSpan.Parse(offset.TrimStart('+'), System.Globalization.CultureInfo.InvariantCulture), input.StartsAt()!.Value.Offset);
    }

    [Theory] // month mode needs a yyyy-MM month from 2000 to 2100 and a valid offset; the day/time fields are not needed
    [InlineData("", "+02:00", false)]
    [InlineData("2026-13", "+02:00", false)]
    [InlineData("1999-12", "+02:00", false)]
    [InlineData("2101-01", "+02:00", false)]
    [InlineData("2026-6", "+02:00", false)]
    [InlineData("2026-06-01", "+02:00", false)]
    [InlineData("2026-06", "nope", false)]
    [InlineData("2026-06", "+02:00", true)]
    public void Month_only_validation(string month, string offset, bool valid)
    {
        var input = new EventInput { TitleAr = "x", DateIsMonth = true, Month = month, UtcOffset = offset };
        var results = new List<ValidationResult>();
        Assert.Equal(valid, Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true));
    }

    private static (string, string)[] EventFields(string title, params (string, string)[] extra) =>
    [
        ("TitleAr", title), ("TitleEn", title), ("Status", "Scheduled"), ("AttendanceMode", "Offline"), ("IsActive", "false"),
        ("RegistrationOpen", "false"), .. extra,
    ];

    [Fact] // the admin form stores month-only events (Feb of a leap year), shows the month back, and can switch back to a date
    public async Task Month_only_event_round_trip()
    {
        var (c, t) = await AdminAsync("/Admin/Events/Create");
        var title = Unique("month-event");
        var r = await c.PostAsync("/Admin/Events/Create", MediaUploadTests.Multipart(t, null, EventFields(title,
            ("DateIsMonth", "true"), ("DateIsMonth", "false"), ("Month", "2028-02"), ("UtcOffset", "+02:00"))));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var e = await f.DbAsync(db => db.Events.AsNoTracking().SingleAsync(x => x.TitleAr == title));
        Assert.True(e.DateIsMonth);
        Assert.Equal(new DateTimeOffset(2028, 2, 1, 0, 0, 0, TimeSpan.FromHours(2)), e.StartsAt);
        Assert.Equal(new DateTimeOffset(2028, 2, 29, 23, 59, 0, TimeSpan.FromHours(2)), e.EndsAt);
        Assert.Equal((1, "February"), (e.EventDay, e.EventMonthEn));
        Assert.False(string.IsNullOrEmpty(e.EventMonthAr));

        var edit = await c.GetStringAsync($"/Admin/Events/Edit/{e.Id}");
        Assert.Contains("value=\"2028-02\"", edit);
        Assert.Matches("id=\"DateIsMonth\"[^>]*checked", edit);
        Assert.DoesNotContain("value=\"2028-02-01T00:00\"", edit); // the exact fields stay empty in month mode
        Assert.Contains("الشهر فقط", await c.GetStringAsync("/Admin/Events"));

        // Back to an exact date: DateIsMonth is cleared.
        r = await c.PostAsync("/Admin/Events/Edit", MediaUploadTests.Multipart(t, null, EventFields(title,
            ("Id", e.Id.ToString()), ("Slug", e.Slug), ("SortOrder", e.SortOrder.ToString()), ("DateIsMonth", "false"), ("Month", "2028-02"),
            ("StartsAtLocal", "2028-03-10T10:00"), ("UtcOffset", "+02:00"))));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        e = await f.DbAsync(db => db.Events.AsNoTracking().SingleAsync(x => x.Id == e.Id));
        Assert.False(e.DateIsMonth);
        Assert.Equal(new DateTimeOffset(2028, 3, 10, 10, 0, 0, TimeSpan.FromHours(2)), e.StartsAt);
        Assert.Null(e.EndsAt);

        await c.PostAsync($"/Admin/Events/Delete", Form(t, ("id", e.Id.ToString())));
    }

    [Fact] // an invalid month is refused with the message and no row
    public async Task Month_only_event_with_a_bad_month_is_refused()
    {
        var (c, t) = await AdminAsync("/Admin/Events/Create");
        var title = Unique("bad-month");
        var r = await c.PostAsync("/Admin/Events/Create", MediaUploadTests.Multipart(t, null, EventFields(title,
            ("DateIsMonth", "true"), ("Month", "2026-13"), ("UtcOffset", "+02:00"))));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("اختر الشهر والسنة", WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
        Assert.False(await f.DbAsync(db => db.Events.AnyAsync(x => x.TitleAr == title)));
    }

    // ── PartnerGroups «يظهر في شريط المنتدى» ──

    [Fact] // create checked → 1; update unchecked (hidden false only) → 0; checked again → 1; the form shows the state
    public async Task Partner_group_forum_strip_round_trip()
    {
        var (c, t) = await AdminAsync("/Admin/Partners");
        var name = Unique("مجموعة");
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/PartnerGroups/Create",
            Form(t, ("NameAr", name), ("ShowOnForumHub", "true"), ("ShowOnForumHub", "false")))).StatusCode);
        var g = await f.DbAsync(db => db.PartnerGroups.AsNoTracking().SingleAsync(x => x.NameAr == name));
        Assert.True(g.ShowOnForumHub);
        async Task<string> GroupForm() => System.Text.RegularExpressions.Regex.Match(await c.GetStringAsync("/Admin/Partners"),
            $"name=\"Id\" value=\"{g.Id}\">((?:(?!</form>)[\\s\\S])*)</form>").Groups[1].Value;
        Assert.Contains("name=\"ShowOnForumHub\" value=\"true\" checked", await GroupForm());

        async Task Update(params (string, string)[] hub) => Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/PartnerGroups/Update",
            Form(t, [("Id", g.Id.ToString()), ("NameAr", name), ("SortOrder", g.SortOrder.ToString()), ("IsActive", "true"), .. hub]))).StatusCode);
        async Task<bool> Hub() => await f.DbAsync(db => db.PartnerGroups.Where(x => x.Id == g.Id).Select(x => x.ShowOnForumHub).SingleAsync());

        await Update(("ShowOnForumHub", "false"));
        Assert.False(await Hub());
        var form = await GroupForm();
        Assert.Contains("name=\"ShowOnForumHub\" value=\"true\"", form);
        Assert.DoesNotContain("name=\"ShowOnForumHub\" value=\"true\" checked", form);
        await Update(("ShowOnForumHub", "true"), ("ShowOnForumHub", "false"));
        Assert.True(await Hub());
        await Update(); // a form without the field leaves false (the checkbox posts its hidden false)
        Assert.False(await Hub());

        // A group created without the box is not in the strip; the migration's «برعاية» group is.
        var plain = Unique("عادية");
        await c.PostAsync("/Admin/PartnerGroups/Create", Form(t, ("NameAr", plain)));
        Assert.False(await f.DbAsync(db => db.PartnerGroups.Where(x => x.NameAr == plain).Select(x => x.ShowOnForumHub).SingleAsync()));
        Assert.True(await f.DbAsync(db => db.PartnerGroups.Where(x => x.NameAr == "برعاية").Select(x => x.ShowOnForumHub).SingleAsync()));

        foreach (var n in new[] { name, plain })
        {
            var id = await f.DbAsync(db => db.PartnerGroups.Where(x => x.NameAr == n).Select(x => x.Id).SingleAsync());
            await c.PostAsync("/Admin/PartnerGroups/Delete", Form(t, ("id", id.ToString())));
        }
    }

    // ── Lists and sidebar ──

    [Theory] // the forum lists name their fields after what the rows hold; the company lists keep «النص»
    [InlineData("forum_figures", "الرقم كما يظهر", "الوصف (ما يعنيه الرقم)")]
    [InlineData("forum_audiences", "عنوان البطاقة", "النقاط — سطر لكل نقطة")]
    [InlineData("forum_pillars", "عنوان البطاقة", "النقاط — سطر لكل نقطة")]
    [InlineData("forum_components", "النص", "في بند الجوائز: جائزة في كل سطر")]
    [InlineData("forum_goals", "النص", "تُرقَّم العناصر")]
    [InlineData("forum_sectors", "النص", "تُرقَّم العناصر")]
    [InlineData("network_steps", "النص", "الوصف (اختياري)")]
    public async Task List_labels_follow_the_group_kind(string group, string textLabel, string other)
    {
        var (c, _) = await AdminAsync();
        var html = WebUtility.HtmlDecode(await c.GetStringAsync($"/Admin/ContentItems?group={group}"));
        Assert.Contains($"<label for=\"new-TextAr\">{textLabel}", html);
        Assert.Contains(other, html);
        if (group.StartsWith("forum_", StringComparison.Ordinal))
            Assert.Contains("href=\"/Admin/Content/Edit#group-forum\"", html);
        else
            Assert.DoesNotContain("النقاط — سطر لكل نقطة", html);
    }

    [Fact] // the sidebar links both forum screens; the anchor shortcut is never the "current" link
    public async Task Sidebar_has_the_forum_entries()
    {
        var (c, _) = await AdminAsync();
        var html = await c.GetStringAsync(Index);
        Assert.Matches("href=\"/Admin/ForumEditions\" class=\"sidebar-link active\"", html);
        Assert.Contains("href=\"/Admin/Content/Edit#group-forum\" class=\"sidebar-link\"", html);
        var content = await c.GetStringAsync("/Admin/Content/Edit");
        Assert.Contains("href=\"/Admin/Content/Edit\" class=\"sidebar-link active\"", content);
        Assert.Contains("href=\"/Admin/Content/Edit#group-forum\" class=\"sidebar-link\"", content);
        Assert.Contains("id=\"group-forum\"", content);
    }
}
