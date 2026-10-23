using System.Net;
using System.Text.RegularExpressions;
using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// Every admin toggle is exercised by posting EXACTLY what the rendered page posts —
/// the form is parsed out of the GET response (hidden inputs, checkbox state, the antiforgery token), never
/// hand-built. A hand-built body sends "true"/"false" and cannot see what Razor actually rendered.
/// </summary>
[Collection("App")]
public class RenderedFormTests(TestAppFactory f)
{
    // ── Minimal browser-like form serialisation ──
    private sealed record RenderedForm(string Action, List<Field> Fields);
    private sealed record Field(string Name, string Type, string Value, bool Checked);

    private static Dictionary<string, string?> Attributes(string tag)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(tag, "([A-Za-z_:][-A-Za-z0-9_:.]*)(?:\\s*=\\s*\"([^\"]*)\")?"))
            result.TryAdd(m.Groups[1].Value, m.Groups[2].Success ? WebUtility.HtmlDecode(m.Groups[2].Value) : null);
        return result;
    }

    private static List<RenderedForm> Forms(string html)
    {
        var forms = new List<RenderedForm>();
        foreach (Match form in Regex.Matches(html, "<form\\b([^>]*)>(.*?)</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            var action = Attributes(form.Groups[1].Value).GetValueOrDefault("action") ?? "";
            var fields = new List<Field>();
            foreach (Match input in Regex.Matches(form.Groups[2].Value, "<input\\b([^>]*)>", RegexOptions.IgnoreCase))
            {
                var a = Attributes(input.Groups[1].Value);
                var name = a.GetValueOrDefault("name");
                if (string.IsNullOrEmpty(name)) continue;
                var type = (a.GetValueOrDefault("type") ?? "text").ToLowerInvariant();
                // A present-but-valueless attribute and an absent attribute both mean "" for a browser,
                // except checkboxes, whose default value is "on".
                var value = a.GetValueOrDefault("value") ?? (type == "checkbox" ? "on" : "");
                fields.Add(new Field(name, type, value, a.ContainsKey("checked")));
            }
            foreach (Match area in Regex.Matches(form.Groups[2].Value, "<textarea\\b([^>]*)>(.*?)</textarea>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var name = Attributes(area.Groups[1].Value).GetValueOrDefault("name");
                if (!string.IsNullOrEmpty(name))
                    fields.Add(new Field(name, "textarea", WebUtility.HtmlDecode(area.Groups[2].Value.TrimStart('\r', '\n')), false));
            }
            forms.Add(new RenderedForm(action, fields));
        }
        return forms;
    }

    /// <summary>The single form posting to <paramref name="action"/> whose hidden <c>id</c> equals <paramref name="id"/>.</summary>
    private static RenderedForm FormFor(string html, string action, int? id = null) =>
        Forms(html).First(form => form.Action.Equals(action, StringComparison.OrdinalIgnoreCase)
            && (id is null || form.Fields.Any(x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase) && x.Value == id.ToString())));

    /// <summary>What a browser submits: every successful control; checkboxes only when checked (optionally overridden).</summary>
    private static FormUrlEncodedContent Submit(RenderedForm form, IReadOnlyDictionary<string, bool>? check = null) =>
        new(form.Fields
            .Where(x => x.Type is not ("submit" or "button" or "reset" or "image"))
            .Where(x => x.Type != "checkbox" || (check?.TryGetValue(x.Name, out var c) == true ? c : x.Checked))
            .Select(x => new KeyValuePair<string, string>(x.Name, x.Value)));

    private static async Task<string> Html(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task AssertSavedWithoutError(HttpClient client, HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var page = await Html(client, response.Headers.Location!.OriginalString);
        Assert.DoesNotContain("alert alert-error", page);
    }

    // ── Nav: «تعطيل» / «تفعيل» (hidden inputs must carry the current values) ──
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Nav_toggle_as_rendered_disables_then_enables_and_keeps_has_dropdown(bool hasDropdown)
    {
        var client = await f.SignedInClientAsync();
        var title = Unique("nav")[..30];
        var id = await f.DbAsync(async db =>
        {
            var item = new NavItem { TitleAr = title, Href = "#qa", HasDropdown = hasDropdown, IsActive = true, SortOrder = 900 };
            db.NavItems.Add(item);
            await db.SaveChangesAsync();
            return item.Id;
        });

        foreach (var expectedActive in new[] { false, true })
        {
            // C5: «ظاهر / مخفي» is its own POST (Views/Admin/Shared/_VisibleSwitch, id in the route).
            var form = FormFor(await Html(client, "/Admin/Nav"), $"/Admin/Nav/Toggle/{id}");
            await AssertSavedWithoutError(client, await client.PostAsync(form.Action, Submit(form)));
            var row = await f.DbAsync(db => db.NavItems.AsNoTracking().SingleAsync(n => n.Id == id));
            Assert.Equal(expectedActive, row.IsActive);
            Assert.Equal(hasDropdown, row.HasDropdown);
            Assert.Equal(title, row.TitleAr);
            Assert.Equal("#qa", row.Href);
        }
    }

    // ── Events / FAQ / News edit: the «مفعّل» checkbox, unchecked then checked ──
    public static TheoryData<string> EditPages => new() { "Events", "Faq", "News" };

    [Theory]
    [MemberData(nameof(EditPages))]
    public async Task Edit_page_is_active_checkbox_as_rendered_turns_off_and_on(string page)
    {
        var client = await f.SignedInClientAsync();
        var marker = Unique("qa")[..30];
        var id = await f.DbAsync(async db =>
        {
            switch (page)
            {
                case "Events":
                    var e = new Event { TitleAr = marker, EventDay = 5, EventMonthAr = "يوليو", IsActive = true };
                    db.Events.Add(e); await db.SaveChangesAsync(); return e.Id;
                case "Faq":
                    var q = new Faq { QuestionAr = marker, AnswerAr = "a", IsActive = true };
                    db.Faqs.Add(q); await db.SaveChangesAsync(); return q.Id;
                default:
                    var n = new News { TitleAr = marker, IsActive = true };
                    db.News.Add(n); await db.SaveChangesAsync(); return n.Id;
            }
        });
        Task<bool> IsActive() => f.DbAsync(db => page switch
        {
            "Events" => db.Events.Where(x => x.Id == id).Select(x => x.IsActive).SingleAsync(),
            "Faq" => db.Faqs.Where(x => x.Id == id).Select(x => x.IsActive).SingleAsync(),
            _ => db.News.Where(x => x.Id == id).Select(x => x.IsActive).SingleAsync()
        });

        foreach (var target in new[] { false, true })
        {
            var form = Forms(await Html(client, $"/Admin/{page}/Edit/{id}"))
                .Single(x => x.Action.StartsWith($"/Admin/{page}/Edit", StringComparison.OrdinalIgnoreCase));
            var response = await client.PostAsync(form.Action, Submit(form, new Dictionary<string, bool> { ["IsActive"] = target }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(target, await IsActive());
        }
    }

    // ── Messages / Registrations: «مقروء» toggle (posts the id only; the server flips the flag) ──
    [Theory]
    [InlineData("Messages")]
    [InlineData("Registrations")]
    public async Task Mark_read_as_rendered_flips_both_ways(string page)
    {
        var client = await f.SignedInClientAsync();
        var email = Unique("qa") + "@example.com";
        var id = await f.DbAsync(async db =>
        {
            if (page == "Messages")
            {
                var m = new ContactMessage { FullName = "QA", Email = email, Subject = "s", Message = "m", CreatedAt = DateTime.MaxValue.AddYears(-1) };
                db.ContactMessages.Add(m); await db.SaveChangesAsync(); return m.Id;
            }
            var r = new Registration { FullName = "QA", Email = email, Interest = "x", CreatedAt = DateTime.MaxValue.AddYears(-1) };
            db.Registrations.Add(r); await db.SaveChangesAsync(); return r.Id;
        });
        Task<bool> IsRead() => f.DbAsync(db => page == "Messages"
            ? db.ContactMessages.Where(x => x.Id == id).Select(x => x.IsRead).SingleAsync()
            : db.Registrations.Where(x => x.Id == id).Select(x => x.IsRead).SingleAsync());

        try
        {
            foreach (var expected in new[] { true, false })
            {
                var form = FormFor(await Html(client, $"/Admin/{page}"), $"/Admin/{page}/MarkRead", id);
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(form.Action, Submit(form))).StatusCode);
                Assert.Equal(expected, await IsRead());
            }
        }
        finally
        {
            // Far-future CreatedAt keeps the row on the first page; remove it so other tests' listings are unaffected.
            await f.DbAsync(db => page == "Messages"
                ? db.ContactMessages.Where(x => x.Id == id).ExecuteDeleteAsync()
                : db.Registrations.Where(x => x.Id == id).ExecuteDeleteAsync());
        }
    }

    // ── Stats: the range message names the field once (it once read «قيمة القيمة …») ──
    [Fact]
    public async Task Stats_out_of_range_value_message_names_the_field_once()
    {
        var client = await f.SignedInClientAsync();
        var stat = await f.DbAsync(db => db.Stats.AsNoTracking().OrderBy(x => x.Id).FirstAsync());
        var form = FormFor(await Html(client, "/Admin/Stats"), "/Admin/Stats/Update", stat.Id);
        var fields = form.Fields.Select(x => x.Name == "value" ? x with { Value = "-5" } : x).ToList();
        var response = await client.PostAsync(form.Action, Submit(form with { Fields = fields }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var page = WebUtility.HtmlDecode(await Html(client, response.Headers.Location!.OriginalString));
        Assert.Contains("حقل القيمة يجب أن يكون بين 0 و1000000000", page);
        Assert.DoesNotContain("قيمة القيمة", page);
        Assert.Equal(stat.Value, await f.DbAsync(db => db.Stats.Where(x => x.Id == stat.Id).Select(x => x.Value).SingleAsync()));
    }
}
