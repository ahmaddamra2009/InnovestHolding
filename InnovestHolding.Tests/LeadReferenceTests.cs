using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// The C4 lead forms against C3's tables (DbLeadReferences): a posted opportunity id must name a published (IsActive) opportunity
/// that is open for interest (IsOpen); a package id an ACTIVE package of the posted unit. Anything else is refused with a 400
/// that names the field, and nothing is written (decision: refuse, never store silently without the reference — the page only
/// offers valid ids, so a different one is a stale page or a tampered post, and the visitor sees why). The reference is a
/// foreign key ON DELETE SET NULL: deleting the opportunity or package keeps the lead and its title snapshot.
/// </summary>
[Collection("App")]
public class LeadReferenceTests(TestAppFactory f)
{
    private static (string, string)[] Base(string email) => [("FullName", "QA Ref"), ("Email", email), ("Consent", "true")];

    private async Task<(HttpClient Client, string Token)> ClientAsync()
    {
        var client = f.NewClient();
        return (client, await client.GetTokenAsync("/angel-network/join"));
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

    private Task<int> Opportunity(string title, bool active = true, bool open = true) =>
        AddAsync(new InvestmentOpportunity { Slug = Unique("ref-opp")[..40], TitleAr = title, TitleEn = title + " EN", IsActive = active, IsOpen = open });

    private Task<int> Package(string unit, string title, bool active = true) =>
        AddAsync(new SponsorshipPackage { Unit = unit, TitleAr = title, TitleEn = title + " EN", IsActive = active });

    private static async Task AssertRefused(HttpResponseMessage r, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var json = await r.JsonAsync();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal(field, json.GetProperty("field").GetString(), ignoreCase: true);
    }

    private Task<int> Leads(string email) => f.DbAsync(async db =>
        await db.OpportunityInterests.CountAsync(x => x.Email == email) + await db.SponsorshipEnquiries.CountAsync(x => x.Email == email));

    [Fact] // a published, open opportunity and an active package of the posted unit are stored with their Arabic title snapshots
    public async Task Valid_opportunity_and_package_ids_are_stored_with_their_snapshots()
    {
        var oppTitle = Unique("فرصة");
        var pkgTitle = Unique("باقة");
        var opp = await Opportunity(oppTitle);
        var pkg = await Package(SponsorshipUnits.Tv, pkgTitle);
        try
        {
            var (c, t) = await ClientAsync();
            var email = Unique("ref-ok") + "@example.com";
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/en/forms/opportunity-interest", Form(t, [.. Base(email), ("OpportunityId", opp.ToString())]))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv"), ("PackageId", pkg.ToString())]))).StatusCode);

            var interest = await f.DbAsync(db => db.OpportunityInterests.AsNoTracking().SingleAsync(x => x.Email == email));
            Assert.Equal((opp, oppTitle), (interest.OpportunityId, interest.OpportunityTitle)); // Arabic snapshot, even from /en
            var enquiry = await f.DbAsync(db => db.SponsorshipEnquiries.AsNoTracking().SingleAsync(x => x.Email == email));
            Assert.Equal((pkg, pkgTitle, "tv"), (enquiry.PackageId, enquiry.PackageTitle, enquiry.Unit));
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(opp);
            await RemoveAsync<SponsorshipPackage>(pkg);
        }
    }

    [Fact] // draft, closed and unknown opportunities; inactive, other-unit and unknown packages: 400 naming the field, nothing written
    public async Task Ids_the_page_could_not_offer_are_refused_and_nothing_is_written()
    {
        var draft = await Opportunity(Unique("مسودة"), active: false);
        var closed = await Opportunity(Unique("مغلقة"), open: false);
        var inactive = await Package(SponsorshipUnits.Tv, Unique("معطلة"), active: false);
        var forum = await Package(SponsorshipUnits.Forum, Unique("منتدى"));
        try
        {
            var (c, t) = await ClientAsync();
            var email = Unique("ref-bad") + "@example.com";
            foreach (var id in new[] { draft, closed, int.MaxValue })
                await AssertRefused(await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("OpportunityId", id.ToString())])), "OpportunityId");
            foreach (var id in new[] { inactive, forum, int.MaxValue })   // forum: a real, active package of the OTHER unit
                await AssertRefused(await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv"), ("PackageId", id.ToString())])), "PackageId");
            // the same Forum package is accepted for its own unit (the refusal above is about the unit, not the row)
            Assert.Equal(0, await Leads(email));
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email), ("CompanyName", "Co"), ("Unit", "forum"), ("PackageId", forum.ToString())]))).StatusCode);
            Assert.Equal(1, await Leads(email));
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(draft);
            await RemoveAsync<InvestmentOpportunity>(closed);
            await RemoveAsync<SponsorshipPackage>(inactive);
            await RemoveAsync<SponsorshipPackage>(forum);
        }
    }

    [Fact] // FK ON DELETE SET NULL: the lead stays, the id becomes null, the title snapshot is kept; the admin sees «حُذفت»
    public async Task Deleting_the_opportunity_or_package_sets_the_reference_to_null()
    {
        var oppTitle = Unique("فرصة-تحذف");
        var pkgTitle = Unique("باقة-تحذف");
        var opp = await Opportunity(oppTitle);
        var pkg = await Package(SponsorshipUnits.Forum, pkgTitle);
        var (c, t) = await ClientAsync();
        var email = Unique("ref-del") + "@example.com";
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("OpportunityId", opp.ToString())]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email), ("CompanyName", "Co"), ("Unit", "forum"), ("PackageId", pkg.ToString())]))).StatusCode);
        var interestId = await f.DbAsync(db => db.OpportunityInterests.Where(x => x.Email == email).Select(x => x.Id).SingleAsync());

        var admin = await f.SignedInClientAsync();
        Assert.Contains($"<span class=\"lead-value\" dir=\"auto\">#{opp}</span>", await admin.GetStringAsync($"/Admin/Leads/Details/{interestId}?type=interest"));

        Assert.Equal(1, await RemoveAsync<InvestmentOpportunity>(opp));
        Assert.Equal(1, await RemoveAsync<SponsorshipPackage>(pkg));

        var interest = await f.DbAsync(db => db.OpportunityInterests.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Equal((null, oppTitle), (interest.OpportunityId, interest.OpportunityTitle));
        var enquiry = await f.DbAsync(db => db.SponsorshipEnquiries.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Equal((null, pkgTitle), (enquiry.PackageId, enquiry.PackageTitle));
        var detail = await admin.GetStringAsync($"/Admin/Leads/Details/{interestId}?type=interest");
        Assert.Contains(oppTitle, detail);
        Assert.Contains("<span class=\"lead-value\" dir=\"auto\">حُذفت</span>", detail);
    }

    [Fact] // the detail page's «express interest» link pre-selects its opportunity in the list page's form, in both languages
    public async Task The_interest_link_pre_selects_the_opportunity_and_the_form_offers_only_open_ones()
    {
        var title = Unique("فرصة-رابط");
        var opp = await Opportunity(title);
        var draft = await Opportunity(Unique("مسودة-رابط"), active: false);
        var closed = await Opportunity(Unique("مغلقة-رابط"), open: false);
        try
        {
            var slug = await f.DbAsync(db => db.InvestmentOpportunities.Where(o => o.Id == opp).Select(o => o.Slug).SingleAsync());
            foreach (var prefix in new[] { "", "/en" })
            {
                var detail = await f.NewClient().GetStringAsync($"{prefix}/angel-network/opportunities/{slug}");
                var href = $"{prefix}/angel-network/opportunities?opportunity={opp}#interest-section";
                Assert.Contains($"href=\"{href}\"", detail);

                var list = await f.NewClient().GetStringAsync($"{prefix}/angel-network/opportunities?opportunity={opp}");
                var select = Regex.Match(list, "<select id=\"interest-opportunity\" name=\"opportunityId\"[^>]*>(.*?)</select>", RegexOptions.Singleline);
                Assert.True(select.Success, "no opportunity select");
                Assert.Matches($"<option value=\"{opp}\" selected=\"selected\"[^>]*>{(prefix == "" ? title : title + " EN")}</option>", select.Value);
                Assert.DoesNotContain($"value=\"{draft}\"", select.Value);
                Assert.DoesNotContain($"value=\"{closed}\"", select.Value);
            }
            // without the parameter (or with one the page could not offer) nothing is pre-selected
            foreach (var q in new[] { "", $"?opportunity={draft}", "?opportunity=abc" })
                Assert.DoesNotContain("selected=\"selected\"", Regex.Match(await f.NewClient().GetStringAsync("/angel-network/opportunities" + q),
                    "<select id=\"interest-opportunity\".*?</select>", RegexOptions.Singleline).Value);
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(opp);
            await RemoveAsync<InvestmentOpportunity>(draft);
            await RemoveAsync<InvestmentOpportunity>(closed);
        }
    }

    [Fact] // Ziad R1/R2: the GET selects come from the cached snapshot — no package query on /tv/sponsorship, one opportunities
           // query (the list) on /angel-network/opportunities — and still follow IsActive / IsOpen after every save
    public async Task Form_selects_come_from_the_snapshot_and_follow_saves()
    {
        var sink = new CapturingLoggerProvider();
        await using var app = f.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(sink)));
        HttpClient Client()
        {
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());
            return client;
        }
        // Edits through this host's own context, so its snapshot sees the save (the interceptor of that host).
        async Task<int> Db(Func<Data.AppDbContext, Task> change)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            await change(db);
            return await db.SaveChangesAsync();
        }
        var opp = new InvestmentOpportunity { Slug = Unique("snap-opp")[..40], TitleAr = Unique("فرصة-لقطة"), IsActive = true, IsOpen = true };
        var package = new SponsorshipPackage { Unit = "tv", TitleAr = Unique("باقة-لقطة"), IsActive = true };
        await Db(db => { db.AddRange(opp, package); return Task.CompletedTask; });
        static string Select(string html, string id) =>
            Regex.Match(html, $"<select id=\"{id}\"[^>]*>(.*?)</select>", RegexOptions.Singleline).Value;
        List<string> Commands() => sink.Lines.Where(l => l.StartsWith("Microsoft.EntityFrameworkCore.Database.Command|Executed DbCommand")).ToList();
        try
        {
            foreach (var prefix in new[] { "", "/en" })
            {
                await Client().GetStringAsync(prefix + "/angel-network/opportunities");   // warms the snapshot
                sink.Lines.Clear();
                var list = await Client().GetStringAsync(prefix + "/angel-network/opportunities");
                Assert.Contains($"value=\"{opp.Id}\"", Select(list, "interest-opportunity"));
                Assert.Single(Commands(), l => l.Contains("[InvestmentOpportunities]"));   // the paged list only

                await Client().GetStringAsync(prefix + "/tv/sponsorship");
                sink.Lines.Clear();
                var sponsorship = await Client().GetStringAsync(prefix + "/tv/sponsorship");
                Assert.Contains($"value=\"{package.Id}\"", Select(sponsorship, "sponsor-package"));
                Assert.Empty(Commands());   // warm and anonymous: no query at all (C3 level)
            }

            // Closing the opportunity and deactivating the package drop them from the selects at once.
            await Db(async db =>
            {
                (await db.InvestmentOpportunities.FindAsync(opp.Id))!.IsOpen = false;
                (await db.SponsorshipPackages.FindAsync(package.Id))!.IsActive = false;
            });
            Assert.DoesNotContain($"value=\"{opp.Id}\"", Select(await Client().GetStringAsync("/angel-network/opportunities"), "interest-opportunity"));
            Assert.DoesNotContain($"value=\"{package.Id}\"", Select(await Client().GetStringAsync("/tv/sponsorship"), "sponsor-package"));

            // Re-opened but unpublished: still not offered.
            await Db(async db =>
            {
                var o = (await db.InvestmentOpportunities.FindAsync(opp.Id))!;
                o.IsOpen = true;
                o.IsActive = false;
            });
            Assert.DoesNotContain($"value=\"{opp.Id}\"", Select(await Client().GetStringAsync("/angel-network/opportunities"), "interest-opportunity"));
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(opp.Id);
            await RemoveAsync<SponsorshipPackage>(package.Id);
        }
    }

    [Fact] // the CSV carries the reference title and id under resx headers (admin requests are Arabic)
    public async Task The_csv_export_carries_the_reference()
    {
        var title = Unique("فرصة-csv");
        var opp = await Opportunity(title);
        try
        {
            var (c, t) = await ClientAsync();
            var email = Unique("ref-csv") + "@example.com";
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("OpportunityId", opp.ToString())]))).StatusCode);
            var csv = await (await f.SignedInClientAsync()).GetStringAsync("/Admin/Leads/Export?type=interest");
            Assert.Contains($"\"{Msg.Ar(MessageKeys.Admin.CsvReference)}\",\"{Msg.Ar(MessageKeys.Admin.CsvReferenceId)}\"\r\n", csv);
            var line = csv.Split("\r\n").Single(l => l.Contains(email));
            Assert.EndsWith($"\"{title}\",\"{opp}\"", line);
        }
        finally
        {
            await RemoveAsync<InvestmentOpportunity>(opp);
        }
    }
}
