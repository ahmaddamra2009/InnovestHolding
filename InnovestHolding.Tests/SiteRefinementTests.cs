using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using InnovestHolding.Controllers.Admin;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Site;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>Search titles, JSON-LD references, figure labels, admin choices, asset caching and unused keys.</summary>
[Collection("App")]
public class SiteRefinementTests(TestAppFactory f)
{
    private Task<string> PageAsync(string path) => f.NewClient().GetStringAsync(path);

    private static JsonElement Graph(string html)
    {
        var m = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(m.Success, "no JSON-LD");
        return JsonDocument.Parse(m.Groups[1].Value).RootElement.Clone().GetProperty("@graph");
    }

    private static string? Meta(string html, string pattern) =>
        Regex.Match(html, pattern, RegexOptions.Singleline) is { Success: true } m ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;

    [Fact] // search results cut longer titles and descriptions; every English page fits
    public async Task English_titles_fit_60_characters_and_descriptions_160()
    {
        var checkedPages = 0;
        foreach (var page in SiteRoutes.All)
        {
            var html = await PageAsync(CultureUrls.Page(page.Key, true));
            var title = Meta(html, "<title>(.*?)</title>")!;
            var description = Meta(html, "<meta name=\"description\" content=\"(.*?)\"");
            Assert.True(title.Length <= 60, $"{page.Key}: title {title.Length} «{title}»");
            Assert.True((description?.Length ?? 0) <= 160, $"{page.Key}: description {description?.Length} «{description}»");
            checkedPages++;
        }
        Assert.Equal(21, checkedPages); // + privacy (C5), + forum.editions (F10)
    }

    [Fact] // a node referenced by @id (isPartOf, publisher, about, parentOrganization) is in the same page's graph
    public async Task Every_json_ld_reference_resolves_inside_the_same_graph()
    {
        var references = 0;
        foreach (var page in SiteRoutes.All)
            foreach (var english in new[] { false, true })
            {
                var graph = Graph(await PageAsync(CultureUrls.Page(page.Key, english)));
                var ids = graph.EnumerateArray().Where(n => n.TryGetProperty("@id", out _)).Select(n => n.GetProperty("@id").GetString()).ToHashSet();
                foreach (var node in graph.EnumerateArray())
                    foreach (var property in node.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Object))
                        if (property.Value.EnumerateObject().Count() == 1 && property.Value.TryGetProperty("@id", out var reference))
                        {
                            references++;
                            Assert.True(ids.Contains(reference.GetString()), $"{page.Key} ({(english ? "en" : "ar")}): {property.Name} → {reference} not in the graph");
                        }
            }
        Assert.True(references >= 38, $"{references} references checked");
    }

    [Fact] // a paged list's ItemList names exactly the rows that page renders
    public async Task Forum_events_item_list_names_only_the_rendered_page()
    {
        var events = await f.DbAsync(db => db.Events.CountAsync(e => e.IsActive));
        Assert.True(events >= 2, $"{events} active events");
        var original = await f.DbAsync(db => db.SiteContents.Where(s => s.SectionKey == "list_page_size").Select(s => s.ContentAr).SingleAsync());
        try
        {
            await SetContentAsync("list_page_size", "1");
            var names = new List<string>();
            foreach (var path in new[] { "/forum/events", "/forum/events?page=2" })
            {
                var html = await PageAsync(path);
                var list = Graph(html).EnumerateArray().Single(n => n.TryGetProperty("mainEntity", out _)).GetProperty("mainEntity");
                var items = list.GetProperty("itemListElement").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();
                var rendered = Regex.Matches(html, "<div class=\"event-info\">\\s*<h3[^>]*><a [^>]*>(.*?)</a>").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
                Assert.Single(items);
                Assert.Equal(rendered, items);
                names.Add(items[0]);
            }
            Assert.NotEqual(names[0], names[1]);
        }
        finally
        {
            await SetContentAsync("list_page_size", original);
        }
    }

    [Fact] // the English page shows the English label before it falls back to the Arabic short label
    public async Task English_figure_uses_its_english_label_before_the_arabic_short_label()
    {
        var id = await f.DbAsync(db => db.Stats.OrderBy(s => s.SortOrder).Select(s => s.Id).FirstAsync());
        var before = await f.DbAsync(db => db.Stats.AsNoTracking().SingleAsync(s => s.Id == id));
        try
        {
            await f.DbAsync(async db =>
            {
                var s = await db.Stats.SingleAsync(x => x.Id == id);
                s.ShortLabelAr = "مختصر"; s.ShortLabelEn = null; s.LabelEn = "Figure label EN";
                return await db.SaveChangesAsync();
            });
            const string label = "<div class=\"hero-stat-label\">{0}</div>"; // in the page's language: no lang/dir
            var en = await PageAsync("/en");
            Assert.True(en.Contains(string.Format(label, "Figure label EN")), string.Join("\n", Regex.Matches(en, "<div class=\"hero-stat-label\".*?</div>").Select(m => m.Value)));
            Assert.DoesNotContain("مختصر", en);
            Assert.Contains(string.Format(label, "مختصر"), await PageAsync("/")); // positive control
        }
        finally
        {
            await f.DbAsync(async db =>
            {
                var s = await db.Stats.SingleAsync(x => x.Id == id);
                s.ShortLabelAr = before.ShortLabelAr; s.ShortLabelEn = before.ShortLabelEn; s.LabelEn = before.LabelEn;
                return await db.SaveChangesAsync();
            });
        }
    }

    [Fact] // the header button's target is chosen from the pages of the site, not typed
    public async Task Header_cta_page_is_a_choice_of_the_route_table()
    {
        var client = await f.SignedInClientAsync();
        var html = await client.GetStringAsync("/Admin/Content/Edit");
        var select = Regex.Match(html, "<select id=\"content-header-cta-page\" name=\"content\\[header_cta_page\\].Ar\" class=\"form-control\">(.*?)</select>", RegexOptions.Singleline);
        Assert.True(select.Success, "no page select");
        var options = Regex.Matches(select.Groups[1].Value, "<option value=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(SiteRoutes.All.Select(p => p.Key).Append(ContentKeys.ExternalLinkChoice), options); // C5: + «رابط خارجي…»
        Assert.Contains("<option value=\"network.join\" selected=\"selected\">", select.Groups[1].Value);
    }

    [Fact] // brand images carry ?v= so they get the long cache; the header logo is fetched first
    public async Task Brand_images_are_versioned_and_the_header_logo_has_high_priority()
    {
        var html = await PageAsync("/");
        var header = Regex.Match(html, "<header id=\"main-header\".*?</header>", RegexOptions.Singleline).Value;
        var logo = Regex.Match(header, "<img src=\"(/assets/brand/logo-on-dark\\.png\\?v=[^\"]+)\"[^>]*fetchpriority=\"high\"");
        Assert.True(logo.Success, "header logo without ?v= or fetchpriority");
        Assert.Matches("srcset=\"/assets/brand/logo-on-dark\\.webp\\?v=[^\" ]+ 1x, /assets/brand/logo-on-dark@2x\\.webp\\?v=[^\" ]+ 2x\"", header);
        // No unversioned brand image in the markup (the JSON-LD logo is a stable identifier and stays unversioned).
        Assert.DoesNotMatch("(src|srcset|href)=\"[^\"]*/assets/brand/[^\"?]+\\.(png|webp|svg)[\" ,]", html);

        var r = await f.NewClient().GetAsync(WebUtility.HtmlDecode(logo.Groups[1].Value));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("immutable", r.Headers.CacheControl!.ToString());
    }

    [Fact] // show/hide of a list item is the checkbox of its edit form; no second, unreachable endpoint
    public async Task Content_items_have_no_separate_toggle_endpoint()
    {
        Assert.Null(typeof(ContentItemsController).GetMethod("Toggle", BindingFlags.Public | BindingFlags.Instance));
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/ContentItems?group=sectors");
        var r = await client.PostAsync("/Admin/ContentItems/Toggle", Form(token, ("id", "1")));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact] // every message key constant is referenced by code or a view (an unused key is dead translation work)
    public void Every_message_key_is_used()
    {
        var root = BilingualUnitTests.RepoRoot();
        var source = string.Join("\n", Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs") || p.EndsWith(".cshtml"))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}InnovestHolding.Tests{Path.DirectorySeparatorChar}")
                        && !p.EndsWith("MessageKeys.cs"))
            .Select(File.ReadAllText));

        var unused = new List<string>();
        foreach (var type in new[] { typeof(MessageKeys) }.Concat(typeof(MessageKeys).GetNestedTypes()))
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => x.IsLiteral))
            {
                var nested = type == typeof(MessageKeys) ? "" : type.Name + "\\.";
                var alias = type == typeof(MessageKeys.Fields) ? "|F\\." : "";
                var pattern = $"\\b(?:(?:MessageKeys|K)\\.{nested}{alias}){field.Name}\\b";
                if (!Regex.IsMatch(source, pattern)) unused.Add($"{type.Name}.{field.Name}");
            }
        Assert.Empty(unused);
    }

    [Theory] // the phone hint follows the company's country (Egypt), in both languages, on every form that shows it
    [InlineData("/")]
    [InlineData("/en")]
    [InlineData("/forum/registration")]
    [InlineData("/en/forum/registration")]
    public async Task Phone_placeholder_is_the_egyptian_format_in_both_languages(string path)
    {
        var html = await PageAsync(path);
        var hints = Regex.Matches(html, "<input type=\"tel\"[^>]*placeholder=\"([^\"]*)\"").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        Assert.NotEmpty(hints);
        Assert.All(hints, h => Assert.Equal("+20 1XX XXX XXXX", h));
        Assert.DoesNotContain("+962", WebUtility.HtmlDecode(html));
    }

    [Fact] // the drawer opens with focus on its first control: that control must not transition the inherited visibility
    public async Task Drawer_controls_do_not_transition_visibility_so_focus_can_enter_on_open()
    {
        var html = await PageAsync("/");
        // The drawer's first control is the language link (what main.js focuses on open).
        Assert.Matches("<div class=\"mobile-nav\" id=\"mobile-nav-drawer\"[^>]*>\\s*<div class=\"mobile-nav-head\">\\s*<a class=\"mobile-lang-link\"", html);

        var css = await f.NewClient().GetStringAsync("/css/pages.css");
        var rule = Regex.Match(css, "\\.mobile-nav a, \\.mobile-nav button \\{ transition-property: ([^;]+);");
        Assert.True(rule.Success, "no transition-property rule for the drawer controls");
        Assert.DoesNotContain("visibility", rule.Groups[1].Value);
        Assert.DoesNotContain("all", rule.Groups[1].Value);
        var js = await f.NewClient().GetStringAsync("/js/main.js");
        // The toggle itself: the open branch moves focus to the first control, the close branch returns it to the button.
        var toggle = Regex.Match(js, "function toggleMobileMenu\\(\\) \\{(.*?)\\n  \\}", RegexOptions.Singleline);
        Assert.True(toggle.Success, "toggleMobileMenu not found");
        var branches = Regex.Match(toggle.Groups[1].Value, "if \\(open\\) \\{(?<open>.*?)\\} else \\{(?<close>.*?)\\}", RegexOptions.Singleline);
        Assert.True(branches.Success, "open/close branches not found");
        Assert.Matches("const first = mobileNav\\.querySelector\\('a, button'\\);\\s*if \\(first\\) first\\.focus\\(\\);", branches.Groups["open"].Value);
        Assert.Matches("(?m)^\\s*hamburger\\.focus\\(\\);", branches.Groups["close"].Value);
    }

    [Theory] // a FAQ item is also a .reveal element: its open state must not be the reveal class, or closing hides it
    [InlineData("/")]
    [InlineData("/en/contact")]
    public async Task Faq_open_state_is_not_the_reveal_class(string path)
    {
        var html = await PageAsync(path);
        var items = Regex.Matches(html, "<div class=\"(faq-item[^\"]*)\">").Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(items);
        Assert.All(items, c => Assert.Contains("reveal", c)); // the premise: the item is revealed with "active"

        var js = await f.NewClient().GetStringAsync("/js/main.js");
        Assert.Contains("entry.target.classList.add('active');", js); // the reveal state
        var faq = Regex.Match(js, "// 6\\. FAQ ACCORDION(.*?)// 7\\.", RegexOptions.Singleline);
        Assert.True(faq.Success, "FAQ accordion block not found");
        Assert.DoesNotContain("'active'", faq.Groups[1].Value);
        Assert.Contains("item.classList.add('is-open');", faq.Groups[1].Value);
        Assert.Contains("otherItem.classList.remove('is-open');", faq.Groups[1].Value);

        foreach (var sheet in new[] { "/css/sections.css", "/css/pages.css", "/css/style.css" })
            Assert.DoesNotMatch("\\.faq-item\\.active\\b", await f.NewClient().GetStringAsync(sheet));
        Assert.Contains(".faq-item.is-open", await f.NewClient().GetStringAsync("/css/sections.css"));
    }

    [Theory] // a values text written as a list becomes its items; anything else is not a list
    [InlineData("الابتكار، الشراكة، الشفافية", new[] { "الابتكار", "الشراكة", "الشفافية" })]
    [InlineData("Innovation, partnership ,transparency", new[] { "Innovation", "partnership", "transparency" })]
    [InlineData("الابتكار، Innovation, الشراكة", new[] { "الابتكار", "Innovation", "الشراكة" })] // both separators in one text
    [InlineData(" ، a ،، b , ", new[] { "a", "b" })]                                              // empty items dropped
    [InlineData("A single sentence without separators", new string[0])]
    [InlineData("one, ", new string[0])]                                                              // one item is not a list
    [InlineData("", new string[0])]
    public void Values_text_splits_into_chips_on_arabic_and_latin_commas(string text, string[] expected) =>
        Assert.Equal(expected, ContentText.ListItems(text));

    [Fact] // the home values card shows the seeded values as chips in both languages; vision and mission stay prose
    public async Task Home_values_render_as_chips_in_a_full_width_row()
    {
        foreach (var (path, count) in new[] { ("/", 7), ("/en", 7) })
        {
            var html = await PageAsync(path);
            var row = Regex.Match(html, "<div class=\"about-values\">(.*?)<div class=\"value-card[^\"]*\">(.*?)</div>\\s*<div class=\"value-card[^\"]*\">(.*?)</div>\\s*<div class=\"value-card[^\"]*\">(.*?)</div>\\s*</div>", RegexOptions.Singleline);
            Assert.True(row.Success, $"{path}: no values row");
            Assert.DoesNotContain("about-values", Regex.Match(html, "<div class=\"about-content[^\"]*\">.*?</a>", RegexOptions.Singleline).Value); // not inside the about column
            Assert.Contains("<p", row.Groups[2].Value);                 // vision: prose
            Assert.Contains("<p", row.Groups[3].Value);                 // mission: prose
            var chips = Regex.Matches(row.Groups[4].Value, "<li>(.*?)</li>").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
            Assert.Equal(count, chips.Count);
            Assert.DoesNotContain(chips, c => c.Contains(',') || c.Contains('،') || c != c.Trim());
        }
    }

    [Fact] // the admin is told how a values list is written; the hint exists in both languages and names both commas
    public async Task Values_field_carries_the_list_hint_in_both_languages()
    {
        foreach (var text in new[] { Msg.Ar(MessageKeys.Admin.ListHint), Msg.En(MessageKeys.Admin.ListHint) })
        {
            Assert.Contains("«،»", text);
            Assert.Contains("«,»", text);
        }
        Assert.NotEqual(Msg.Ar(MessageKeys.Admin.ListHint), Msg.En(MessageKeys.Admin.ListHint));

        var client = await f.SignedInClientAsync();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Admin/Content/Edit"));
        // The hint sits after the values_text inputs and before the next field's pair.
        var field = html.IndexOf("name=\"content[values_text].Ar\"", StringComparison.Ordinal);
        Assert.True(field > 0, "values_text field not found");
        var next = html.IndexOf("class=\"lang-pair", field, StringComparison.Ordinal);
        var hint = html.IndexOf($"<p class=\"field-hint lang-pair-hint\">{Msg.Ar(MessageKeys.Admin.ListHint)}</p>", field, StringComparison.Ordinal);
        Assert.True(hint > field && (next < 0 || hint < next), $"hint at {hint}, field at {field}, next pair at {next}");
        Assert.Single(Regex.Matches(html, "lang-pair-hint")); // only that field
    }

    private async Task SetContentAsync(string key, string value) =>
        await f.DbAsync(async db =>
        {
            var row = await db.SiteContents.SingleAsync(s => s.SectionKey == key);
            row.ContentAr = value;
            return await db.SaveChangesAsync();
        });
}
