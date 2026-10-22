using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>News and event detail pages: slugs, redirects, the English fallback rule, JSON-LD and the sitemap.</summary>
[Collection("App")]
public class DetailPageTests(TestAppFactory f)
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

    private Task<int> AddAsync<T>(T entity) where T : class =>
        f.DbAsync(async db =>
        {
            db.Add(entity);
            await db.SaveChangesAsync();
            return (int)db.Entry(entity).Property("Id").CurrentValue!;
        });

    private Task RemoveAsync<T>(int id) where T : class =>
        f.DbAsync(async db =>
        {
            db.Remove((await db.FindAsync<T>(id))!);
            db.SlugRedirects.RemoveRange(db.SlugRedirects.Where(r => r.EntityId == id));
            return await db.SaveChangesAsync();
        });

    private static News NewsItem(string slug, bool english, bool active = true) => new()
    {
        Slug = slug, TitleAr = "خبر " + slug, SummaryAr = "ملخص عربي", BodyAr = "فقرة أولى\n\nفقرة ثانية",
        TitleEn = english ? "News " + slug : null, SummaryEn = english ? "English summary" : null,
        CategoryAr = "عام", NewsDateAr = "1 مايو 2026", IsActive = active, SortOrder = 500,
        PublishedOn = new DateOnly(2026, 5, 1),
    };

    [Fact] // existing items got news-{id} / event-{id}: their detail pages answer in both languages
    public async Task Backfilled_slugs_answer_in_both_languages()
    {
        foreach (var path in new[] { "/news/news-1", "/en/news/news-1", "/forum/events/event-1", "/en/forum/events/event-1" })
        {
            var r = await f.NewClient().GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.False(r.Headers.Contains("X-Robots-Tag"), path);
            var html = await r.Content.ReadAsStringAsync();
            Assert.Single(Regex.Matches(html, "<h1[ >]"));
            Assert.Contains($"<link rel=\"canonical\" href=\"{Base}{path}\">", html);
            Assert.Contains("<link rel=\"alternate\" hreflang=\"en\"", html);
        }
        var en = await f.NewClient().GetStringAsync("/en/news/news-1");
        Assert.Contains("<html lang=\"en\" dir=\"ltr\"", en);
        Assert.Contains("Innovest Holding signs a strategic partnership", en);
        // The list links to the page and the breadcrumb ends with the item.
        Assert.Contains("href=\"/news/news-1\"", await f.NewClient().GetStringAsync("/news"));
        Assert.Contains("href=\"/en/forum/events/event-1\"", await f.NewClient().GetStringAsync("/en/forum/events"));
    }

    [Fact] // CTO decision 2: no English text → the English URL renders the Arabic fallback, noindex, canonical to Arabic
    public async Task English_page_without_english_text_is_noindex_with_arabic_canonical()
    {
        var slug = Unique("ar-only").ToLowerInvariant();
        var id = await AddAsync(NewsItem(slug, english: false));
        try
        {
            var r = await f.NewClient().GetAsync("/en/news/" + slug);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("noindex, follow", string.Join(",", r.Headers.GetValues("X-Robots-Tag")));
            var html = await r.Content.ReadAsStringAsync();
            Assert.Contains("<meta name=\"robots\" content=\"noindex, follow\">", html);
            Assert.Contains($"<link rel=\"canonical\" href=\"{Base}/news/{slug}\">", html);
            Assert.DoesNotContain("<link rel=\"alternate\"", html);
            Assert.Contains("lang=\"ar\" dir=\"rtl\">خبر " + slug, html); // fallback marked
            Assert.Contains("Arabic only", html);

            var ar = await f.NewClient().GetAsync("/news/" + slug);
            Assert.False(ar.Headers.Contains("X-Robots-Tag"));
            Assert.DoesNotContain("<link rel=\"alternate\"", await ar.Content.ReadAsStringAsync());

            var locs = await SitemapLocsAsync();
            Assert.Contains($"{Base}/news/{slug}", locs);
            Assert.DoesNotContain($"{Base}/en/news/{slug}", locs);
        }
        finally
        {
            await RemoveAsync<News>(id);
        }
    }

    [Fact] // English text present → both URLs in the sitemap with the hreflang triple
    public async Task Bilingual_item_is_paired_in_the_sitemap()
    {
        var slug = Unique("both").ToLowerInvariant();
        var id = await AddAsync(NewsItem(slug, english: true));
        try
        {
            var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
            XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xh = "http://www.w3.org/1999/xhtml";
            foreach (var loc in new[] { $"{Base}/news/{slug}", $"{Base}/en/news/{slug}" })
            {
                var url = xml.Root!.Elements(sm + "url").Single(u => u.Element(sm + "loc")!.Value == loc);
                Assert.Equal(3, url.Elements(xh + "link").Count());
                Assert.NotNull(url.Element(sm + "lastmod"));
            }
        }
        finally
        {
            await RemoveAsync<News>(id);
        }
    }

    private async Task<List<string>> SitemapLocsAsync()
    {
        var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return xml.Root!.Elements(sm + "url").Select(u => u.Element(sm + "loc")!.Value).ToList();
    }

    [Theory] // unknown, malformed and unpublished items are the branded 404
    [InlineData("/news/no-such-item")]
    [InlineData("/en/forum/events/no-such-event")]
    [InlineData("/news/bad--slug")]
    [InlineData("/news/-x")]
    public async Task Unknown_slugs_are_404(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("noindex, nofollow", string.Join(",", r.Headers.GetValues("X-Robots-Tag")));
    }

    [Theory] // an unpublished news item or event is a 404 in both languages and absent from the sitemap
    [InlineData("news")]
    [InlineData("event")]
    public async Task Inactive_item_is_404_and_not_in_the_sitemap(string kind)
    {
        var slug = Unique("hidden").ToLowerInvariant();
        var news = kind == "news";
        var id = news
            ? await AddAsync(NewsItem(slug, english: true, active: false))
            : await AddAsync(new Event
            {
                Slug = slug, TitleAr = "فعالية مخفية", TitleEn = "Hidden", SummaryEn = "Summary", DescriptionAr = "وصف",
                EventDay = 1, EventMonthAr = "مايو", IsActive = false, RegistrationOpen = true,
            });
        var path = (news ? "/news/" : "/forum/events/") + slug;
        try
        {
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/en" + path)).StatusCode);
            var locs = await SitemapLocsAsync();
            Assert.DoesNotContain($"{Base}{path}", locs);
            Assert.DoesNotContain($"{Base}/en{path}", locs);
        }
        finally
        {
            if (news) await RemoveAsync<News>(id); else await RemoveAsync<Event>(id);
        }
    }

    [Fact] // other spellings of a detail URL: one 301 to the canonical lower-case path
    public async Task Upper_case_detail_url_is_redirected_once()
    {
        var r = await f.NewClient().GetAsync("/News/News-1/");
        Assert.Equal(HttpStatusCode.MovedPermanently, r.StatusCode);
        Assert.Equal("/news/news-1", r.Headers.Location!.OriginalString);
        r = await f.NewClient().GetAsync("/EN/Forum/Events/Event-1");
        Assert.Equal("/en/forum/events/event-1", r.Headers.Location!.OriginalString);
    }

    [Fact] // a slug change keeps the old URL working (301, same language); the old slug is never given to another item
    public async Task Renamed_slug_redirects_and_cannot_be_reused()
    {
        var client = await f.SignedInClientAsync();
        var oldSlug = Unique("old").ToLowerInvariant();
        var newSlug = Unique("new").ToLowerInvariant();
        var id = await AddAsync(NewsItem(oldSlug, english: true));
        try
        {
            var token = await client.GetTokenAsync($"/Admin/News/Edit/{id}");
            var edit = await client.PostAsync("/Admin/News/Edit", MediaUploadTests.Multipart(token, null,
                ("Id", id.ToString()), ("TitleAr", "خبر"), ("Slug", newSlug), ("IsActive", "true"), ("SortOrder", "500")));
            Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);

            var r = await f.NewClient().GetAsync("/news/" + oldSlug);
            Assert.Equal(HttpStatusCode.MovedPermanently, r.StatusCode);
            Assert.Equal("/news/" + newSlug, r.Headers.Location!.OriginalString);
            r = await f.NewClient().GetAsync("/en/news/" + oldSlug);
            Assert.Equal("/en/news/" + newSlug, r.Headers.Location!.OriginalString);
            // An old slug written with capitals or a trailing slash: ONE hop, straight to the current slug.
            foreach (var (path, target) in new[] { ($"/news/{oldSlug}/", $"/news/{newSlug}"), ($"/EN/News/{oldSlug.ToUpperInvariant()}", $"/en/news/{newSlug}") })
            {
                r = await f.NewClient().GetAsync(path);
                Assert.Equal(HttpStatusCode.MovedPermanently, r.StatusCode);
                Assert.Equal(target, r.Headers.Location!.OriginalString);
                Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync(target)).StatusCode);
            }
            Assert.Equal(HttpStatusCode.OK, (await f.NewClient().GetAsync("/news/" + newSlug)).StatusCode);

            // Another item may take neither the current slug nor the old one.
            foreach (var taken in new[] { oldSlug, newSlug })
            {
                token = await client.GetTokenAsync("/Admin/News/Create");
                var title = Unique("dup");
                var create = await client.PostAsync("/Admin/News/Create", MediaUploadTests.Multipart(token, null,
                    ("TitleAr", title), ("Slug", taken), ("IsActive", "true")));
                Assert.Equal(HttpStatusCode.OK, create.StatusCode);
                Assert.Contains("مستخدم", WebUtility.HtmlDecode(await create.Content.ReadAsStringAsync()));
                Assert.False(await f.DbAsync(db => db.News.AnyAsync(n => n.TitleAr == title)));
            }

            // A malformed or reserved slug is refused.
            foreach (var bad in new[] { "Bad Slug", "admin", "x_y", "ar" })
            {
                token = await client.GetTokenAsync($"/Admin/News/Edit/{id}");
                var refused = await client.PostAsync("/Admin/News/Edit", MediaUploadTests.Multipart(token, null,
                    ("Id", id.ToString()), ("TitleAr", "خبر"), ("Slug", bad), ("IsActive", "true"), ("SortOrder", "500")));
                Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
            }
            Assert.Equal(newSlug, await f.DbAsync(db => db.News.Where(n => n.Id == id).Select(n => n.Slug).SingleAsync()));

            // Deleting the item removes its redirects: the old URL is then a 404.
            token = await client.GetTokenAsync("/Admin/News");
            await client.PostAsync("/Admin/News/Delete", Form(token, ("id", id.ToString())));
            Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync("/news/" + oldSlug)).StatusCode);
            Assert.False(await f.DbAsync(db => db.SlugRedirects.AnyAsync(s => s.EntityId == id && s.EntityType == SlugTypes.News)));
        }
        finally
        {
            if (await f.DbAsync(db => db.News.AnyAsync(n => n.Id == id))) await RemoveAsync<News>(id);
        }
    }

    [Fact] // an admin-created item without a typed slug gets one from its English title (or news-{id})
    public async Task Created_items_get_a_slug()
    {
        var client = await f.SignedInClientAsync();
        var en = "Forum " + Guid.NewGuid().ToString("N")[..8] + " Opens!";
        var token = await client.GetTokenAsync("/Admin/News/Create");
        await client.PostAsync("/Admin/News/Create", MediaUploadTests.Multipart(token, null, ("TitleAr", en + " ع"), ("TitleEn", en), ("IsActive", "true")));
        var arOnly = Unique("بلا-إنجليزي");
        token = await client.GetTokenAsync("/Admin/News/Create");
        await client.PostAsync("/Admin/News/Create", MediaUploadTests.Multipart(token, null, ("TitleAr", arOnly), ("IsActive", "true")));
        var rows = await f.DbAsync(db => db.News.Where(n => n.TitleAr == en + " ع" || n.TitleAr == arOnly).Select(n => new { n.Id, n.TitleAr, n.Slug }).ToListAsync());
        try
        {
            Assert.Equal(en.ToLowerInvariant().Replace(" ", "-").TrimEnd('!'), rows.Single(r => r.TitleAr != arOnly).Slug);
            var ar = rows.Single(r => r.TitleAr == arOnly);
            Assert.Equal("news-" + ar.Id, ar.Slug);
        }
        finally
        {
            foreach (var r in rows) await RemoveAsync<News>(r.Id);
        }
    }

    [Fact] // NewsArticle: headline, dates, publisher, the page's canonical; breadcrumb ends with the item
    public async Task News_detail_carries_a_newsarticle()
    {
        var html = await f.NewClient().GetStringAsync("/en/news/news-1");
        var article = Node(html, "NewsArticle");
        Assert.Equal("Innovest Holding signs a strategic partnership with leading technology companies", article.GetProperty("headline").GetString());
        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}", article.GetProperty("datePublished").GetString()!);
        Assert.EndsWith("Z", article.GetProperty("dateModified").GetString()!);
        Assert.Equal($"{Base}/#organization", article.GetProperty("publisher").GetProperty("@id").GetString());
        Assert.Equal($"{Base}/en/news/news-1", article.GetProperty("mainEntityOfPage").GetString());
        Assert.Equal("en", article.GetProperty("inLanguage").GetString());
        var crumbs = Node(html, "BreadcrumbList").GetProperty("itemListElement").EnumerateArray().ToList();
        Assert.Equal(3, crumbs.Count);
        Assert.False(crumbs[^1].TryGetProperty("item", out _));
        Assert.Contains("<meta property=\"og:type\" content=\"article\">", html);
    }

    [Fact] // Event: dates with the event's own offset, status, mode, place; past events lose the register button
    public async Task Event_detail_carries_dates_with_offset_and_its_state()
    {
        var future = new Event
        {
            Slug = Unique("ev").ToLowerInvariant(), TitleAr = "منتدى", TitleEn = "Forum", SummaryEn = "Summary", DescriptionAr = "وصف",
            StartsAt = new DateTimeOffset(2030, 11, 5, 10, 0, 0, TimeSpan.FromHours(3)),
            EndsAt = new DateTimeOffset(2030, 11, 5, 17, 30, 0, TimeSpan.FromHours(3)),
            Status = EventStatus.Postponed, AttendanceMode = AttendanceMode.Mixed, OnlineUrl = "https://example.org/live",
            LocationAr = "قاعة", LocationEn = "Hall A", CityAr = "الرياض", CityEn = "Riyadh", EventDay = 5, EventMonthAr = "نوفمبر",
            RegistrationOpen = true, IsActive = true, SortOrder = 900,
        };
        var past = new Event
        {
            Slug = Unique("past").ToLowerInvariant(), TitleAr = "ملتقى انتهى", DescriptionAr = "وصف",
            StartsAt = new DateTimeOffset(2020, 1, 2, 9, 0, 0, TimeSpan.FromHours(2)), EventDay = 2, EventMonthAr = "يناير",
            RegistrationOpen = true, IsActive = true, SortOrder = 0,
        };
        var futureId = await AddAsync(future);
        var pastId = await AddAsync(past);
        try
        {
            var html = WebUtility.HtmlDecode(await f.NewClient().GetStringAsync("/en/forum/events/" + future.Slug));
            var e = Node(html, "Event");
            Assert.Equal("2030-11-05T10:00:00+03:00", e.GetProperty("startDate").GetString());
            Assert.Equal("2030-11-05T17:30:00+03:00", e.GetProperty("endDate").GetString());
            Assert.Equal("https://schema.org/EventPostponed", e.GetProperty("eventStatus").GetString());
            Assert.Equal("https://schema.org/MixedEventAttendanceMode", e.GetProperty("eventAttendanceMode").GetString());
            Assert.Equal(2, e.GetProperty("location").GetArrayLength());
            Assert.Contains("10:00 – 17:30 (UTC+03:00)", html);
            Assert.Contains("Tuesday 5 November 2030", html);
            Assert.Contains("href=\"/en/forum/registration\"", ActionCard(html));

            var pastHtml = await f.NewClient().GetStringAsync("/forum/events/" + past.Slug);
            Assert.Contains("انتهت هذه الفعالية", pastHtml);
            Assert.DoesNotContain("href=\"/forum/registration\"", ActionCard(pastHtml));

            // The list shows upcoming events before past ones, whatever their admin order.
            var list = await f.NewClient().GetStringAsync("/forum/events");
            Assert.True(list.IndexOf(future.Slug, StringComparison.Ordinal) < list.IndexOf(past.Slug, StringComparison.Ordinal));
        }
        finally
        {
            await RemoveAsync<Event>(futureId);
            await RemoveAsync<Event>(pastId);
        }
    }

    private static string ActionCard(string html)
    {
        var start = html.IndexOf("class=\"detail-card\"", StringComparison.Ordinal);
        Assert.True(start > 0, "no action card");
        return html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];
    }

    [Fact] // the free-text day/month of older events: no Event node (no fabricated start date)
    public async Task Event_without_a_date_is_not_described_as_an_event()
    {
        var html = await f.NewClient().GetStringAsync("/forum/events/event-1");
        Assert.DoesNotContain(Graph(html).EnumerateArray(), n => n.GetProperty("@type").GetString() == "Event");
        Assert.Contains("15 يوليو", html);
    }
}
