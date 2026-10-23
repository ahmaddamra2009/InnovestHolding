using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Site;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// C5 Dev 4: the footer (T5.7, T5.11 footer link), the list screens of services, figures, partners, social links and
/// extra links (T5.9), and authorization/antiforgery of every new admin POST (T5.10).
/// Tests marked "depends on Dev 3" read the PUBLIC footer HTML: they need SiteViews.Shell to fill ShellVm.Footer and
/// SocialVm.Label/IconKey (Dev 3's files); they pass once the branches are merged.
/// </summary>
[Collection("App")]
public class FooterTests(TestAppFactory f)
{
    private async Task<SiteViews> ViewsAsync(bool english = false) =>
        new(await f.Services.GetRequiredService<ISiteContentCache>().GetAsync(), english);

    /// <summary>One line per column: title | href=title | … (comparable by value).</summary>
    private static List<string> Flat(IEnumerable<FooterColumnVm> columns) =>
        columns.Select(c => c.Title.Value + " | " + string.Join(" | ", c.Links.Select(l => l.Href + "=" + l.Title.Value))).ToList();

    /// <summary>The C1–C4 footer, derived as Views/Shared/_Footer did from the header menu (ShowInMenu).</summary>
    private static List<string> C4Footer(SiteViews v)
    {
        var shell = v.Shell(PageKeys.Home, null);
        var company = shell.Menu.Where(m => m.Children.Count == 0).ToList();
        var eco = shell.Menu.FirstOrDefault(m => m.Children.Count > 0);
        if (eco is not null) company.Insert(Math.Min(2, company.Count), eco);
        var quick = company.Select(m => new LinkVm(m.Href, m.Title))
            .Concat(v.Snapshot.Nav.Select(n => new LinkVm(n.Href, v.P(n.TitleAr, n.TitleEn)))).ToList();
        var columns = new List<FooterColumnVm> { new(v.Content.Text("footer_quicklinks_title"), quick) };
        if (eco is not null)
            columns.AddRange(eco.Children.Select(u => new FooterColumnVm(u.Title,
                new[] { new LinkVm(u.Href, u.Title) }.Concat(u.Children.Select(c => new LinkVm(c.Href, c.Title))).ToList())));
        return Flat(columns);
    }

    [Theory] // T5.7: on default data (ShowInFooter = ShowInMenu, all published) the automatic footer IS the C4 footer
    [InlineData(false)]
    [InlineData(true)]
    public async Task Automatic_footer_equals_the_C4_footer(bool english)
    {
        var v = await ViewsAsync(english);
        Assert.Empty(v.Snapshot.Footer);
        var expected = C4Footer(v);
        var actual = Flat(FooterBuilder.Build(v).Columns);
        Assert.True(expected.Count >= 4, "an empty derivation cannot pass"); // quick links + forum, network, tv
        Assert.Equal(expected, actual);
        // The unit columns are headed by their own page (rendered as the summary link, as before).
        var columns = FooterBuilder.Build(v).Columns;
        Assert.Null(FooterBuilder.HeadingLink(columns[0]));
        Assert.All(columns.Skip(1), c => Assert.NotNull(FooterBuilder.HeadingLink(c)));
    }

    [Fact] // the footer has its own switch: hiding a page from the footer keeps it in the header, and the reverse
    public async Task ShowInFooter_and_IsPublished_filter_the_automatic_footer()
    {
        try
        {
            await SetPageAsync(PageKeys.News, p => p.ShowInFooter = false);
            var v = await ViewsAsync();
            var hrefs = FooterBuilder.Build(v).Columns.SelectMany(c => c.Links).Select(l => l.Href).ToList();
            Assert.DoesNotContain("/news", hrefs);
            Assert.Contains(v.Shell(PageKeys.Home, null).Menu, m => m.Href == "/news");
            Assert.Contains("/contact", hrefs);

            await SetPageAsync(PageKeys.News, p => p.ShowInFooter = true);
            await SetPageAsync(PageKeys.Network, p => p.IsPublished = false);
            v = await ViewsAsync();
            var columns = FooterBuilder.Build(v).Columns;
            Assert.DoesNotContain(columns.SelectMany(c => c.Links), l => l.Href.StartsWith("/angel-network", StringComparison.Ordinal));
            Assert.Equal(3, columns.Count); // quick links + forum + tv: the hidden unit's column is gone with it
        }
        finally
        {
            await SetPageAsync(PageKeys.News, p => p.ShowInFooter = true);
            await SetPageAsync(PageKeys.Network, p => p.IsPublished = true);
        }
    }

    [Fact] // T5.7: «تخصيص» copies the current footer (identical in both languages); «إعادة» returns to automatic
    public async Task Customise_copies_the_footer_and_reset_returns_to_automatic()
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Nav?tab=footer");
        var beforeAr = Flat(FooterBuilder.Build(await ViewsAsync()).Columns);
        var beforeEn = Flat(FooterBuilder.Build(await ViewsAsync(true)).Columns);
        try
        {
            var r = await client.PostAsync("/Admin/Nav/CustomiseFooter", Form(token));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.True(await f.DbAsync(db => db.FooterColumns.CountAsync()) > 0);
            Assert.Equal(beforeAr, Flat(FooterBuilder.Build(await ViewsAsync()).Columns));
            Assert.Equal(beforeEn, Flat(FooterBuilder.Build(await ViewsAsync(true)).Columns));

            // Customised: an unpublished page in a column is skipped; a column without a shown link is skipped.
            var first = await f.DbAsync(db => db.FooterColumns.OrderBy(c => c.SortOrder).FirstAsync());
            var page = Assert.Single(await f.DbAsync(db => db.FooterColumns.Where(c => c.SortOrder == 2).ToListAsync()));
            await client.PostAsync("/Admin/Nav/UpdateColumn", Form(token, ("Id", first.Id.ToString()), ("TitleAr", "QA عمود"), ("TitleEn", "QA column")));
            var v = await ViewsAsync(true);
            Assert.Equal("QA column", FooterBuilder.Build(v).Columns[0].Title.Value);

            // Move a column: the order follows.
            await client.PostAsync("/Admin/Nav/MoveColumn", Form(token, ("id", page.Id.ToString()), ("direction", "-1")));
            Assert.Equal(page.TitleAr, FooterBuilder.Build(await ViewsAsync()).Columns[0].Title.Value);

            // Add an extra page link, refuse an unknown target.
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/Nav/AddLink", Form(token, ("ColumnId", first.Id.ToString()), ("Target", "page:privacy")))).StatusCode);
            var links = await f.DbAsync(db => db.FooterLinks.CountAsync(l => l.FooterColumnId == first.Id && l.PageKey == "privacy"));
            Assert.Equal(1, links);
            await client.PostAsync("/Admin/Nav/AddLink", Form(token, ("ColumnId", first.Id.ToString()), ("Target", "page:nope")));
            Assert.Equal(0, await f.DbAsync(db => db.FooterLinks.CountAsync(l => l.PageKey == "nope")));
        }
        finally
        {
            var r = await client.PostAsync("/Admin/Nav/ResetFooter", Form(token));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        }
        Assert.Equal(0, await f.DbAsync(db => db.FooterColumns.CountAsync()));
        Assert.Equal(0, await f.DbAsync(db => db.FooterLinks.CountAsync()));
        Assert.Equal(beforeAr, Flat(FooterBuilder.Build(await ViewsAsync()).Columns));
    }

    [Fact] // T5.11 (footer part): the link exists while the policy has a body; emptied → gone
    public async Task Privacy_link_follows_the_policy_body()
    {
        var original = await f.DbAsync(db => db.Pages.Where(p => p.Key == PageKeys.Privacy).Select(p => p.BodyAr).SingleAsync());
        Assert.False(string.IsNullOrWhiteSpace(original)); // the migration ships the CONTENT-C5 text
        Assert.Equal("/privacy", FooterBuilder.Build(await ViewsAsync()).Privacy?.Href);
        Assert.Equal("/en/privacy", FooterBuilder.Build(await ViewsAsync(true)).Privacy?.Href);
        try
        {
            await SetPageAsync(PageKeys.Privacy, p => p.BodyAr = "");
            Assert.Null(FooterBuilder.Build(await ViewsAsync()).Privacy);
            await SetPageAsync(PageKeys.Privacy, p => { p.BodyAr = original; p.IsPublished = false; });
            Assert.Null(FooterBuilder.Build(await ViewsAsync()).Privacy);
        }
        finally
        {
            await SetPageAsync(PageKeys.Privacy, p => { p.BodyAr = original; p.IsPublished = true; });
        }
    }

    [Theory] // depends on Dev 3 (ShellVm.Footer filled by SiteViews.Shell): the rendered footer
    [InlineData("/", "/privacy", "/about")]
    [InlineData("/en", "/en/privacy", "/en/about")]
    public async Task Rendered_footer_has_its_columns_and_a_bare_privacy_link(string url, string privacy, string about)
    {
        var html = await (await f.NewClient().GetAsync(url)).Content.ReadAsStringAsync();
        var footer = html[html.IndexOf("<footer class=\"site-footer\">", StringComparison.Ordinal)..];
        Assert.Contains($"<a href=\"{about}\" class=\"footer-link\">", footer);
        Assert.Equal(3, Regex.Matches(footer, "<details class=\"footer-col footer-unit\" open>").Count);
        Assert.Matches($"<div class=\"footer-bottom\">\\s*<span[^>]*>[^<]*</span>\\s*<a href=\"{Regex.Escape(privacy)}\" class=\"footer-legal-link\"", footer);
    }

    [Fact] // depends on Dev 3 (SocialVm.Label/IconKey + custom rows in SiteViews.Socials): a custom network renders
    public async Task Custom_social_link_renders_with_its_label_and_whitelisted_icon()
    {
        var label = "QA" + Guid.NewGuid().ToString("N")[..6];
        var id = await f.DbAsync(async db =>
        {
            var row = new SocialLink { Network = SocialLink.Custom, Url = "https://example.com/qa-social", Label = label, IconKey = "globe", SortOrder = 999 };
            db.SocialLinks.Add(row);
            await db.SaveChangesAsync();
            return row.Id;
        });
        try
        {
            var html = await (await f.NewClient().GetAsync("/en")).Content.ReadAsStringAsync();
            Assert.Contains($"href=\"https://example.com/qa-social\" class=\"social-link\" aria-label=\"{label}\"", html);
        }
        finally
        {
            await f.DbAsync(async db => { db.SocialLinks.Remove(await db.SocialLinks.FindAsync(id) ?? throw new InvalidOperationException()); return await db.SaveChangesAsync(); });
        }
    }

    private Task<int> SetPageAsync(string key, Action<Page> change) => f.DbAsync(async db =>
    {
        var page = await db.Pages.SingleAsync(p => p.Key == key);
        change(page);
        return await db.SaveChangesAsync();
    });
}

[Collection("App")]
public class AdminListTests(TestAppFactory f)
{
    private async Task<(HttpClient Client, string Token)> AdminAsync(string page)
    {
        var client = await f.SignedInClientAsync();
        return (client, await client.GetTokenAsync(page));
    }

    [Fact] // T5.9 services: create with an icon key, IconSvg never bound, unknown icon refused, move, toggle, delete
    public async Task Services_crud_icon_and_order()
    {
        var (c, t) = await AdminAsync("/Admin/Platforms");
        var title = Unique("svc");
        var r = await c.PostAsync("/Admin/Platforms/Create", Form(t, ("TitleAr", title), ("DescriptionAr", "a\nb"), ("IconKey", "rocket"),
            ("IconSvg", "<script>x</script>"), ("SortOrder", "-5"), ("IsActive", "false")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var row = await f.DbAsync(db => db.Platforms.AsNoTracking().SingleAsync(p => p.TitleAr == title));
        Assert.Equal("rocket", row.IconKey);
        Assert.Equal("", row.IconSvg);
        Assert.True(row.IsActive);
        Assert.Equal(await f.DbAsync(db => db.Platforms.MaxAsync(p => p.SortOrder)), row.SortOrder); // appended last

        var bad = Unique("svc-bad");
        await c.PostAsync("/Admin/Platforms/Create", Form(t, ("TitleAr", bad), ("IconKey", "not-an-icon")));
        Assert.Equal(0, await f.DbAsync(db => db.Platforms.CountAsync(p => p.TitleAr == bad)));

        var count = await f.DbAsync(db => db.Platforms.CountAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/Admin/Platforms/Move", Form(t, ("id", row.Id.ToString()), ("direction", "-1")))).StatusCode);
        var ordered = await f.DbAsync(db => db.Platforms.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Select(p => p.Id).ToListAsync());
        Assert.Equal(count - 2, ordered.IndexOf(row.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/Admin/Platforms/Move", Form(t, ("id", row.Id.ToString()), ("direction", "5")))).StatusCode);

        await c.PostAsync("/Admin/Platforms/Toggle", Form(t, ("id", row.Id.ToString())));
        var snapshot = await f.Services.GetRequiredService<ISiteContentCache>().GetAsync();
        Assert.DoesNotContain(snapshot.Platforms, p => p.TitleAr == title);

        await c.PostAsync("/Admin/Platforms/Delete", Form(t, ("id", row.Id.ToString())));
        Assert.Equal(0, await f.DbAsync(db => db.Platforms.CountAsync(p => p.Id == row.Id)));
    }

    [Fact] // T5.9 figures: create, hide (out of the snapshot), move, delete; IconSvg never bound
    public async Task Stats_crud_hide_and_order()
    {
        var (c, t) = await AdminAsync("/Admin/Stats");
        var label = Unique("stat");
        await c.PostAsync("/Admin/Stats/Create", Form(t, ("LabelAr", label), ("Value", "42"), ("Suffix", "%"), ("IconSvg", "<x/>")));
        var row = await f.DbAsync(db => db.Stats.AsNoTracking().SingleAsync(s => s.LabelAr == label));
        Assert.Equal((42, "%", "", true), (row.Value, row.Suffix, row.IconSvg, row.IsActive));
        var cache = f.Services.GetRequiredService<ISiteContentCache>();
        Assert.Contains((await cache.GetAsync()).Stats, s => s.LabelAr == label);

        await c.PostAsync("/Admin/Stats/Toggle", Form(t, ("id", row.Id.ToString())));
        Assert.DoesNotContain((await cache.GetAsync()).Stats, s => s.LabelAr == label);

        var before = await f.DbAsync(db => db.Stats.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(s => s.Id).ToListAsync());
        await c.PostAsync("/Admin/Stats/Move", Form(t, ("id", row.Id.ToString()), ("direction", "-1")));
        var after = await f.DbAsync(db => db.Stats.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(s => s.Id).ToListAsync());
        Assert.Equal(before.IndexOf(row.Id) - 1, after.IndexOf(row.Id));
        // Back down: the seeded figures keep their order numbers (MigrationTests compares them with a fresh database).
        await c.PostAsync("/Admin/Stats/Move", Form(t, ("id", row.Id.ToString()), ("direction", "1")));
        Assert.Equal(before, await f.DbAsync(db => db.Stats.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(s => s.Id).ToListAsync()));

        await c.PostAsync("/Admin/Stats/Create", Form(t, ("LabelAr", Unique("stat-bad")), ("Value", "abc")));
        await c.PostAsync("/Admin/Stats/Delete", Form(t, ("id", row.Id.ToString())));
        Assert.Equal(0, await f.DbAsync(db => db.Stats.CountAsync(s => s.Id == row.Id)));
    }

    [Fact] // T5.9 partners: ▲▼ within the group; every other partner keeps its place in the one global order
    public async Task Partner_move_is_scoped_to_its_group()
    {
        var (c, t) = await AdminAsync("/Admin/Partners");
        var groupId = await f.DbAsync(async db =>
        {
            var g = new PartnerGroup { NameAr = Unique("grp"), SortOrder = 99, IsActive = true };
            db.PartnerGroups.Add(g);
            await db.SaveChangesAsync();
            return g.Id;
        });
        var names = new[] { Unique("pa"), Unique("pb"), Unique("pc") };
        await c.PostAsync("/Admin/Partners/Create", Form(t, ("NameAr", names[0]), ("PartnerGroupId", groupId.ToString())));
        await c.PostAsync("/Admin/Partners/Create", Form(t, ("NameAr", names[1])));
        await c.PostAsync("/Admin/Partners/Create", Form(t, ("NameAr", names[2]), ("PartnerGroupId", groupId.ToString())));
        var ids = await f.DbAsync(db => db.Partners.Where(p => names.Contains(p.NameAr)).OrderBy(p => p.SortOrder).Select(p => p.Id).ToListAsync());
        try
        {
            async Task<List<int>> Order() => await f.DbAsync(db => db.Partners.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Select(p => p.Id).ToListAsync());
            var before = await Order();
            // pc moves up within its group: it swaps with pa (not with the ungrouped pb in between).
            await c.PostAsync("/Admin/Partners/Move", Form(t, ("id", ids[2].ToString()), ("direction", "-1")));
            var after = await Order();
            var expected = before.ToList();
            (expected[expected.IndexOf(ids[0])], expected[expected.IndexOf(ids[2])]) = (ids[2], ids[0]);
            Assert.Equal(expected, after);
            // At the group's edge nothing moves.
            await c.PostAsync("/Admin/Partners/Move", Form(t, ("id", ids[2].ToString()), ("direction", "-1")));
            Assert.Equal(expected, await Order());
        }
        finally
        {
            foreach (var id in ids) await c.PostAsync("/Admin/Partners/Delete", Form(t, ("id", id.ToString())));
            await f.DbAsync(async db => { db.PartnerGroups.Remove(await db.PartnerGroups.FindAsync(groupId) ?? throw new InvalidOperationException()); return await db.SaveChangesAsync(); });
        }
    }

    [Fact] // T5.9 social: a known network once; a custom one needs a label and a whitelisted icon; known rows are not deleted
    public async Task Social_links_add_known_once_and_custom_with_label_and_icon()
    {
        var (c, t) = await AdminAsync("/Admin/SocialLinks");
        var known = await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == "linkedin"));
        Assert.Equal(1, known);
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "linkedin"), ("Url", "https://example.com/dup")));
        Assert.Equal(1, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == "linkedin")));
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "myspace"), ("Url", "https://example.com/x")));
        Assert.Equal(0, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Network == "myspace")));

        var label = Unique("net")[..20];
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "custom"), ("Url", "https://example.com/c"), ("Label", label)));
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "custom"), ("Url", "https://example.com/c"), ("Label", label), ("IconKey", "<svg>")));
        Assert.Equal(0, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Label == label)));
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "custom"), ("Url", "javascript:alert(1)"), ("Label", label), ("IconKey", "globe")));
        Assert.Equal(0, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Label == label)));
        await c.PostAsync("/Admin/SocialLinks/Create", Form(t, ("Network", "custom"), ("Url", "https://example.com/c"), ("Label", label), ("IconKey", "globe")));
        var row = await f.DbAsync(db => db.SocialLinks.AsNoTracking().SingleAsync(s => s.Label == label));
        Assert.Equal(("custom", "globe"), (row.Network, row.IconKey));
        Assert.Contains((await f.Services.GetRequiredService<ISiteContentCache>().GetAsync()).Socials, s => s.Label == label);

        var linkedin = await f.DbAsync(db => db.SocialLinks.SingleAsync(s => s.Network == "linkedin"));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/Admin/SocialLinks/Delete", Form(t, ("id", linkedin.Id.ToString())))).StatusCode);
        await c.PostAsync("/Admin/SocialLinks/Delete", Form(t, ("id", row.Id.ToString())));
        Assert.Equal(0, await f.DbAsync(db => db.SocialLinks.CountAsync(s => s.Id == row.Id)));
    }

    [Fact] // extra links: header switch, order; the dead sub-item actions are gone
    public async Task Nav_header_switch_order_and_no_sub_items()
    {
        var (c, t) = await AdminAsync("/Admin/Nav");
        var title = Unique("nav");
        await c.PostAsync("/Admin/Nav/Create", Form(t, ("TitleAr", title), ("Href", "https://example.com/n")));
        var row = await f.DbAsync(db => db.NavItems.AsNoTracking().SingleAsync(n => n.TitleAr == title));
        Assert.True(row.ShowInHeader);
        try
        {
            await c.PostAsync("/Admin/Nav/ToggleHeader", Form(t, ("id", row.Id.ToString())));
            var snap = await f.Services.GetRequiredService<ISiteContentCache>().GetAsync();
            var nav = Assert.Single(snap.Nav, n => n.Id == row.Id);
            Assert.False(nav.ShowInHeader);
            // Out of the header, still in the automatic footer.
            Assert.Contains(FooterBuilder.Plan(snap)[0].Links, l => l.NavItemId == row.Id);

            Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/Admin/Nav/AddSubItem", Form(t, ("NavItemId", row.Id.ToString()), ("TitleAr", "x"), ("Href", "/x")))).StatusCode);
            var html = await (await c.GetAsync("/Admin/Nav")).Content.ReadAsStringAsync();
            Assert.DoesNotContain("AddSubItem", html);
            Assert.DoesNotContain("hasDropdown", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await c.PostAsync("/Admin/Nav/Delete", Form(t, ("id", row.Id.ToString())));
        }
    }

    public static readonly TheoryData<string> NewPosts =
    [
        "/Admin/Nav/Toggle", "/Admin/Nav/ToggleHeader", "/Admin/Nav/Move", "/Admin/Nav/CustomiseFooter", "/Admin/Nav/ResetFooter",
        "/Admin/Nav/AddColumn", "/Admin/Nav/UpdateColumn", "/Admin/Nav/DeleteColumn", "/Admin/Nav/MoveColumn",
        "/Admin/Nav/AddLink", "/Admin/Nav/DeleteLink", "/Admin/Nav/MoveLink",
        "/Admin/Platforms/Create", "/Admin/Platforms/Move", "/Admin/Platforms/Delete",
        "/Admin/Stats/Create", "/Admin/Stats/Toggle", "/Admin/Stats/Move", "/Admin/Stats/Delete",
        "/Admin/Partners/Move", "/Admin/SocialLinks/Create", "/Admin/SocialLinks/Toggle", "/Admin/SocialLinks/Move",
        "/Admin/SocialLinks/Delete",
    ];

    [Theory] // T5.10 (Standing Check #5): anonymous → login; signed in without a token → 400
    [MemberData(nameof(NewPosts))]
    public async Task New_admin_posts_need_a_session_and_a_token(string url)
    {
        var anon = f.NewClient();
        var token = await anon.GetTokenAsync("/Account/Login");
        var r = await anon.PostAsync(url, Form(token, ("id", "1"), ("direction", "1")));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Account/Login", r.Headers.Location!.OriginalString);

        var admin = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(url, Form(("id", "1"), ("direction", "1")))).StatusCode);
    }
}
