using System.Net;
using System.Reflection;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Migrations;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// F10 polish (Dev 1): Hassan 🟡1/🔵2 one "English complete" rule; QA F10-QA-1 venue once; F10-QA-2 forum section counts;
/// F10-QA-3 the Arabic figures' plus; F10-QA-4 no "Arabic only" promise without Arabic text; O1/O2 the forum's full name
/// (seeded values only). The admin-chip ↔ public-page agreement is in ForumPagesTests (editions SEO test).
/// </summary>
[Collection("App")]
public class ForumPolishTests(TestAppFactory f)
{
    [Fact] // 🟡1/🔵2 — the SQL expression and the in-memory rule are the same rule
    public void English_rule_expression_equals_the_function()
    {
        var compiled = ForumEditionQueries.HasEnglish.Compile();
        string?[] values = [null, "", "  ", "x"];
        var cases = 0;
        foreach (var title in values)
            foreach (var textAr in values)
                foreach (var textEn in values)
                {
                    var e = new ForumEdition { TitleEn = title, TextAr = textAr ?? "", TextEn = textEn };
                    Assert.Equal(ForumEditionQueries.IsEnglishComplete(title, textAr ?? "", textEn), compiled(e));
                    cases++;
                }
        Assert.Equal(64, cases);
        Assert.True(ForumEditionQueries.IsEnglishComplete("Title", "", null));      // title only, no text anywhere
        Assert.False(ForumEditionQueries.IsEnglishComplete("Title", "نص", null));   // Arabic text without English
        Assert.False(ForumEditionQueries.IsEnglishComplete(null, "", null));
    }

    private async Task<int> AddEventAsync(Event e) => await f.DbAsync(async db => { db.Events.Add(e); await db.SaveChangesAsync(); return e.Id; });

    private Task RemoveEventAsync(int id) => f.DbAsync(async db =>
    {
        db.Events.Remove(await db.Events.SingleAsync(x => x.Id == id));
        return await db.SaveChangesAsync();
    });

    [Fact] // F10-QA-1 — a venue typed again as the city is shown once; the programme events carry the place in Location only
    public async Task Venue_is_shown_once()
    {
        var slug = Unique("venue").ToLowerInvariant();
        var id = await AddEventAsync(new Event
        {
            Slug = slug, TitleAr = "فعالية المكان", TitleEn = "Venue event", DescriptionAr = "وصف", DescriptionEn = "Text",
            StartsAt = new DateTimeOffset(2031, 3, 4, 10, 0, 0, TimeSpan.FromHours(2)), EventDay = 4, EventMonthAr = "مارس",
            LocationAr = "القاهرة", LocationEn = "Cairo", CityAr = "القاهرة", CityEn = "Cairo", RegistrationOpen = true, IsActive = true,
        });
        try
        {
            var ar = await f.NewClient().GetStringAsync("/forum/events/" + slug);
            var en = await f.NewClient().GetStringAsync("/en/forum/events/" + slug);
            Assert.DoesNotContain("القاهرة — القاهرة", ar);
            Assert.DoesNotContain("Cairo — Cairo", en);
            Assert.Contains("Cairo", en);
        }
        finally
        {
            await RemoveEventAsync(id);
        }

        var programme = await f.DbAsync(db => db.Events.AsNoTracking().Where(e => e.DateIsMonth)
            .Select(e => new { e.LocationAr, e.CityAr, e.CityEn }).ToListAsync());
        Assert.Equal(9, programme.Count);
        Assert.All(programme, p => Assert.True(p.CityAr == "" && p.CityEn == null));
        Assert.Equal(4, programme.Count(p => p.LocationAr != ""));
        var bahrain = await f.NewClient().GetStringAsync("/en/forum/events/2026-11-regional-summit-bahrain");
        Assert.Contains("Bahrain", bahrain);
        Assert.DoesNotContain("Bahrain — Bahrain", bahrain);
    }

    [Fact] // F10-QA-4 — the "Arabic only" notice only when the Arabic page has text the English page lacks
    public async Task Arabic_only_notice_needs_arabic_text()
    {
        var notice = Msg.En(MessageKeys.Ui.ArabicOnlyNotice);
        Assert.DoesNotContain(notice, await f.NewClient().GetStringAsync("/en/forum/events/2026-08-innovest-upper-egypt-forum"));

        var slug = Unique("arabic-only").ToLowerInvariant();
        var id = await AddEventAsync(new Event
        {
            Slug = slug, TitleAr = "فعالية بنص عربي", SummaryAr = "ملخص عربي فقط", DescriptionAr = "",
            StartsAt = new DateTimeOffset(2031, 5, 6, 10, 0, 0, TimeSpan.FromHours(2)), EventDay = 6, EventMonthAr = "مايو",
            RegistrationOpen = true, IsActive = true,
        });
        try
        {
            Assert.Contains(notice, await f.NewClient().GetStringAsync("/en/forum/events/" + slug));
        }
        finally
        {
            await RemoveEventAsync(id);
        }
    }

    [Fact] // F10-QA-2 — the sections screen counts the forum's own lists (6 sectors), the company pages keep theirs
    public async Task Forum_section_counts_use_the_forum_lists()
    {
        var snapshot = await f.Services.GetRequiredService<ISiteContentCache>().GetAsync();
        var count = typeof(PageSectionsController).GetMethod("ContentCount", BindingFlags.NonPublic | BindingFlags.Static)!;
        int? Count(string page, string section) => (int?)count.Invoke(null, [snapshot, page, section]);
        Assert.Equal(snapshot.ItemsOf(ContentItemGroups.ForumSectors).Count, Count(PageKeys.Forum, "sectors"));
        Assert.Equal(6, Count(PageKeys.Forum, "sectors"));
        Assert.Equal(snapshot.ItemsOf(ContentItemGroups.Sectors).Count, Count(PageKeys.About, "sectors"));
        Assert.NotEqual(Count(PageKeys.About, "sectors"), Count(PageKeys.Forum, "sectors"));
        Assert.Equal(6, Count(PageKeys.Forum, "figures"));
        Assert.Equal(3, Count(PageKeys.Forum, "audiences"));
        Assert.Equal(6, Count(PageKeys.Forum, "pillars"));
        Assert.Equal(5, Count(PageKeys.Forum, "goals"));
    }

    [Fact] // F10-QA-3 — the Arabic figures keep the plus on the reading-start (right) side, as the PDF and the bullets
    public async Task Arabic_figures_put_the_plus_after_the_number()
    {
        var figures = await f.DbAsync(db => db.ContentItems.Where(i => i.Group == ContentItemGroups.ForumFigures)
            .OrderBy(i => i.SortOrder).Select(i => i.TextAr).ToListAsync());
        Assert.Equal(["2000+", "459", "300+", "58+", "420 مليون", "50"], figures);
        var hub = await f.NewClient().GetStringAsync("/forum");
        // Razor encodes "+" as &#x2B;.
        Assert.Matches("<bdi>2000(\\+|&#x2B;)</bdi>", hub);
        Assert.DoesNotMatch("<bdi>(\\+|&#x2B;)2000</bdi>", hub);
    }

    [Fact] // O1/O2 — the home forum card and the hub's English title use the forum's full name (fresh install = seeder)
    public async Task Forum_full_name_on_the_home_card_and_the_hub_title()
    {
        var home = await f.NewClient().GetStringAsync("/");
        Assert.Contains(AddForumContent.ForumFullNameAr, home);
        Assert.DoesNotContain("منتدى الابتكار والاستثمار العربي", home);
        Assert.Contains(AddForumContent.ForumFullNameEn, await f.NewClient().GetStringAsync("/en"));
        Assert.Contains("<title>" + System.Net.WebUtility.HtmlEncode(AddForumContent.HubMetaTitleEn) + " | ", await f.NewClient().GetStringAsync("/en/forum"));
    }

    [Fact] // O1/O2 in the migration: a seeded value is renamed, an admin's edit is never overwritten, Down restores
    public async Task Naming_updates_change_only_seeded_values()
    {
        await using var db = MigrationTests.NewContext();
        await db.Database.EnsureDeletedAsync();
        var (before, target) = ForumContentMigrationTests.Ids(db);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(before);
        // The forum card as the company profile seeded it (an empty database has no platforms: insert it as seeded).
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Platforms (TitleAr, TitleEn, DescriptionAr, DescriptionEn, IconSvg, SortOrder, IsActive, ShowInFooter) VALUES (N'منتدى إنوفست', N'Innovest Forum', {0}, {1}, N'', 6, 1, 1)",
            AddForumContent.CardComponentsAr, "Admin wrote this");
        Assert.Equal("Innovest Forum for Investment", await db.Pages.Where(p => p.Key == "forum").Select(p => p.MetaTitleEn).SingleAsync());

        await migrator.MigrateAsync(target);
        var card = await db.Platforms.AsNoTracking().Where(p => p.TitleAr == "منتدى إنوفست").Select(p => new { p.DescriptionAr, p.DescriptionEn }).SingleAsync();
        Assert.Contains(AddForumContent.ForumFullNameAr, card.DescriptionAr);
        Assert.DoesNotContain("منتدى الابتكار والاستثمار العربي", card.DescriptionAr);
        Assert.Equal("Admin wrote this", card.DescriptionEn); // the admin's edit is kept
        Assert.Equal(AddForumContent.HubMetaTitleEn, await db.Pages.Where(p => p.Key == "forum").Select(p => p.MetaTitleEn).SingleAsync());

        await migrator.MigrateAsync(before);
        Assert.Equal(AddForumContent.CardComponentsAr, await db.Platforms.Where(p => p.TitleAr == "منتدى إنوفست").Select(p => p.DescriptionAr).SingleAsync());
        Assert.Equal("Innovest Forum for Investment", await db.Pages.Where(p => p.Key == "forum").Select(p => p.MetaTitleEn).SingleAsync());
    }
}
