using System.Collections.Concurrent;
using System.Net;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>The lead forms (Forum registration, Join the Network, sponsorship, opportunity interest): POST endpoints.</summary>
[Collection("App")]
public class LeadFormTests(TestAppFactory f)
{
    private const string Ok = "OK";

    private async Task<(HttpClient Client, string Token)> ClientAsync(bool english = false, string? ip = null)
    {
        var client = f.NewClient(ip);
        return (client, await client.GetTokenAsync(english ? "/en/angel-network/join" : "/angel-network/join"));
    }

    private Task<int> Group(string group, int index) => f.DbAsync(db => db.ContentItems.Where(i => i.Group == group && i.IsActive)
        .OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Skip(index).Select(i => i.Id).FirstAsync());

    private Task<int> ActiveEvent() => f.DbAsync(db => db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).Select(e => e.Id).FirstAsync());

    private static (string, string)[] Base(string email, string name = "QA Lead") =>
        [("FullName", name), ("Email", email), ("Consent", "true")];

    private static async Task<string> Message(HttpResponseMessage r) => (await r.JsonAsync()).GetProperty("message").GetString()!;

    private static async Task<string?> Field(HttpResponseMessage r) =>
        (await r.JsonAsync()).TryGetProperty("field", out var v) ? v.GetString() : null;

    private static async Task AssertRefused(HttpResponseMessage r, string? field = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var json = await r.JsonAsync();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));
        if (field is not null) Assert.Equal(field, json.GetProperty("field").GetString(), ignoreCase: true);
    }

    // ── Valid submissions write one row with the server-written fields and the Arabic snapshots ──

    [Fact]
    public async Task Forum_registration_writes_a_forum_row_with_event_and_interest_snapshots()
    {
        var (c, t) = await ClientAsync(english: true);
        var email = Unique("forum") + "@example.com";
        var eventId = await ActiveEvent();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var r = await c.PostAsync("/en/forms/forum-registration", Form(t, [.. Base(email),
            ("Phone", "+20 100"), ("Organization", "QA Org"), ("JobTitle", "Tester"), ("Country", "Egypt"),
            ("InterestOptionId", f.InterestId), ("EventId", eventId.ToString()), ("IsRead", "true"), ("Kind", "quick")]));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.ForumRegistrationSuccess), await Message(r));

        var row = await f.DbAsync(db => db.Registrations.AsNoTracking().SingleAsync(x => x.Email == email));
        var evt = await f.DbAsync(db => db.Events.AsNoTracking().SingleAsync(e => e.Id == eventId));
        var option = await f.DbAsync(db => db.InterestOptions.AsNoTracking().SingleAsync(o => o.Id == int.Parse(f.InterestId)));
        Assert.Equal(Registration.Forum, row.Kind);      // over-posted Kind ignored
        Assert.False(row.IsRead);                        // over-posted IsRead ignored
        Assert.Equal("en", row.Culture);
        Assert.Equal(eventId, row.EventId);
        Assert.Equal(evt.TitleAr, row.EventTitle);
        Assert.Equal(option.NameAr, row.Interest);
        Assert.Equal(("QA Org", "Tester", "Egypt"), (row.Organization, row.JobTitle, row.Country));
        Assert.NotNull(row.ConsentAt);
        Assert.InRange(row.ConsentAt!.Value, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Join_as_investor_stores_the_investor_fields_only()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("inv") + "@example.com";
        var type = await Group(ContentItemGroups.InvestorTypes, 1);
        var ticket = await Group(ContentItemGroups.TicketSizes, 0);
        var s1 = await Group(ContentItemGroups.Sectors, 0);
        var s2 = await Group(ContentItemGroups.Sectors, 2);
        var stage = await Group(ContentItemGroups.StartupStages, 0);
        var r = await c.PostAsync("/forms/join", Form(t, [.. Base(email), ("Kind", "investor"), ("Country", "UAE"),
            ("InvestorTypeId", type.ToString()), ("TicketSizeId", ticket.ToString()), ("SectorIds", s1.ToString()), ("SectorIds", s2.ToString()),
            ("SectorIds", s1.ToString()), ("Organization", "QA Capital"), ("LinkedInUrl", "https://www.linkedin.com/in/qa"),
            // startup fields posted too (both sets without JavaScript): ignored for an investor
            ("StartupName", "Ignored"), ("StageId", stage.ToString()), ("Pitch", "ignored")]));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(Msg.Ar(MessageKeys.JoinSuccess), await Message(r));

        var row = await f.DbAsync(db => db.NetworkApplications.AsNoTracking().SingleAsync(x => x.Email == email));
        var texts = await f.DbAsync(db => db.ContentItems.AsNoTracking().Where(i => new[] { type, ticket, s1, s2 }.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.TextAr));
        Assert.Equal(NetworkApplication.Investor, row.Kind);
        Assert.Equal(texts[type], row.InvestorType);
        Assert.Equal(texts[ticket], row.TicketSize);
        Assert.Equal(texts[s1] + "، " + texts[s2], row.Sectors);   // duplicates removed, order kept
        Assert.Equal("QA Capital", row.Organization);
        Assert.Null(row.Stage);
        Assert.Null(row.Pitch);
        Assert.Equal("ar", row.Culture);
        Assert.Equal("UAE", row.Country);
    }

    [Fact]
    public async Task Join_as_startup_stores_the_startup_fields_and_requires_its_own()
    {
        var (c, t) = await ClientAsync(english: true);
        var email = Unique("st") + "@example.com";
        var sector = await Group(ContentItemGroups.Sectors, 3);
        var stage = await Group(ContentItemGroups.StartupStages, 1);
        var funding = await Group(ContentItemGroups.FundingRanges, 2);
        (string, string)[] Body(string? name) => [.. Base(email), ("Kind", "startup"), ("Country", "Egypt"), ("StartupName", name ?? ""),
            ("SectorId", sector.ToString()), ("StageId", stage.ToString()), ("FundingId", funding.ToString()),
            ("WebsiteUrl", "https://startup.example"), ("Pitch", new string('p', 1000))];

        var missing = await c.PostAsync("/en/forms/join", Form(t, Body(null)));
        await AssertRefused(missing, "StartupName");
        Assert.Equal(Msg.En(MessageKeys.Required, Msg.En(MessageKeys.Fields.StartupName)), await Message(missing));

        var r = await c.PostAsync("/en/forms/join", Form(t, Body("QA Startup")));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var row = await f.DbAsync(db => db.NetworkApplications.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Equal((NetworkApplication.Startup, "QA Startup", "en"), (row.Kind, row.Organization, row.Culture));
        Assert.NotNull(row.Sectors);
        Assert.NotNull(row.Stage);
        Assert.NotNull(row.FundingSought);
        Assert.Equal(1000, row.Pitch!.Length);
        Assert.Null(row.InvestorType);
    }

    [Fact]
    public async Task Sponsorship_and_general_interest_are_stored()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("sp") + "@example.com";
        var sp = await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email, "Contact Person"), ("CompanyName", "QA Co"), ("Unit", "forum"), ("Message", "hi")]));
        Assert.Equal(HttpStatusCode.OK, sp.StatusCode);
        Assert.Equal(Msg.Ar(MessageKeys.SponsorshipSuccess), await Message(sp));
        var row = await f.DbAsync(db => db.SponsorshipEnquiries.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Equal(("forum", "QA Co", "Contact Person", "ar"), (row.Unit, row.CompanyName, row.ContactName, row.Culture));
        Assert.Null(row.PackageId);

        var (e, te) = await ClientAsync(english: true);
        var interest = await e.PostAsync("/en/forms/opportunity-interest", Form(te, Base(email)));
        Assert.Equal(HttpStatusCode.OK, interest.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.InterestSuccess), await Message(interest));
        var lead = await f.DbAsync(db => db.OpportunityInterests.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Null(lead.OpportunityId);
        Assert.Equal("en", lead.Culture);
    }

    // ── Refusals ──

    public static TheoryData<string> LeadPaths => ["/forms/forum-registration", "/forms/join", "/forms/sponsorship", "/forms/opportunity-interest"];

    private static (string, string)[] ValidBody(string path, string email, string extra1, string extra2) => path switch
    {
        "/forms/join" => [.. Base(email), ("Kind", "investor"), ("Country", "Egypt"), ("InvestorTypeId", extra1)],
        "/forms/sponsorship" => [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv")],
        _ => Base(email),
    };

    [Theory, MemberData(nameof(LeadPaths))]
    public async Task Honeypot_answers_success_and_writes_nothing(string path)
    {
        var (c, t) = await ClientAsync();
        var email = Unique("bot") + "@example.com";
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        var r = await c.PostAsync(path, Form(t, [.. ValidBody(path, email, type, ""), ("Website", "http://spam")]));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.True((await r.JsonAsync()).GetProperty("success").GetBoolean());
        Assert.Equal(0, await LeadCount(email));
    }

    [Theory, MemberData(nameof(LeadPaths))]
    public async Task Missing_consent_is_refused_in_the_page_language(string path)
    {
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        var email = Unique("nc") + "@example.com";
        var (c, t) = await ClientAsync();
        var ar = await c.PostAsync(path, Form(t, ValidBody(path, email, type, "").Where(p => p.Item1 != "Consent").ToArray()));
        await AssertRefused(ar, "Consent");
        Assert.Equal(Msg.Ar(MessageKeys.ConsentRequired), await Message(ar));

        var (e, te) = await ClientAsync(english: true);
        var en = await e.PostAsync("/en" + path, Form(te, [.. ValidBody(path, email, type, "").Where(p => p.Item1 != "Consent"), ("Consent", "false")]));
        await AssertRefused(en, "Consent");
        Assert.Equal(Msg.En(MessageKeys.ConsentRequired), await Message(en));
        Assert.Equal(0, await LeadCount(email));
    }

    [Theory, MemberData(nameof(LeadPaths))]
    public async Task No_antiforgery_token_is_400_and_writes_nothing(string path)
    {
        var c = f.NewClient();
        var email = Unique("csrf") + "@example.com";
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        var r = await c.PostAsync(path, Form(ValidBody(path, email, type, "")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(0, await LeadCount(email));
    }

    [Fact]
    public async Task Lengths_at_the_column_limit_pass_and_one_more_is_refused()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("len") + "@example.com";
        await AssertRefused(await c.PostAsync("/forms/forum-registration", Form(t, [.. Base(email), ("Organization", new string('o', 201))])), "Organization");
        await AssertRefused(await c.PostAsync("/forms/sponsorship", Form(t, [.. Base(email), ("CompanyName", new string('c', 201)), ("Unit", "tv")])), "CompanyName");
        await AssertRefused(await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("Message", new string('m', 2001))])), "Message");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/forum-registration", Form(t, [.. Base(email), ("Organization", new string('o', 200)),
            ("JobTitle", new string('j', 150)), ("Country", new string('c', 100))]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("Message", new string('m', 2000))]))).StatusCode);
        Assert.Equal(1, await f.DbAsync(db => db.Registrations.CountAsync(r => r.Email == email)));
        Assert.Equal(1, await f.DbAsync(db => db.OpportunityInterests.CountAsync(r => r.Email == email)));
    }

    [Fact]
    public async Task Tampered_choices_and_types_are_refused()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("tamper") + "@example.com";
        var inactiveEvent = await f.DbAsync(async db =>
        {
            var e = new Event { TitleAr = "فعالية متوقفة", DescriptionAr = "", EventMonthAr = "", LocationAr = "", TimeAr = "", IsActive = false, CreatedAt = DateTime.UtcNow };
            db.Events.Add(e);
            await db.SaveChangesAsync();
            return e.Id;
        });
        var sector = (await Group(ContentItemGroups.Sectors, 0)).ToString();
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();

        await AssertRefused(await c.PostAsync("/forms/forum-registration", Form(t, [.. Base(email), ("EventId", inactiveEvent.ToString())])), "EventId");
        await AssertRefused(await c.PostAsync("/forms/forum-registration", Form(t, [.. Base(email), ("InterestOptionId", "999999")])), "InterestOptionId");
        // an id of ANOTHER group (a sector as the investor type)
        var (c2, t2) = await ClientAsync();
        await AssertRefused(await c2.PostAsync("/forms/join", Form(t2, [.. Base(email), ("Kind", "investor"), ("Country", "Egypt"), ("InvestorTypeId", sector)])), "InvestorTypeId");
        await AssertRefused(await c2.PostAsync("/forms/join", Form(t2, [.. Base(email), ("Kind", "admin"), ("Country", "Egypt"), ("InvestorTypeId", type)])), "Kind");
        await AssertRefused(await c2.PostAsync("/forms/join", Form(t2, [.. Base(email), ("Kind", "investor"), ("Country", "Egypt"), ("InvestorTypeId", type),
            ("LinkedInUrl", "javascript:alert(1)")])), "LinkedInUrl");
        var (c3, t3) = await ClientAsync();
        await AssertRefused(await c3.PostAsync("/forms/sponsorship", Form(t3, [.. Base(email), ("CompanyName", "Co"), ("Unit", "news")])), "Unit");
        // packages and opportunities arrive in a later phase: until then no id is known
        await AssertRefused(await c3.PostAsync("/forms/sponsorship", Form(t3, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv"), ("PackageId", "1")])), "PackageId");
        await AssertRefused(await c3.PostAsync("/forms/opportunity-interest", Form(t3, [.. Base(email), ("OpportunityId", "1")])), "OpportunityId");
        var en = await c3.PostAsync("/en/forms/opportunity-interest", Form(t3, [.. Base(email), ("OpportunityId", "1")]));
        Assert.Equal(Msg.En(MessageKeys.OptionInvalid, Msg.En(MessageKeys.Fields.Opportunity)), await Message(en));
        Assert.Equal(0, await LeadCount(email));
    }

    [Fact] // one bucket per action for /forms/x and /en/forms/x; the 429 is JSON in the page language
    public async Task Rate_limit_is_per_action_shared_by_both_languages_and_answers_json()
    {
        var ip = NewIp();
        var (ar, tAr) = await ClientAsync(ip: ip);
        var (en, tEn) = await ClientAsync(english: true, ip: ip);
        var email = Unique("rl") + "@example.com";
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/forms/sponsorship", Form(tAr, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv")]))).StatusCode);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await en.PostAsync("/en/forms/sponsorship", Form(tEn, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv")]))).StatusCode);
        var limited = await en.PostAsync("/en/forms/sponsorship", Form(tEn, [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv")]));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.TooManyRequests), await Message(limited));
        // another action of the same IP has its own bucket
        Assert.Equal(HttpStatusCode.OK, (await ar.PostAsync("/forms/opportunity-interest", Form(tAr, Base(email)))).StatusCode);
        Assert.Equal(5, await f.DbAsync(db => db.SponsorshipEnquiries.CountAsync(s => s.Email == email)));
    }

    [Theory] // exact pins: two POST URLs per action, each with the JSON-429 marker, the rate limit and the 32 KB cap
    [InlineData("ForumRegistration", "forum-registration")]
    [InlineData("Join", "join")]
    [InlineData("Sponsorship", "sponsorship")]
    [InlineData("OpportunityInterest", "opportunity-interest")]
    public void Each_lead_action_has_exactly_its_two_pinned_routes(string action, string name)
    {
        var endpoints = f.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is { ControllerName: "Forms" } a && a.ActionName == action)
            .ToList();
        Assert.Equal(["/en/forms/" + name, "/forms/" + name], endpoints.Select(e => "/" + e.RoutePattern.RawText!.TrimStart('/')).Order());
        Assert.All(endpoints, e =>
        {
            Assert.Equal(["POST"], e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
            Assert.NotNull(e.Metadata.GetMetadata<JsonFormEndpointAttribute>());
            Assert.Equal(RateLimitPolicies.PublicForm, e.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
            Assert.Equal(InputLimits.PublicBodyBytes, e.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>()?.MaxRequestBodySize);
        });
        Assert.Contains(name, CultureUrls.FormNames);
    }

    [Fact] // a lead is not page content: submitting one keeps the cached snapshot
    public async Task A_lead_does_not_invalidate_the_site_snapshot()
    {
        var cache = f.Services.GetRequiredService<ISiteContentCache>();
        var (c, t) = await ClientAsync();
        var email = Unique("cache") + "@example.com";
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        foreach (var path in new[] { "/forms/forum-registration", "/forms/join", "/forms/sponsorship", "/forms/opportunity-interest" })
        {
            var before = await cache.GetAsync();
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsync(path, Form(t, ValidBody(path, email, type, "")))).StatusCode);
            Assert.Same(before, await cache.GetAsync());
        }
        Assert.Equal(4, await LeadCount(email));
    }

    [Fact] // no log line of a submission carries the visitor's name, email or phone
    public async Task Personal_data_is_never_logged()
    {
        var sink = new CapturingLoggerProvider();
        await using var app = f.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(sink).SetMinimumLevel(LogLevel.Trace)));
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());
        var token = await client.GetTokenAsync("/angel-network/join");
        var name = Unique("Name");
        var email = Unique("pii") + "@example.com";
        var phone = "+20 155 " + Random.Shared.Next(1000000, 9999999);
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        sink.Lines.Clear();
        foreach (var path in new[] { "/forms/forum-registration", "/forms/join", "/forms/sponsorship", "/forms/opportunity-interest" })
        {
            var body = ValidBody(path, email, type, "").Select(p => p.Item1 == "FullName" ? (p.Item1, name) : p).Append(("Phone", phone)).ToArray();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(path, Form(token, body))).StatusCode);
        }
        Assert.NotEmpty(sink.Lines);   // the sink saw the requests (an empty capture would prove nothing)
        Assert.DoesNotContain(sink.Lines, l => l.Contains(email) || l.Contains(name) || l.Contains(phone));
    }

    // Every option field, forged with an id of ANOTHER list (or an unknown id): refused, nothing written.
    [Theory]
    [InlineData("/forms/join", "investor", "InvestorTypeId", ContentItemGroups.Sectors)]
    [InlineData("/forms/join", "investor", "TicketSizeId", ContentItemGroups.InvestorTypes)]
    [InlineData("/forms/join", "investor", "SectorIds", ContentItemGroups.TicketSizes)]
    [InlineData("/forms/join", "startup", "SectorId", ContentItemGroups.StartupStages)]
    [InlineData("/forms/join", "startup", "StageId", ContentItemGroups.FundingRanges)]
    [InlineData("/forms/join", "startup", "FundingId", ContentItemGroups.TicketSizes)]
    [InlineData("/forms/forum-registration", null, "InterestOptionId", null)]
    [InlineData("/forms/forum-registration", null, "EventId", null)]
    public async Task Every_option_field_refuses_an_id_of_another_list(string path, string? kind, string field, string? otherGroup)
    {
        var (c, t) = await ClientAsync();
        var email = Unique("forge") + "@example.com";
        var forged = otherGroup is null ? "999999" : (await Group(otherGroup, 0)).ToString();
        var body = new List<(string, string)>(Base(email));
        if (kind == "investor")
            body.AddRange([("Kind", "investor"), ("Country", "Egypt"), ("InvestorTypeId", (await Group(ContentItemGroups.InvestorTypes, 0)).ToString())]);
        if (kind == "startup")
            body.AddRange([("Kind", "startup"), ("Country", "Egypt"), ("StartupName", "QA"), ("SectorId", (await Group(ContentItemGroups.Sectors, 0)).ToString()),
                ("StageId", (await Group(ContentItemGroups.StartupStages, 0)).ToString())]);
        body.RemoveAll(p => p.Item1 == field);
        body.Add((field, forged));
        await AssertRefused(await c.PostAsync(path, Form(t, body.ToArray())), field);
        Assert.Equal(0, await LeadCount(email));
    }

    [Fact] // a link may not carry a quote, an angle bracket or whitespace (stored text shown to the admin)
    public async Task Links_with_quotes_or_angle_brackets_are_refused()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("url") + "@example.com";
        var type = (await Group(ContentItemGroups.InvestorTypes, 0)).ToString();
        await AssertRefused(await c.PostAsync("/forms/join", Form(t, [.. Base(email), ("Kind", "investor"), ("Country", "Egypt"), ("InvestorTypeId", type),
            ("LinkedInUrl", "https://example.invalid/q\"><script>x</script>")])), "LinkedInUrl");
        await AssertRefused(await c.PostAsync("/forms/join", Form(t, [.. Base(email), ("Kind", "startup"), ("Country", "Egypt"), ("StartupName", "QA"),
            ("SectorId", (await Group(ContentItemGroups.Sectors, 0)).ToString()), ("StageId", (await Group(ContentItemGroups.StartupStages, 0)).ToString()),
            ("WebsiteUrl", "https://example.invalid/a<b")])), "WebsiteUrl");
        Assert.Equal(0, await LeadCount(email));
    }

    [Fact] // the admin's workflow fields cannot be set from a public form
    public async Task Status_and_note_cannot_be_over_posted()
    {
        var (c, t) = await ClientAsync();
        var email = Unique("st") + "@example.com";
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/forms/opportunity-interest", Form(t, [.. Base(email), ("Status", "2"), ("AdminNote", "forged")]))).StatusCode);
        var row = await f.DbAsync(db => db.OpportunityInterests.AsNoTracking().SingleAsync(x => x.Email == email));
        Assert.Equal((LeadStatus.New, (string?)null), (row.Status, row.AdminNote));
    }

    [Fact] // the global cap per form (all addresses together); honeypot and refused posts do not use the budget
    public async Task The_global_cap_refuses_a_form_once_its_window_is_full()
    {
        await using var app = f.WithWebHostBuilder(b => b.UseSetting("Security:RateLimit:LeadFormGlobalPermitLimit", "2"));
        async Task<HttpResponseMessage> Post(string path, (string, string)[] body)
        {
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());   // a new address every time
            var token = await client.GetTokenAsync("/en/tv/sponsorship");
            return await client.PostAsync(path, Form(token, body));
        }
        var email = Unique("cap") + "@example.com";
        (string, string)[] valid = [.. Base(email), ("CompanyName", "Co"), ("Unit", "tv")];
        Assert.Equal(HttpStatusCode.OK, (await Post("/en/forms/sponsorship", [.. valid, ("Website", "bot")])).StatusCode);   // honeypot: not counted
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/en/forms/sponsorship", [.. Base(email), ("Unit", "tv")])).StatusCode);   // refused: not counted
        Assert.Equal(HttpStatusCode.OK, (await Post("/en/forms/sponsorship", valid)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post("/en/forms/sponsorship", valid)).StatusCode);
        var full = await Post("/en/forms/sponsorship", valid);
        Assert.Equal(HttpStatusCode.TooManyRequests, full.StatusCode);
        Assert.Equal(Msg.En(MessageKeys.TooManyRequests), await Message(full));
        Assert.Equal(2, await f.DbAsync(db => db.SponsorshipEnquiries.CountAsync(s => s.Email == email)));
        // another form keeps its own window
        Assert.Equal(HttpStatusCode.OK, (await Post("/forms/opportunity-interest", Base(email))).StatusCode);
    }

    private Task<int> LeadCount(string email) => f.DbAsync(async db =>
        await db.Registrations.CountAsync(r => r.Email == email) + await db.NetworkApplications.CountAsync(r => r.Email == email)
        + await db.SponsorshipEnquiries.CountAsync(r => r.Email == email) + await db.OpportunityInterests.CountAsync(r => r.Email == email));
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Lines { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose() { }

    private sealed class Logger(CapturingLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Lines.Enqueue($"{category}|{formatter(state, exception)}|{exception}");
    }
}
