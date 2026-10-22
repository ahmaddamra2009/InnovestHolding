using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>Pure rules of the ecosystem entities: the YouTube id parser and the two forms of each English rule.</summary>
public class EcosystemRuleTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?feature=share&v=dQw4w9WgXcQ&t=10", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ", null)]       // not https
    [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ", null)]
    [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ", null)]
    [InlineData("https://www.youtube.com/watch?v=short", null)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ%22%3E", null)] // id with markup
    [InlineData("https://www.youtube.com:8443/watch?v=dQw4w9WgXcQ", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void YouTube_ids_come_only_from_known_https_addresses(string? url, string? id) => Assert.Equal(id, YouTube.VideoId(url));

    public static TheoryData<string?, string, string?, string, string?> PersonCases()
    {
        var data = new TheoryData<string?, string, string?, string, string?>();
        foreach (var nameEn in new string?[] { null, "", "Name" })
        foreach (var jobAr in new[] { "", "منصب" })
        foreach (var jobEn in new string?[] { null, "Role" })
        foreach (var bioAr in new[] { "", "نبذة" })
        foreach (var bioEn in new string?[] { null, "Bio" })
            data.Add(nameEn, jobAr, jobEn, bioAr, bioEn);
        return data;
    }

    [Theory, MemberData(nameof(PersonCases))] // the SQL count and the page decide the same (stored values: trimmed or NULL)
    public void Person_english_rule_is_the_same_in_sql_and_in_code(string? nameEn, string jobAr, string? jobEn, string bioAr, string? bioEn)
    {
        var row = new Speaker { NameEn = nameEn, JobTitleAr = jobAr, JobTitleEn = jobEn, BioAr = bioAr, BioEn = bioEn };
        Assert.Equal(EcosystemQueries.PersonHasEnglish(nameEn, jobAr, jobEn, bioAr, bioEn),
            EcosystemQueries.PersonEnglish<Speaker>().Compile()(row));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("T", null)]
    [InlineData(null, "S")]
    [InlineData("T", "S")]
    [InlineData("", "S")]
    public void Episode_and_opportunity_english_rules_are_the_same_in_sql_and_in_code(string? titleEn, string? summaryEn)
    {
        Assert.Equal(EcosystemQueries.EpisodeHasEnglish(titleEn, summaryEn),
            EcosystemQueries.EpisodeEnglish.Compile()(new TvEpisode { TitleEn = titleEn, SummaryEn = summaryEn }));
        Assert.Equal(EcosystemQueries.OpportunityHasEnglish(titleEn, summaryEn),
            EcosystemQueries.OpportunityEnglish.Compile()(new InvestmentOpportunity { TitleEn = titleEn, SummaryEn = summaryEn }));
    }
}

/// <summary>Speakers, TV guests and episodes, opportunities and sponsorship packages: admin both ways, public pages, SEO.</summary>
[Collection("App")]
public class EcosystemTests(TestAppFactory f)
{
    private const string Base = "https://www.innovest-holding.com";

    private static JsonElement Graph(string html)
    {
        var m = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(m.Success, "no JSON-LD");
        return JsonDocument.Parse(m.Groups[1].Value).RootElement.Clone().GetProperty("@graph");
    }

    private static JsonElement Node(string html, string type) =>
        Graph(html).EnumerateArray().Single(n => n.GetProperty("@type").GetString() == type);

    /// <summary>No empty string, object or array anywhere in a JSON-LD node.</summary>
    private static void NoEmptyValues(JsonElement e, string path = "$")
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.String:
                Assert.False(string.IsNullOrWhiteSpace(e.GetString()), path + " is empty");
                break;
            case JsonValueKind.Object:
                Assert.NotEmpty(e.EnumerateObject());
                foreach (var p in e.EnumerateObject()) NoEmptyValues(p.Value, path + "." + p.Name);
                break;
            case JsonValueKind.Array:
                Assert.NotEqual(0, e.GetArrayLength());
                var i = 0;
                foreach (var x in e.EnumerateArray()) NoEmptyValues(x, $"{path}[{i++}]");
                break;
        }
    }

    private Task<int> AddAsync<T>(T entity) where T : class =>
        f.DbAsync(async db =>
        {
            db.Add(entity);
            await db.SaveChangesAsync();
            return (int)db.Entry(entity).Property("Id").CurrentValue!;
        });

    private Task<int> RemoveAsync<T>(int id) where T : class =>
        f.DbAsync(async db =>
        {
            if (await db.FindAsync<T>(id) is { } row) db.Remove(row);
            db.SlugRedirects.RemoveRange(db.SlugRedirects.Where(r => r.EntityId == id));
            return await db.SaveChangesAsync();
        });

    private async Task<List<string>> SitemapLocsAsync()
    {
        var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return xml.Root!.Elements(sm + "url").Select(u => u.Element(sm + "loc")!.Value).ToList();
    }

    private static string Slug(string prefix) => Unique(prefix).ToLowerInvariant();

    private static Speaker NewSpeaker(string slug, bool english, bool active = true) => new()
    {
        Slug = slug, NameAr = "متحدث " + slug, NameEn = english ? "Speaker " + slug : null,
        JobTitleAr = "مدير", JobTitleEn = english ? "Director" : null, OrganizationAr = "شركة", OrganizationEn = english ? "Company" : null,
        BioAr = "نبذة أولى\n\nنبذة ثانية", BioEn = english ? "First bio\n\nSecond bio" : null, IsActive = active, SortOrder = 900,
        LinkedInUrl = "https://www.linkedin.com/in/example",
    };

    private static TvGuest NewGuest(string slug, bool english, bool active = true) => new()
    {
        Slug = slug, NameAr = "ضيف " + slug, NameEn = english ? "Guest " + slug : null, JobTitleAr = "", BioAr = "",
        IsActive = active, SortOrder = 900,
    };

    private static TvEpisode NewEpisode(string slug, bool english, string? video = null, bool active = true) => new()
    {
        Slug = slug, TitleAr = "حلقة " + slug, TitleEn = english ? "Episode " + slug : null, SummaryAr = "ملخص الحلقة",
        SummaryEn = english ? "Episode summary" : null, Season = 2, Number = 5, AiredOn = new DateOnly(2026, 9, 1), VideoUrl = video,
        IsActive = active, SortOrder = 900,
    };

    private static InvestmentOpportunity NewOpportunity(string slug, bool english, bool active = true, bool open = true) => new()
    {
        Slug = slug, TitleAr = "فرصة " + slug, TitleEn = english ? "Opportunity " + slug : null, SummaryAr = "ملخص الفرصة",
        SummaryEn = english ? "Opportunity summary" : null, StageTextAr = "تأسيس", StageTextEn = english ? "Seed" : null,
        AskTextAr = "عند الطلب", AskTextEn = english ? "On request" : null, IsActive = active, IsOpen = open, SortOrder = 900,
    };

    // ── T-C3.1: admin both ways ──

    public static TheoryData<string> AdminLists => new() { "/Admin/Speakers", "/Admin/TvGuests", "/Admin/TvEpisodes", "/Admin/Opportunities", "/Admin/SponsorshipPackages" };

    [Theory, MemberData(nameof(AdminLists))] // anonymous → login; signed in → 200; POST without a token → 400 and nothing written
    public async Task Admin_screens_need_a_signed_in_admin_and_a_token(string path)
    {
        var anon = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, anon.StatusCode);
        Assert.Contains("/Account/Login", anon.Headers.Location!.OriginalString);
        var anonCreate = await f.NewClient().GetAsync(path + "/Create");
        Assert.Equal(HttpStatusCode.Redirect, anonCreate.StatusCode);

        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path + "/Create")).StatusCode);
        var title = Unique("no-token");
        var r = await client.PostAsync(path + "/Create", Form(("NameAr", title), ("TitleAr", title), ("SummaryAr", "x")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False(await f.DbAsync(db => db.Speakers.AnyAsync(s => s.NameAr == title)));
        Assert.False(await f.DbAsync(db => db.TvEpisodes.AnyAsync(s => s.TitleAr == title)));
    }

    [Fact] // a speaker created in admin (with a photo) is on the list at once and has its profile page; rename → 301; delete → 404
    public async Task Speaker_lifecycle_through_the_admin()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Speakers/Create");
        var nameEn = "Test Speaker " + Guid.NewGuid().ToString("N")[..8];
        var r = await client.PostAsync("/Admin/Speakers/Create", MediaUploadTests.Multipart(token,
            ("p.jpg", ImageInspectorTests.Fixture("photo-exif.jpg"), "image/jpeg"),
            ("NameAr", "متحدث تجريبي"), ("NameEn", nameEn), ("JobTitleAr", "مديرة"), ("JobTitleEn", "Director"),
            ("OrganizationAr", "جهة"), ("OrganizationEn", "Org"), ("BioAr", "نبذة"), ("BioEn", "Bio"),
            ("LinkedInUrl", "https://www.linkedin.com/in/x"), ("IsActive", "true")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var row = await f.DbAsync(db => db.Speakers.Include(s => s.Photo).SingleAsync(s => s.NameEn == nameEn));
        var slug = row.Slug;
        try
        {
            Assert.Equal(nameEn.ToLowerInvariant().Replace(' ', '-'), slug);
            Assert.NotNull(row.Photo);

            var list = await f.NewClient().GetStringAsync("/forum/speakers");
            Assert.Contains($"href=\"/forum/speakers/{slug}\"", list);
            Assert.Contains("src=\"/media/" + row.Photo!.FileKey, list);
            Assert.Contains($"href=\"/en/forum/speakers/{slug}\"", await f.NewClient().GetStringAsync("/en/forum/speakers"));

            var page = await f.NewClient().GetAsync("/en/forum/speakers/" + slug);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.False(page.Headers.Contains("X-Robots-Tag"));
            var html = await page.Content.ReadAsStringAsync();
            Assert.Single(Regex.Matches(html, "<h1[ >]"));
            Assert.Contains("href=\"https://www.linkedin.com/in/x\"", html);
            var person = Node(html, "Person");
            Assert.Equal(nameEn, person.GetProperty("name").GetString());
            Assert.Equal("Director", person.GetProperty("jobTitle").GetString());
            Assert.Equal("Org", person.GetProperty("worksFor").GetProperty("name").GetString());
            Assert.Equal($"{Base}/en/forum/speakers/{slug}#person", person.GetProperty("@id").GetString());
            NoEmptyValues(person);

            // Rename the slug → the old URL answers 301 to the new one (same language).
            var editToken = await client.GetTokenAsync($"/Admin/Speakers/Edit/{row.Id}");
            var renamed = slug + "-x";
            var edit = await client.PostAsync("/Admin/Speakers/Edit", MediaUploadTests.Multipart(editToken, null,
                ("Id", row.Id.ToString()), ("Slug", renamed), ("NameAr", "متحدث تجريبي"), ("NameEn", nameEn), ("IsActive", "true")));
            Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
            var old = await f.NewClient().GetAsync("/en/forum/speakers/" + slug);
            Assert.Equal(HttpStatusCode.MovedPermanently, old.StatusCode);
            Assert.Equal("/en/forum/speakers/" + renamed, old.Headers.Location!.OriginalString);
            slug = renamed;

            // Delete: the page is gone, the photo row too.
            var del = await client.PostAsync("/Admin/Speakers/Delete", Form(await client.GetTokenAsync("/Admin/Speakers"), ("id", row.Id.ToString())));
            Assert.Equal(HttpStatusCode.Redirect, del.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/forum/speakers/" + slug)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/forum/speakers/" + row.Slug)).StatusCode);
            Assert.False(await f.DbAsync(db => db.MediaFiles.AnyAsync(m => m.Id == row.PhotoId)));
        }
        finally
        {
            await RemoveAsync<Speaker>(row.Id);
        }
    }

    [Fact] // invalid input is refused with the form and nothing written: https only, required fields, unknown links, bad unit
    public async Task Admin_refuses_invalid_input()
    {
        var client = await f.SignedInClientAsync();
        var title = Unique("bad");
        foreach (var url in new[] { "javascript:alert(1)", "http://www.youtube.com/watch?v=dQw4w9WgXcQ" })
        {
            var t = await client.GetTokenAsync("/Admin/TvEpisodes/Create");
            var r = await client.PostAsync("/Admin/TvEpisodes/Create", MediaUploadTests.Multipart(t, null,
                ("TitleAr", title), ("SummaryAr", "ملخص"), ("VideoUrl", url)));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("field-error", await r.Content.ReadAsStringAsync());
        }
        var t2 = await client.GetTokenAsync("/Admin/TvEpisodes/Create");
        var unknown = await client.PostAsync("/Admin/TvEpisodes/Create", MediaUploadTests.Multipart(t2, null,
            ("TitleAr", title), ("SummaryAr", "ملخص"), ("GuestIds", "987654321")));
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.False(await f.DbAsync(db => db.TvEpisodes.AnyAsync(e => e.TitleAr == title)));

        var t3 = await client.GetTokenAsync("/Admin/Speakers/Create");
        var xUrl = await client.PostAsync("/Admin/Speakers/Create", MediaUploadTests.Multipart(t3, null, ("NameAr", title), ("XUrl", "http://x.com/a")));
        Assert.Equal(HttpStatusCode.OK, xUrl.StatusCode);
        Assert.False(await f.DbAsync(db => db.Speakers.AnyAsync(s => s.NameAr == title)));

        var t4 = await client.GetTokenAsync("/Admin/SponsorshipPackages/Create");
        var unit = await client.PostAsync("/Admin/SponsorshipPackages/Create", Form(t4, ("Unit", "radio"), ("TitleAr", title)));
        Assert.Equal(HttpStatusCode.OK, unit.StatusCode);
        Assert.False(await f.DbAsync(db => db.SponsorshipPackages.AnyAsync(p => p.TitleAr == title)));

        var t5 = await client.GetTokenAsync("/Admin/Opportunities/Create");
        var other = await f.DbAsync(db => db.ContentItems.Where(i => i.Group != ContentItemGroups.Sectors).Select(i => i.Id).FirstAsync());
        var sector = await client.PostAsync("/Admin/Opportunities/Create", MediaUploadTests.Multipart(t5, null,
            ("TitleAr", title), ("SummaryAr", "ملخص"), ("SectorItemId", other.ToString())));
        Assert.Equal(HttpStatusCode.OK, sector.StatusCode);
        Assert.False(await f.DbAsync(db => db.InvestmentOpportunities.AnyAsync(o => o.TitleAr == title)));
    }

    // ── T-C3.2: drafts are not public ──

    [Fact] // an opportunity created in admin is a draft (404); published → 200; closed → no interest action
    public async Task Opportunity_is_a_draft_until_published()
    {
        var client = await f.SignedInClientAsync();
        var sectorId = await f.DbAsync(db => db.ContentItems.Where(i => i.Group == ContentItemGroups.Sectors && i.IsActive).Select(i => i.Id).FirstAsync());
        var title = Unique("فرصة");
        var token = await client.GetTokenAsync("/Admin/Opportunities/Create");
        var r = await client.PostAsync("/Admin/Opportunities/Create", MediaUploadTests.Multipart(token, null,
            ("TitleAr", title), ("SummaryAr", "ملخص"), ("SectorItemId", sectorId.ToString()), ("StageTextAr", "نمو"),
            ("IsOpen", "true"), ("IsOpen", "false"), ("IsActive", "false")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var o = await f.DbAsync(db => db.InvestmentOpportunities.SingleAsync(x => x.TitleAr == title));
        try
        {
            Assert.False(o.IsActive);
            Assert.StartsWith("opportunity-", o.Slug);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/angel-network/opportunities/" + o.Slug)).StatusCode);
            Assert.DoesNotContain(o.Slug, await f.NewClient().GetStringAsync("/angel-network/opportunities"));

            await f.DbAsync(async db => { (await db.InvestmentOpportunities.FindAsync(o.Id))!.IsActive = true; return await db.SaveChangesAsync(); });
            var html = await f.NewClient().GetStringAsync("/angel-network/opportunities/" + o.Slug);
            Assert.Contains(title, html);
            Assert.Contains(Msg.Ar(Infrastructure.Localization.MessageKeys.Ui.ExpressInterest), html);
            Assert.Contains(Msg.Ar(Infrastructure.Localization.MessageKeys.Ui.OpportunityOpen), html);
            Assert.Contains("نمو", html);
            NoEmptyValues(Node(html, "WebPage"));

            await f.DbAsync(async db => { (await db.InvestmentOpportunities.FindAsync(o.Id))!.IsOpen = false; return await db.SaveChangesAsync(); });
            var closed = await f.NewClient().GetStringAsync("/angel-network/opportunities/" + o.Slug);
            Assert.Contains(Msg.Ar(Infrastructure.Localization.MessageKeys.Ui.OpportunityClosed), closed);
            Assert.DoesNotContain(Msg.Ar(Infrastructure.Localization.MessageKeys.Ui.ExpressInterest), closed);

            // A sector in use cannot be deleted (T-C3.6).
            var delToken = await client.GetTokenAsync("/Admin/ContentItems?group=sectors");
            var del = await client.PostAsync("/Admin/ContentItems/Delete", Form(delToken, ("id", sectorId.ToString())));
            Assert.Equal(HttpStatusCode.Redirect, del.StatusCode);
            Assert.True(await f.DbAsync(db => db.ContentItems.AnyAsync(i => i.Id == sectorId)));
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(o.Id);
        }
    }

    [Fact] // inactive speaker / guest / episode → 404 and absent from the lists
    public async Task Hidden_items_are_not_public()
    {
        var s = Slug("hidden-s");
        var g = Slug("hidden-g");
        var e = Slug("hidden-e");
        var ids = (await AddAsync(NewSpeaker(s, true, active: false)), await AddAsync(NewGuest(g, true, active: false)), await AddAsync(NewEpisode(e, true, active: false)));
        try
        {
            foreach (var (list, path) in new[] { ("/forum/speakers", "/forum/speakers/" + s), ("/tv/guests", "/tv/guests/" + g), ("/tv/episodes", "/tv/episodes/" + e) })
            {
                Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(path)).StatusCode);
                Assert.DoesNotContain(path, await f.NewClient().GetStringAsync(list));
            }
        }
        finally
        {
            await RemoveAsync<Speaker>(ids.Item1);
            await RemoveAsync<TvGuest>(ids.Item2);
            await RemoveAsync<TvEpisode>(ids.Item3);
        }
    }

    // ── T-C3.3: event ↔ speakers ──

    [Fact] // the event page lists its published speakers in the event's order (and as Event performers); deleting a speaker removes the link only
    public async Task Event_page_lists_its_speakers_in_order()
    {
        var a = Slug("spk-a");
        var b = Slug("spk-b");
        var hidden = Slug("spk-h");
        var idA = await AddAsync(NewSpeaker(a, true));
        var idB = await AddAsync(NewSpeaker(b, true));
        var idH = await AddAsync(NewSpeaker(hidden, true, active: false));
        var eventSlug = Slug("evt");
        var eventId = await AddAsync(new Event
        {
            Slug = eventSlug, TitleAr = "فعالية " + eventSlug, TitleEn = "Event " + eventSlug, DescriptionAr = "وصف", SummaryAr = "ملخص",
            EventDay = 3, EventMonthAr = "مارس", LocationAr = "القاهرة", TimeAr = "", IsActive = true, SortOrder = 900,
            StartsAt = new DateTimeOffset(2031, 3, 3, 10, 0, 0, TimeSpan.FromHours(2)),
        });
        try
        {
            await f.DbAsync(async db =>
            {
                db.EventSpeakers.AddRange(
                    new EventSpeaker { EventId = eventId, SpeakerId = idB, SortOrder = 0 },
                    new EventSpeaker { EventId = eventId, SpeakerId = idA, SortOrder = 1 },
                    new EventSpeaker { EventId = eventId, SpeakerId = idH, SortOrder = 2 });
                return await db.SaveChangesAsync();
            });
            var html = await f.NewClient().GetStringAsync("/forum/events/" + eventSlug);
            var posA = html.IndexOf($"href=\"/forum/speakers/{a}\"", StringComparison.Ordinal);
            var posB = html.IndexOf($"href=\"/forum/speakers/{b}\"", StringComparison.Ordinal);
            Assert.True(posB > 0 && posA > posB, $"order: b={posB} a={posA}");
            Assert.DoesNotContain(hidden, html);
            var performers = Node(html, "Event").GetProperty("performer").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
            Assert.Equal(["متحدث " + b, "متحدث " + a], performers);

            // The speaker's page lists the event.
            Assert.Contains($"href=\"/forum/events/{eventSlug}\"", await f.NewClient().GetStringAsync("/forum/speakers/" + a));

            // Delete speaker B in the admin: the link goes, the event stays.
            var client = await f.SignedInClientAsync();
            var del = await client.PostAsync("/Admin/Speakers/Delete", Form(await client.GetTokenAsync("/Admin/Speakers"), ("id", idB.ToString())));
            Assert.Equal(HttpStatusCode.Redirect, del.StatusCode);
            Assert.Equal(2, await f.DbAsync(db => db.EventSpeakers.CountAsync(es => es.EventId == eventId)));
            var after = await f.NewClient().GetStringAsync("/forum/events/" + eventSlug);
            Assert.DoesNotContain(b, after);
            Assert.Contains($"href=\"/forum/speakers/{a}\"", after);
        }
        finally
        {
            await RemoveAsync<Event>(eventId);
            await RemoveAsync<Speaker>(idA);
            await RemoveAsync<Speaker>(idB);
            await RemoveAsync<Speaker>(idH);
        }
    }

    // ── T-C3.4 / T-C3.7: episodes ──

    [Fact] // the episode page makes no request to YouTube before the click; TVEpisode + VideoObject JSON-LD; guests in the admin's order
    public async Task Episode_video_is_click_to_load_and_described_in_json_ld()
    {
        var g1 = Slug("guest-1");
        var g2 = Slug("guest-2");
        var idG1 = await AddAsync(NewGuest(g1, true));
        var idG2 = await AddAsync(NewGuest(g2, true));
        var client = await f.SignedInClientAsync();
        var title = Unique("حلقة");
        var token = await client.GetTokenAsync("/Admin/TvEpisodes/Create");
        var r = await client.PostAsync("/Admin/TvEpisodes/Create", MediaUploadTests.Multipart(token, null,
            ("TitleAr", title), ("TitleEn", "Episode " + g1), ("SummaryAr", "ملخص"), ("SummaryEn", "Summary"),
            ("Season", "2"), ("Number", "5"), ("AiredOn", "2026-09-01"), ("VideoUrl", "https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
            ("GuestIds", idG2.ToString()), ("GuestIds", idG1.ToString()), ("IsActive", "true")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var episode = await f.DbAsync(db => db.TvEpisodes.SingleAsync(e => e.TitleAr == title));
        try
        {
            Assert.Equal([idG2, idG1], await f.DbAsync(db => db.TvEpisodeGuests.Where(x => x.TvEpisodeId == episode.Id)
                .OrderBy(x => x.SortOrder).Select(x => x.TvGuestId).ToListAsync()));

            foreach (var path in new[] { "/tv/episodes/" + episode.Slug, "/en/tv/episodes/" + episode.Slug })
            {
                var html = await f.NewClient().GetStringAsync(path);
                Assert.Contains("data-video-id=\"dQw4w9WgXcQ\"", html);
                Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ytimg", html, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotMatch("(src|href|poster)=\"https?://[^\"]*youtube", html);
                var pos1 = html.IndexOf($"/tv/guests/{g1}\"", StringComparison.Ordinal);
                var pos2 = html.IndexOf($"/tv/guests/{g2}\"", StringComparison.Ordinal);
                Assert.True(pos2 > 0 && pos1 > pos2, $"guest order: g2={pos2} g1={pos1}");

                var node = Node(html, "TVEpisode");
                NoEmptyValues(node);
                Assert.Equal(5, node.GetProperty("episodeNumber").GetInt32());
                Assert.Equal(2, node.GetProperty("partOfSeason").GetProperty("seasonNumber").GetInt32());
                Assert.Equal("2026-09-01", node.GetProperty("datePublished").GetString());
                Assert.Equal($"{Base}/tv#tvseries", node.GetProperty("partOfSeries").GetProperty("@id").GetString());
                Assert.Equal(2, node.GetProperty("actor").GetArrayLength());
                var video = node.GetProperty("video");
                Assert.Equal("VideoObject", video.GetProperty("@type").GetString());
                Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", video.GetProperty("embedUrl").GetString());
                Assert.Contains("<meta property=\"og:type\" content=\"video.episode\">", html);
            }

            // The TV hub carries the series node the episodes point at.
            var hub = Node(await f.NewClient().GetStringAsync("/tv"), "TVSeries");
            Assert.Equal($"{Base}/tv#tvseries", hub.GetProperty("@id").GetString());

            // The guest's page lists the episode.
            Assert.Contains($"href=\"/tv/episodes/{episode.Slug}\"", await f.NewClient().GetStringAsync("/tv/guests/" + g1));

            // No video stored → no VideoObject, no player.
            await f.DbAsync(async db => { (await db.TvEpisodes.FindAsync(episode.Id))!.VideoUrl = null; return await db.SaveChangesAsync(); });
            var plain = await f.NewClient().GetStringAsync("/tv/episodes/" + episode.Slug);
            Assert.False(Node(plain, "TVEpisode").TryGetProperty("video", out _));
            Assert.DoesNotContain("data-video-id", plain);
        }
        finally
        {
            await RemoveAsync<TvEpisode>(episode.Id);
            await RemoveAsync<TvGuest>(idG1);
            await RemoveAsync<TvGuest>(idG2);
        }
    }

    // ── T-C3.5: indexability ──

    public static TheoryData<string, string> Lists => new()
    {
        { "/forum/speakers", "speaker" }, { "/tv/guests", "guest" }, { "/tv/episodes", "episode" }, { "/angel-network/opportunities", "opportunity" },
    };

    private async Task<int> AddOneAsync(string kind, string slug, bool english) => kind switch
    {
        "speaker" => await AddAsync(NewSpeaker(slug, english)),
        "guest" => await AddAsync(NewGuest(slug, english)),
        "episode" => await AddAsync(NewEpisode(slug, english)),
        _ => await AddAsync(NewOpportunity(slug, english)),
    };

    private Task RemoveOneAsync(string kind, int id) => kind switch
    {
        "speaker" => RemoveAsync<Speaker>(id),
        "guest" => RemoveAsync<TvGuest>(id),
        "episode" => RemoveAsync<TvEpisode>(id),
        _ => RemoveAsync<InvestmentOpportunity>(id),
    };

    private async Task<string?> RobotsAsync(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return r.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : null;
    }

    [Theory, MemberData(nameof(Lists))] // empty → noindex,follow and out of the sitemap; the first Arabic-only item indexes the Arabic list; English text indexes the English one
    public async Task List_becomes_indexable_with_its_first_published_item(string path, string kind)
    {
        Assert.Equal(0, await f.DbAsync(db => kind switch
        {
            "speaker" => db.Speakers.CountAsync(x => x.IsActive),
            "guest" => db.TvGuests.CountAsync(x => x.IsActive),
            "episode" => db.TvEpisodes.CountAsync(x => x.IsActive),
            _ => db.InvestmentOpportunities.CountAsync(x => x.IsActive),
        }));
        Assert.Equal("noindex, follow", await RobotsAsync(path));
        Assert.Equal("noindex, follow", await RobotsAsync("/en" + path));
        Assert.DoesNotContain(Base + path, await SitemapLocsAsync());

        var arSlug = Slug("ar-only");
        var arId = await AddOneAsync(kind, arSlug, english: false);
        int? enId = null;
        try
        {
            Assert.Null(await RobotsAsync(path));
            Assert.Equal("noindex, follow", await RobotsAsync("/en" + path));
            var detail = SiteDetail(path, arSlug);
            Assert.Null(await RobotsAsync(detail));
            Assert.Equal("noindex, follow", await RobotsAsync("/en" + detail)); // Arabic fallback, not indexed
            var en = await f.NewClient().GetStringAsync("/en" + detail);
            Assert.Contains($"<link rel=\"canonical\" href=\"{Base}{detail}\">", en);
            var locs = await SitemapLocsAsync();
            Assert.Contains(Base + path, locs);
            Assert.DoesNotContain(Base + "/en" + path, locs);
            Assert.Contains(Base + detail, locs);
            Assert.DoesNotContain(Base + "/en" + detail, locs);

            var enSlug = Slug("both");
            enId = await AddOneAsync(kind, enSlug, english: true);
            Assert.Null(await RobotsAsync("/en" + path));
            Assert.Null(await RobotsAsync("/en" + SiteDetail(path, enSlug)));
            locs = await SitemapLocsAsync();
            Assert.Contains(Base + "/en" + path, locs);
            Assert.Contains(Base + "/en" + SiteDetail(path, enSlug), locs);
        }
        finally
        {
            await RemoveOneAsync(kind, arId);
            if (enId is { } id) await RemoveOneAsync(kind, id);
        }
        Assert.Equal("noindex, follow", await RobotsAsync(path)); // the cache followed the deletes
    }

    private static string SiteDetail(string listPath, string slug) => listPath + "/" + slug;

    [Fact] // packages: TV ones on the TV sponsorship page, Forum ones on the Forum partners page, in both languages; inactive ones hidden
    public async Task Sponsorship_packages_are_listed_on_their_unit_page()
    {
        var tv = Unique("باقة-تلفزيون");
        var forum = Unique("باقة-منتدى");
        var off = Unique("باقة-معطلة");
        var ids = new[]
        {
            await AddAsync(new SponsorshipPackage { Unit = SponsorshipUnits.Tv, TitleAr = tv, TitleEn = "Gold TV", PriceTextAr = "عند الطلب", BenefitsAr = "ميزة 1\nميزة 2", BenefitsEn = "Benefit 1\nBenefit 2", IsActive = true }),
            await AddAsync(new SponsorshipPackage { Unit = SponsorshipUnits.Forum, TitleAr = forum, IsActive = true }),
            await AddAsync(new SponsorshipPackage { Unit = SponsorshipUnits.Tv, TitleAr = off, IsActive = false }),
        };
        try
        {
            var ar = await f.NewClient().GetStringAsync("/tv/sponsorship");
            Assert.Contains(tv, ar);
            Assert.Contains("<li>ميزة 2</li>", ar);
            Assert.DoesNotContain(forum, ar);
            Assert.DoesNotContain(off, ar);
            var en = await f.NewClient().GetStringAsync("/en/tv/sponsorship");
            Assert.Contains("Gold TV", en);
            Assert.Contains("<li>Benefit 1</li>", en);
            Assert.Contains("Sponsorship packages", en);
            Assert.Contains(forum, await f.NewClient().GetStringAsync("/forum/partners"));
            Assert.Contains($"lang=\"ar\" dir=\"rtl\">{forum}", await f.NewClient().GetStringAsync("/en/forum/partners")); // fallback marked
        }
        finally
        {
            foreach (var id in ids) await RemoveAsync<SponsorshipPackage>(id);
        }
    }
}
