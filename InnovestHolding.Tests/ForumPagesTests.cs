using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// F10 Dev 2 (ARCHITECTURE-F10 T7–T8): the forum hub's content blocks (ar + en, from the database, hide/move, empty →
/// nothing), the /forum/editions page (gallery, lazy images with their size, thin/noindex rules, sitemap), month-only
/// events (card, detail, WebPage JSON-LD, past), the thin-event rule and the registration choices (not-past events only).
/// Every test restores what it changed.
/// </summary>
[Collection("App")]
public class ForumPagesTests(TestAppFactory f)
{
    /// <summary>The section heading ids of the forum hub's content blocks, in the registry's default order.</summary>
    private static readonly string[] HubIds =
    [
        "sec-forum-about", "sec-forum_figures", "sec-forum_audiences", "sec-forum_pillars", "sec-forum_goals", "sec-forum_sectors",
        "sec-forum_components", "sec-forum-editions", "sec-forum-patronage", "sec-forum-contact",
    ];

    private async Task<(HttpResponseMessage Response, string Html)> GetAsync(string path)
    {
        var r = await f.NewClient().GetAsync(path);
        return (r, await r.Content.ReadAsStringAsync());
    }

    private async Task<string> PageAsync(string path)
    {
        var (r, html) = await GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return html;
    }

    private static List<string> Ids(string html, IEnumerable<string> known) =>
        Regex.Matches(html, "id=\"(sec-[a-z_-]+)\"").Select(m => m.Groups[1].Value).Where(known.Contains).ToList();

    /// <summary>The app's HTML encoder (all Unicode ranges allowed: Program.cs WebEncoderOptions).</summary>
    private static readonly System.Text.Encodings.Web.HtmlEncoder Encoder =
        System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

    private static string Enc(string value) => Encoder.Encode(value);

    private static string? Robots(HttpResponseMessage r) =>
        r.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : null;

    private async Task<List<string>> SitemapAsync()
    {
        var xml = XDocument.Parse(await f.NewClient().GetStringAsync("/sitemap.xml"));
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return xml.Root!.Elements(sm + "url").Select(u => new Uri(u.Element(sm + "loc")!.Value).AbsolutePath).ToList();
    }

    private Task Db(Func<Data.AppDbContext, Task> change) => f.DbAsync(async db =>
    {
        await change(db);
        return await db.SaveChangesAsync();
    });

    private async Task WithSectionsRestoredAsync(string page, Func<Task> body)
    {
        var saved = await f.DbAsync(db => db.PageSections.AsNoTracking().Where(r => r.PageKey == page)
            .Select(r => new { r.SectionKey, r.IsVisible, r.SortOrder }).ToListAsync());
        try { await body(); }
        finally
        {
            await Db(async db =>
            {
                foreach (var row in await db.PageSections.Where(r => r.PageKey == page).ToListAsync())
                    if (saved.FirstOrDefault(s => s.SectionKey == row.SectionKey) is { } s) { row.IsVisible = s.IsVisible; row.SortOrder = s.SortOrder; }
            });
        }
    }

    private Task SetSectionAsync(string page, string section, bool? visible = null, int? order = null) => Db(async db =>
    {
        var row = await db.PageSections.SingleAsync(r => r.PageKey == page && r.SectionKey == section);
        if (visible is { } v) row.IsVisible = v;
        if (order is { } o) row.SortOrder = o;
    });

    // ── T7: the forum hub ──

    [Theory]
    [InlineData("/forum", false)]
    [InlineData("/en/forum", true)]
    public async Task Hub_renders_every_forum_block_from_the_database_in_registry_order(string path, bool english)
    {
        var html = await PageAsync(path);
        Assert.Equal(HubIds, Ids(html, HubIds));

        // Values come from the migration's rows (SiteContents, ContentItems, partners, editions), in the page language.
        var keys = await f.DbAsync(db => db.SiteContents.AsNoTracking()
            .Where(s => s.SectionKey.StartsWith("forum_") || s.SectionKey.StartsWith("section_forum_"))
            .ToDictionaryAsync(s => s.SectionKey, s => (s.ContentAr, s.ContentEn)));
        string T(string key) => Enc(english && !string.IsNullOrWhiteSpace(keys[key].ContentEn) ? keys[key].ContentEn! : keys[key].ContentAr);
        Assert.Contains(T(ContentKeys.Forum.EditionLabel), html);
        Assert.Contains(T(ContentKeys.Forum.Tagline), html);
        Assert.Contains(T(ContentKeys.Forum.StrategicLinkTitle), html);
        Assert.Contains(T("section_forum_goals_note"), html);
        Assert.Contains(T(ContentKeys.Forum.ContactTitle), html);

        var figure = await f.DbAsync(db => db.ContentItems.AsNoTracking().Where(i => i.Group == ContentItemGroups.ForumFigures && i.IsActive)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id).FirstAsync());
        Assert.Contains($"<bdi>{Enc(english ? figure.TextEn! : figure.TextAr)}</bdi>", html); // the figure as stored

        var edition = await f.DbAsync(db => db.ForumEditions.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.SortOrder).ThenBy(e => e.Id).FirstAsync());
        Assert.Contains(Enc(english ? edition.TitleEn! : edition.TitleAr), html);
        Assert.Contains($"href=\"{(english ? "/en" : "")}/forum/editions\"", html); // the teaser's link to the page

        // The website is a link with rel noopener; the social handle is text only (never a link).
        Assert.Contains("href=\"https://www.innovest-arab.com\" rel=\"noopener noreferrer\"", html);
        Assert.Contains("<bdi dir=\"ltr\">innovest1</bdi>", html);
        Assert.DoesNotContain("href=\"innovest1", html);
        Assert.Contains("href=\"tel:&#x2B;201515204475\"", html); // "+" is encoded by the HTML encoder

        // Patronage: groups flagged for the hub only (برعاية, بتنظيم), not «بمشاركة» (the hidden ministries' group).
        var hub = html[html.IndexOf("sec-forum-patronage", StringComparison.Ordinal)..];
        hub = hub[..hub.IndexOf("</section>", StringComparison.Ordinal)];
        Assert.Contains(english ? "Powered by" : "بتنظيم", hub);
        Assert.DoesNotContain(english ? "With the Participation of" : "بمشاركة", hub);
    }

    [Fact]
    public async Task Hub_sections_hide_and_move_as_the_admin_sets_them()
    {
        await WithSectionsRestoredAsync(PageKeys.Forum, async () =>
        {
            await SetSectionAsync(PageKeys.Forum, "figures", visible: false);
            await SetSectionAsync(PageKeys.Forum, "contact", order: 1);
            await SetSectionAsync(PageKeys.Forum, "goals", order: 2);
            foreach (var path in new[] { "/forum", "/en/forum" })
            {
                var ids = Ids(await PageAsync(path), HubIds);
                Assert.DoesNotContain("sec-forum_figures", ids);
                Assert.Equal(["sec-forum-contact", "sec-forum_goals", "sec-forum-about"], ids.Take(3));
            }
        });
        // Restored: the default order again (negative control of the assertion above).
        Assert.Equal(HubIds, Ids(await PageAsync("/forum"), HubIds));
    }

    [Fact]
    public async Task An_empty_forum_list_renders_nothing_and_the_registration_page_shows_the_contact_block()
    {
        var goals = await f.DbAsync(db => db.ContentItems.Where(i => i.Group == ContentItemGroups.ForumGoals && i.IsActive).Select(i => i.Id).ToListAsync());
        Assert.NotEmpty(goals);
        try
        {
            await Db(async db => { foreach (var i in await db.ContentItems.Where(i => goals.Contains(i.Id)).ToListAsync()) i.IsActive = false; });
            var html = await PageAsync("/forum");
            Assert.DoesNotContain("sec-forum_goals", html);
            Assert.Contains("sec-forum_sectors", html);
        }
        finally
        {
            await Db(async db => { foreach (var i in await db.ContentItems.Where(i => goals.Contains(i.Id)).ToListAsync()) i.IsActive = true; });
        }
        Assert.Contains("sec-forum_goals", await PageAsync("/forum"));

        foreach (var path in new[] { "/forum/registration", "/en/forum/registration" })
            Assert.Contains("sec-forum-contact", await PageAsync(path));
        await WithSectionsRestoredAsync(PageKeys.ForumRegistration, async () =>
        {
            await SetSectionAsync(PageKeys.ForumRegistration, "contact", visible: false);
            Assert.DoesNotContain("sec-forum-contact", await PageAsync("/forum/registration"));
        });
    }

    // ── T7: /forum/editions ──

    [Fact]
    public async Task Editions_page_gallery_lazy_images_with_size_and_more_photos()
    {
        // Its own edition, first in order (the bundled editions already carry the photos imported at startup).
        var editionId = await f.DbAsync(async db =>
        {
            var e = new ForumEdition { Year = 2027, TitleAr = "نسخة اختبار المعرض", EditionLabelAr = "اختبار", SortOrder = -1000, IsActive = true };
            db.ForumEditions.Add(e);
            await db.SaveChangesAsync();
            return e.Id;
        });
        var photoIds = new List<int>();
        var mediaIds = new List<int>();
        try
        {
            await Db(async db =>
            {
                for (var i = 0; i < 6; i++)
                {
                    var m = new MediaFile { FileKey = Guid.NewGuid().ToString("N"), Format = "jpeg", Width = 1279, Height = 851, Bytes = 1, AltAr = i == 0 ? "صورة الافتتاح" : "", AltEn = i == 0 ? "Opening photo" : null };
                    db.MediaFiles.Add(m);
                    await db.SaveChangesAsync();
                    mediaIds.Add(m.Id);
                    var p = new ForumEditionPhoto { ForumEditionId = editionId, ImageId = m.Id, SortOrder = i + 1 };
                    db.ForumEditionPhotos.Add(p);
                    await db.SaveChangesAsync();
                    photoIds.Add(p.Id);
                }
            });

            var ar = await PageAsync("/forum/editions");
            Assert.Contains("class=\"forum-gallery\"", ar);
            Assert.Contains("alt=\"صورة الافتتاح\"", ar);
            Assert.Equal(6, Regex.Matches(ar, "width=\"1279\" height=\"851\"").Count);
            // The first edition's large photo is eager (likely the LCP); every other photo is lazy.
            Assert.Single(Regex.Matches(ar, "class=\"forum-gallery-main\"[^>]*fetchpriority=\"high\""));
            Assert.Equal(6, Regex.Matches(ar, "width=\"1279\" height=\"851\"[^>]*(loading=\"lazy\"|fetchpriority=\"high\")").Count);
            Assert.Equal(5, Regex.Matches(ar, "width=\"1279\" height=\"851\"[^>]*loading=\"lazy\"").Count);
            Assert.Contains("<details class=\"forum-gallery-more\">", ar);
            Assert.Contains(Msg.Ar(MessageKeys.Ui.EditionMorePhotos)!.Replace("{0}", "2"), ar);

            var en = await PageAsync("/en/forum/editions");
            Assert.Contains("alt=\"Opening photo\"", en);
            Assert.Contains(Msg.En(MessageKeys.Ui.EditionMorePhotos)!.Replace("{0}", "2"), en);

            // The hub's teaser shows the cover only, lazy.
            var hub = await PageAsync("/forum");
            Assert.Single(Regex.Matches(hub, "class=\"forum-edition-cover\"[^>]*alt=\"صورة الافتتاح\"[^>]*loading=\"lazy\""));
            Assert.DoesNotContain("forum-gallery", hub);
        }
        finally
        {
            await Db(async db =>
            {
                db.ForumEditionPhotos.RemoveRange(await db.ForumEditionPhotos.Where(p => photoIds.Contains(p.Id)).ToListAsync());
                await db.SaveChangesAsync();
                db.MediaFiles.RemoveRange(await db.MediaFiles.Where(m => mediaIds.Contains(m.Id)).ToListAsync());
                db.ForumEditions.RemoveRange(await db.ForumEditions.Where(e => e.Id == editionId).ToListAsync());
            });
        }
    }

    [Fact]
    public async Task Editions_page_is_thin_while_empty_and_indexable_with_one_edition_per_language()
    {
        var active = await f.DbAsync(db => db.ForumEditions.Where(e => e.IsActive).Select(e => e.Id).ToListAsync());
        Assert.NotEmpty(active);
        var first = active[0];
        var saved = await f.DbAsync(db => db.ForumEditions.AsNoTracking().Where(e => active.Contains(e.Id)).ToListAsync());
        var admin = await f.SignedInClientAsync();
        // Rows whose EN chip is complete (the page shows one chip per edition row).
        async Task<int> ChipsAsync() => Regex.Matches(await admin.GetStringAsync("/Admin/ForumEditions"), "badge-active en-chip\"").Count;
        try
        {
            // ONE rule (Hassan F10 🟡1): the migration's editions have an English title and no text in either language, so
            // they are English-complete — the admin shows EN ✓ on every row AND /en/forum/editions is indexable and listed.
            var (arR, _) = await GetAsync("/forum/editions");
            Assert.Null(Robots(arR));
            var (enR, _) = await GetAsync("/en/forum/editions");
            Assert.Null(Robots(enR));
            var sitemap = await SitemapAsync();
            Assert.Contains("/forum/editions", sitemap);
            Assert.Contains("/en/forum/editions", sitemap);
            Assert.Equal(active.Count, await ChipsAsync());

            // Arabic text without English on every edition: EN — everywhere in the admin AND the English page is thin.
            await Db(async db => { foreach (var e in await db.ForumEditions.Where(x => active.Contains(x.Id)).ToListAsync()) { e.TextAr = "نص عربي"; e.TextEn = null; } });
            Assert.Equal(0, await ChipsAsync());
            (enR, _) = await GetAsync("/en/forum/editions");
            Assert.Contains("noindex", Robots(enR));
            Assert.DoesNotContain("/en/forum/editions", await SitemapAsync());

            // One edition with its English text too → EN ✓ on that row and the English page is indexable and listed.
            await Db(async db => { var e = await db.ForumEditions.SingleAsync(x => x.Id == first); e.TitleEn = "QA Edition"; e.TextEn = "QA text."; });
            Assert.Equal(1, await ChipsAsync());
            (enR, var enHtml) = await GetAsync("/en/forum/editions");
            Assert.Null(Robots(enR));
            Assert.Contains("QA text.", enHtml);
            Assert.Contains("/en/forum/editions", await SitemapAsync());

            // No active edition → the admin's empty text, noindex, out of the sitemap (both languages).
            await Db(async db => { foreach (var e in await db.ForumEditions.Where(x => active.Contains(x.Id)).ToListAsync()) e.IsActive = false; });
            var (emptyR, emptyHtml) = await GetAsync("/forum/editions");
            Assert.Equal(HttpStatusCode.OK, emptyR.StatusCode);
            Assert.Contains("noindex", Robots(emptyR));
            Assert.Contains("class=\"empty-state\"", emptyHtml);
            Assert.DoesNotContain("sec-forum-editions", await PageAsync("/forum")); // the hub teaser disappears too
            sitemap = await SitemapAsync();
            Assert.DoesNotContain("/forum/editions", sitemap);
            Assert.DoesNotContain("/en/forum/editions", sitemap);
        }
        finally
        {
            await Db(async db =>
            {
                foreach (var e in await db.ForumEditions.Where(x => active.Contains(x.Id)).ToListAsync())
                {
                    var s = saved.Single(x => x.Id == e.Id);
                    (e.IsActive, e.TitleEn, e.TextAr, e.TextEn) = (true, s.TitleEn, s.TextAr, s.TextEn);
                }
            });
        }
    }

    [Fact]
    public async Task Editions_page_unpublished_is_404_and_listed_in_the_menu_when_published()
    {
        Assert.Contains("href=\"/forum/editions\"", await PageAsync("/forum/events")); // menus / unit tabs pick it up
        try
        {
            await Db(async db => (await db.Pages.SingleAsync(p => p.Key == PageKeys.ForumEditions)).IsPublished = false);
            Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/forum/editions")).Response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/en/forum/editions")).Response.StatusCode);
            Assert.DoesNotContain("href=\"/forum/editions\"", await PageAsync("/forum"));
        }
        finally
        {
            await Db(async db => (await db.Pages.SingleAsync(p => p.Key == PageKeys.ForumEditions)).IsPublished = true);
        }
    }

    // ── T8: month-only events, thin events, registration choices ──

    private async Task<int> AddEventAsync(string slug, string titleAr, DateTimeOffset starts, DateTimeOffset ends, bool monthOnly,
        string summaryAr = "", string? titleEn = null)
    {
        var e = new Event
        {
            Slug = slug, TitleAr = titleAr, TitleEn = titleEn, SummaryAr = summaryAr, EventDay = 1, EventMonthAr = "", StartsAt = starts,
            EndsAt = ends, DateIsMonth = monthOnly, IsActive = true, SortOrder = 9000, CityAr = "القاهرة", LocationAr = "القاهرة",
        };
        await Db(db => { db.Events.Add(e); return Task.CompletedTask; });
        return e.Id;
    }

    private Task RemoveEventsAsync(params int[] ids) => Db(async db => db.Events.RemoveRange(await db.Events.Where(e => ids.Contains(e.Id)).ToListAsync()));

    private static (DateTimeOffset Start, DateTimeOffset End) Month(DateTimeOffset at)
    {
        var start = new DateTimeOffset(at.Year, at.Month, 1, 0, 0, 0, TimeSpan.FromHours(2));
        return (start, start.AddMonths(1).AddMinutes(-1));
    }

    [Fact]
    public async Task Month_only_event_card_detail_and_json_ld()
    {
        var now = f.Time.GetUtcNow();
        // This month: upcoming (it ends at the month's end), so it is on the first page of the list.
        var (start, end) = Month(now);
        var slug = Unique("qa-month");
        var title = Unique("فعالية شهرية");
        var id = await AddEventAsync(slug, title, start, end, monthOnly: true, titleEn: "QA monthly event");
        try
        {
            var arMonth = EventDates.Month(start, false);
            var list = await PageAsync("/forum/events");
            var at = list.IndexOf("<a href=\"/forum/events/" + slug + "\"", StringComparison.Ordinal);
            Assert.True(at > 0, "the event card is on the first page of the list");
            var card = list[list.LastIndexOf("<article", at, StringComparison.Ordinal)..];
            card = card[..card.IndexOf("</article>", StringComparison.Ordinal)];
            Assert.Contains("event-date-box is-month", card);
            Assert.Contains($">{arMonth}</div>", card);
            Assert.Contains($">{start.Year}</div>", card);
            Assert.DoesNotContain("event-date-day\">1<", card); // no day digit
            Assert.DoesNotContain("UTC+", card);                 // no clock time

            var (r, html) = await GetAsync("/forum/events/" + slug);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains(EventDates.MonthYear(start, false), html);
            Assert.Contains("\"@type\":\"WebPage\"", html);
            Assert.DoesNotContain("\"@type\":\"Event\"", html);
            Assert.DoesNotContain("startDate", html);
            var en = await PageAsync("/en/forum/events/" + slug);
            Assert.Contains(EventDates.MonthYear(start, true), en);

            // Negative control: the same row with an exact date → Event markup with a startDate.
            await Db(async db => (await db.Events.SingleAsync(e => e.Id == id)).DateIsMonth = false);
            var exact = await PageAsync("/forum/events/" + slug);
            Assert.Contains("\"@type\":\"Event\"", exact);
            Assert.Contains("startDate", exact);
        }
        finally { await RemoveEventsAsync(id); }
    }

    [Fact]
    public async Task Month_only_event_is_past_after_its_month_and_upcoming_during_it()
    {
        var now = f.Time.GetUtcNow();
        var (pastStart, pastEnd) = Month(now.AddMonths(-2));
        var (curStart, curEnd) = Month(now);
        var past = await AddEventAsync(Unique("qa-past-month"), Unique("فعالية ماضية"), pastStart, pastEnd, true);
        var current = await AddEventAsync(Unique("qa-this-month"), Unique("فعالية هذا الشهر"), curStart, curEnd, true);
        try
        {
            var slugs = await f.DbAsync(db => db.Events.Where(e => e.Id == past || e.Id == current).ToDictionaryAsync(e => e.Id, e => e.Slug));
            var pastHtml = await PageAsync("/forum/events/" + slugs[past]);
            Assert.Contains($"detail-badge\">{Msg.Ar(MessageKeys.Ui.EventPast)}<", pastHtml);
            var currentHtml = await PageAsync("/forum/events/" + slugs[current]);
            Assert.DoesNotContain($"detail-badge\">{Msg.Ar(MessageKeys.Ui.EventPast)}<", currentHtml);
        }
        finally { await RemoveEventsAsync(past, current); }
    }

    [Fact]
    public async Task Event_without_summary_or_description_is_noindex_and_out_of_the_sitemap()
    {
        var now = f.Time.GetUtcNow();
        var (start, end) = Month(now.AddMonths(13));
        var slug = Unique("qa-thin");
        var id = await AddEventAsync(slug, Unique("فعالية بلا نص"), start, end, true);
        try
        {
            var (r, html) = await GetAsync("/forum/events/" + slug);
            Assert.Contains("noindex", Robots(r));
            Assert.Contains("<meta name=\"robots\"", html);
            Assert.DoesNotContain("/forum/events/" + slug, await SitemapAsync());

            // With an Arabic summary the page is indexable and listed (negative control of the rule).
            await Db(async db => (await db.Events.SingleAsync(e => e.Id == id)).SummaryAr = "ملخص الفعالية");
            (r, _) = await GetAsync("/forum/events/" + slug);
            Assert.Null(Robots(r));
            Assert.Contains("/forum/events/" + slug, await SitemapAsync());
        }
        finally { await RemoveEventsAsync(id); }
    }

    [Fact]
    public async Task Registration_choices_offer_only_events_that_have_not_passed_and_the_post_refuses_a_past_one()
    {
        var now = f.Time.GetUtcNow();
        var pastTitle = Unique("ملتقى انتهى");
        var futureTitle = Unique("ملتقى قادم");
        var (ps, pe) = Month(now.AddMonths(-3));
        var (fs, fe) = Month(now.AddMonths(3));
        var past = await AddEventAsync(Unique("qa-reg-past"), pastTitle, ps, pe, true);
        var future = await AddEventAsync(Unique("qa-reg-future"), futureTitle, fs, fe, true);
        try
        {
            foreach (var path in new[] { "/forum/registration", "/en/forum/registration" })
            {
                var html = await PageAsync(path);
                Assert.Contains(futureTitle, html);
                Assert.DoesNotContain(pastTitle, html);
            }

            var c = f.NewClient();
            var t = await c.GetTokenAsync("/forum/registration");
            (string, string)[] Fields(int eventId) =>
            [
                ("FullName", "QA Lead"), ("Email", Unique("reg") + "@example.com"), ("Consent", "true"),
                ("InterestOptionId", f.InterestId), ("EventId", eventId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ];
            var refused = await c.PostAsync("/forms/forum-registration", Form(t, Fields(past)));
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Equal("EventId", (await refused.JsonAsync()).GetProperty("field").GetString(), ignoreCase: true);
            var accepted = await c.PostAsync("/forms/forum-registration", Form(t, Fields(future)));
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }
        finally
        {
            await Db(async db => db.Registrations.RemoveRange(await db.Registrations.Where(r => r.EventId == past || r.EventId == future).ToListAsync()));
            await RemoveEventsAsync(past, future);
        }
    }
}
