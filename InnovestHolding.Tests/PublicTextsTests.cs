using System.Net;
using System.Reflection;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// The override layer of the public interface texts (PublicTexts, Admin › رسائل وأزرار الموقع): no row = the resx text,
/// an override per language on pages and in JSON replies, placeholders kept, reset, admin-only keys untouched.
/// </summary>
[Collection("App")]
public class PublicTextsTests(TestAppFactory f)
{
    private const string Screen = "/Admin/SiteTexts";

    private static string Ar(string key) => $"texts[{key}].Ar";
    private static string En(string key) => $"texts[{key}].En";

    private async Task<(HttpClient Client, string Token)> AdminAsync()
    {
        var c = await f.SignedInClientAsync();
        return (c, await c.GetTokenAsync(Screen));
    }

    private Task<int> RowsAsync(string resxKey) =>
        f.DbAsync(db => db.SiteContents.CountAsync(s => s.SectionKey == PublicTexts.Prefix + resxKey));

    private Task DeleteRowsAsync(params string[] resxKeys) => f.DbAsync(async db =>
    {
        var keys = resxKeys.Select(k => PublicTexts.Prefix + k).ToList();
        db.SiteContents.RemoveRange(await db.SiteContents.Where(s => keys.Contains(s.SectionKey)).ToListAsync());
        return await db.SaveChangesAsync();
    });

    private static async Task<string> Html(HttpClient c, string url) => await (await c.GetAsync(url)).Content.ReadAsStringAsync();

    // ── Registry ──

    [Fact]
    public async Task Registry_lists_every_public_ui_key_exists_in_both_resx_and_has_no_admin_key()
    {
        var ui = typeof(MessageKeys.Ui).GetFields(BindingFlags.Public | BindingFlags.Static).Select(x => (string)x.GetValue(null)!).ToList();
        Assert.DoesNotContain(ui, k => PublicTexts.Find(k) is null);
        Assert.Equal(PublicTexts.All.Count, PublicTexts.All.Select(t => t.ResxKey).Distinct().Count());
        foreach (var t in PublicTexts.All)
        {
            Assert.NotNull(Msg.Ar(t.ResxKey));
            Assert.NotNull(Msg.En(t.ResxKey));
            Assert.True(t.RowKey.Length <= 50, t.RowKey); // SiteContents.SectionKey is nvarchar(50)
            Assert.Contains(PublicTexts.Groups, g => g.Id == t.Group);
        }
        Assert.DoesNotContain(PublicTexts.All, t => t.ResxKey.StartsWith("Admin_", StringComparison.Ordinal));
        Assert.Null(PublicTexts.Find(MessageKeys.LoginInvalid));
        Assert.Null(PublicTexts.Find(MessageKeys.LoginLocked));
        Assert.Null(PublicTexts.Find(MessageKeys.Fields.Password));
        // No migration or seed writes an override: a fresh database renders exactly the resx.
        Assert.Equal(0, await f.DbAsync(db => db.SiteContents.CountAsync(s => s.SectionKey.StartsWith(PublicTexts.Prefix))));
    }

    [Theory]
    [InlineData("{0} مطلوب", "{0} حقل إلزامي", true)]
    [InlineData("{0} مطلوب", "حقل إلزامي", false)]       // placeholder dropped
    [InlineData("{0} مطلوب", "{0} {1} إلزامي", false)]   // placeholder added
    [InlineData("{0} مطلوب", "{0} {x}", false)]          // not a valid format string
    [InlineData("{0} / {1}", "{1} من {0}", true)]          // reordered: allowed
    [InlineData("تابعنا", "{0} تابعنا", false)]           // a default with no placeholder gets none
    [InlineData("تابعنا", "تابعنا {اليوم}", true)]          // never formatted: braces are free
    public void Placeholders_must_match_the_default(string defaultText, string value, bool ok) =>
        Assert.Equal(ok, PublicTexts.PlaceholdersMatch(value, defaultText));

    // ── Pages ──

    [Fact]
    public async Task Page_text_default_with_no_row_then_override_per_language_then_reset()
    {
        var key = MessageKeys.Ui.ContactSubmit;
        await DeleteRowsAsync(key);
        var anon = f.NewClient();
        Assert.Contains(Msg.Ar(key), await Html(anon, "/contact"));
        Assert.Contains(Msg.En(key), await Html(anon, "/en/contact"));

        var ar = "أرسل-" + Guid.NewGuid().ToString("N")[..8];
        var en = Unique("send")[..20];
        var (c, t) = await AdminAsync();
        try
        {
            var save = await c.PostAsync(Screen + "?tab=forms", Form(t, (Ar(key), ar), (En(key), en)));
            Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
            var arHtml = await Html(anon, "/contact");
            var enHtml = await Html(anon, "/en/contact");
            Assert.Contains(ar, arHtml);
            Assert.DoesNotContain(Msg.Ar(key), arHtml);
            Assert.DoesNotContain(en, arHtml);
            Assert.Contains(en, enHtml);
            Assert.DoesNotContain(Msg.En(key), enHtml);
            Assert.DoesNotContain(ar, enHtml);

            // The admin screen shows the stored value, the default as placeholder and the "custom" chip.
            var screen = await Html(c, Screen + "?tab=forms");
            Assert.Contains($"value=\"{ar}\"", screen);
            Assert.Contains($"placeholder=\"{Msg.En(key)}\"", screen);

            // Per-field reset is a button of the same form: the row is deleted AND the other edits posted with it are saved.
            var other = MessageKeys.Ui.ContactSocial;
            var otherEn = Unique("Follow")[..20];
            var reset = await c.PostAsync(Screen + "?tab=forms", Form(t, ("reset", key), (Ar(key), ar), (En(key), en), (En(other), otherEn)));
            Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
            Assert.EndsWith("#t-" + key.Replace("_", ""), reset.Headers.Location!.OriginalString);
            Assert.Equal(0, await RowsAsync(key));
            Assert.Equal(otherEn, await f.DbAsync(db => db.SiteContents.Where(x => x.SectionKey == PublicTexts.Prefix + other).Select(x => x.ContentEn).SingleAsync()));
            Assert.Contains(Msg.Ar(key), await Html(anon, "/contact"));
            Assert.Contains(Msg.En(key), await Html(anon, "/en/contact"));

            // Group reset (same form): every text of the group goes back to its default.
            var group = await c.PostAsync(Screen, Form(t, ("resetGroup", "details")));
            Assert.Equal(HttpStatusCode.Redirect, group.StatusCode);
            Assert.Equal(0, await RowsAsync(other));
        }
        finally
        {
            await DeleteRowsAsync(key, MessageKeys.Ui.ContactSocial);
        }
    }

    [Fact]
    public async Task English_only_override_keeps_the_arabic_default_and_404_title_follows_the_language()
    {
        var key = MessageKeys.Ui.NotFoundTitle;
        var en = Unique("Lost")[..20];
        var (c, t) = await AdminAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync(Screen, Form(t, (Ar(key), ""), (En(key), en)))).StatusCode);
            var anon = f.NewClient();
            var enPage = await anon.GetAsync("/en/no-such-page-" + Guid.NewGuid().ToString("N"));
            Assert.Equal(HttpStatusCode.NotFound, enPage.StatusCode);
            var enHtml = await enPage.Content.ReadAsStringAsync();
            Assert.Contains($"{en} | ", enHtml); // the tab title too (PublicControllerBase)
            Assert.Contains(en, enHtml);
            var arHtml = await (await anon.GetAsync("/no-such-page-" + Guid.NewGuid().ToString("N"))).Content.ReadAsStringAsync();
            Assert.Contains(Msg.Ar(key), arHtml);
            Assert.DoesNotContain(en, arHtml);

            // Both languages emptied: the row goes away (no empty rows).
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync(Screen, Form(t, (Ar(key), " "), (En(key), "")))).StatusCode);
            Assert.Equal(0, await RowsAsync(key));
        }
        finally
        {
            await DeleteRowsAsync(key);
        }
    }

    // ── JSON replies ──

    private static (string, string)[] Contact(string email, string name = "QA") => [("FullName", name), ("Email", email), ("Subject", "s"), ("Message", "m")];

    [Fact]
    public async Task Thank_you_and_validation_json_use_the_override_in_the_request_language()
    {
        var success = MessageKeys.ContactSuccess;
        var required = MessageKeys.Required;
        var field = MessageKeys.Fields.FullName;
        var okAr = "شكراً-" + Guid.NewGuid().ToString("N")[..8];
        var okEn = Unique("Thanks")[..22];
        var nameEn = Unique("YourName")[..20];
        var (c, t) = await AdminAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync(Screen, Form(t,
                (Ar(success), okAr), (En(success), okEn),
                (Ar(required), "{0} — هذا الحقل إلزامي"), (En(required), "Please fill in {0}"),
                (En(field), nameEn)))).StatusCode);

            var ar = f.NewClient();
            var tAr = await ar.GetTokenAsync("/contact");
            var en = f.NewClient();
            var tEn = await en.GetTokenAsync("/en/contact");
            var email = Unique("pt") + "@example.com";

            Assert.Equal(okAr, (await (await ar.PostAsync("/forms/contact", Form(tAr, Contact(email)))).JsonAsync()).GetProperty("message").GetString());
            Assert.Equal(okEn, (await (await en.PostAsync("/en/forms/contact", Form(tEn, Contact(email)))).JsonAsync()).GetProperty("message").GetString());

            // Validation: the overridden message, its {0} filled with the (overridden in English) field name.
            var badEn = await en.PostAsync("/en/forms/contact", Form(tEn, Contact(email, "")));
            Assert.Equal(HttpStatusCode.BadRequest, badEn.StatusCode);
            Assert.Equal($"Please fill in {nameEn}", (await badEn.JsonAsync()).GetProperty("message").GetString());
            var badAr = await ar.PostAsync("/forms/contact", Form(tAr, Contact(email, "")));
            Assert.Equal($"{Msg.Ar(field)} — هذا الحقل إلزامي", (await badAr.JsonAsync()).GetProperty("message").GetString());
        }
        finally
        {
            await DeleteRowsAsync(success, required, field);
        }
    }

    [Fact]
    public async Task Rate_limit_429_json_uses_the_override_in_the_request_language()
    {
        var key = MessageKeys.TooManyRequests;
        var limitAr = "تمهّل-" + Guid.NewGuid().ToString("N")[..8];
        var limitEn = Unique("SlowDown")[..22];
        var (c, t) = await AdminAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync(Screen, Form(t, (Ar(key), limitAr), (En(key), limitEn)))).StatusCode);
            var ip = NewIp();
            var ar = f.NewClient(ip);
            var tAr = await ar.GetTokenAsync("/contact");
            var en = f.NewClient(ip);
            var tEn = await en.GetTokenAsync("/en/contact");
            var email = Unique("rl") + "@example.com";
            HttpResponseMessage? limited = null;
            for (var i = 0; i < 10 && limited is null; i++)
            {
                var r = await en.PostAsync("/en/forms/contact", Form(tEn, Contact(email)));
                if (r.StatusCode == HttpStatusCode.TooManyRequests) limited = r;
            }
            Assert.NotNull(limited);
            Assert.Equal(limitEn, (await limited!.JsonAsync()).GetProperty("message").GetString());
            var limitedAr = await ar.PostAsync("/forms/contact", Form(tAr, Contact(email)));
            Assert.Equal(HttpStatusCode.TooManyRequests, limitedAr.StatusCode);
            Assert.Equal(limitAr, (await limitedAr.JsonAsync()).GetProperty("message").GetString());
        }
        finally
        {
            await DeleteRowsAsync(key);
        }
    }

    // ── Admin screen: validation, over-posting, authorization ──

    [Fact]
    public async Task Admin_refuses_changed_placeholders_and_markup_and_ignores_unknown_keys()
    {
        var (c, t) = await AdminAsync();
        var required = MessageKeys.Required;
        var social = MessageKeys.Ui.ContactSocial;
        try
        {
            var r = await c.PostAsync(Screen, Form(t, (Ar(required), "هذا الحقل إلزامي")));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with the error, nothing saved
            var html = await r.Content.ReadAsStringAsync();
            Assert.Contains("field-error", html);
            Assert.Contains("{0}", html);
            Assert.Equal(0, await RowsAsync(required));

            r = await c.PostAsync(Screen, Form(t, (En(social), "<b>Follow</b>")));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal(0, await RowsAsync(social));

            var unknown = "Admin_ContentSaved";
            r = await c.PostAsync(Screen, Form(t, (Ar(unknown), "x"), (Ar("Nope_" + Guid.NewGuid().ToString("N")[..8]), "x")));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(0, await RowsAsync(unknown));
        }
        finally
        {
            await DeleteRowsAsync(required, social, "Admin_ContentSaved");
        }
    }

    [Fact]
    public async Task Admin_texts_and_admin_pages_are_not_overridden()
    {
        // An override of a shared key (Msg_Required) applies to public requests only: the admin's own forms keep the resx.
        var key = MessageKeys.Required;
        var (c, t) = await AdminAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync(Screen, Form(t, (Ar(key), "{0} ZZ-override")))).StatusCode);
            var contentToken = await c.GetTokenAsync("/Admin/Content/Edit");
            var r = await c.PostAsync("/Admin/Content/Edit", Form(contentToken, ("content[hero_title].Ar", "")));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered with the Required error
            var error = await r.Content.ReadAsStringAsync();
            Assert.Contains("field-error", error);
            Assert.DoesNotContain("ZZ-override", error);
        }
        finally
        {
            await DeleteRowsAsync(key);
        }
    }

    [Fact]
    public async Task Screen_is_admin_only_and_its_posts_need_the_antiforgery_token()
    {
        var anon = f.NewClient();
        var get = await anon.GetAsync(Screen);
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Contains("/Account/Login", get.Headers.Location!.OriginalString);
        var post = await anon.PostAsync(Screen, Form((Ar(MessageKeys.Ui.ContactSocial), "x")));
        Assert.NotEqual(HttpStatusCode.OK, post.StatusCode);
        Assert.Equal(0, await RowsAsync(MessageKeys.Ui.ContactSocial));

        var admin = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(Screen)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(Screen, Form((Ar(MessageKeys.Ui.ContactSocial), "x")))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(Screen, Form(("reset", MessageKeys.Ui.ContactSocial)))).StatusCode);
        Assert.Equal(0, await RowsAsync(MessageKeys.Ui.ContactSocial));
    }
    // ── Fix round (Yasser M-C5A-1, L-C5A-1; Hassan A1; A5) ──

    [Theory]
    [InlineData(MessageKeys.Required, "{0,999999} مطلوب")]
    [InlineData(MessageKeys.Required, "{0:D9999999} مطلوب")]
    [InlineData(MessageKeys.Ui.PageOf, "{0} / {1:D9999999}")]
    [InlineData(MessageKeys.ContactSuccess, "شكراً {1:D9999999}")]
    public async Task Admin_refuses_alignment_or_format_parts_in_placeholders(string key, string value)
    {
        var (c, t) = await AdminAsync();
        try
        {
            var r = await c.PostAsync(Screen, Form(t, (Ar(key), value)));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode); // re-rendered, nothing saved
            var html = WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
            Assert.Contains(Msg.Ar(MessageKeys.Admin.SiteTextFormatSpec, Msg.Ar(key), "ع"), html);
            Assert.Equal(0, await RowsAsync(key));
        }
        finally
        {
            await DeleteRowsAsync(key);
        }
    }

    [Fact]
    public async Task A_format_part_written_outside_the_admin_is_ignored_and_the_reply_stays_small()
    {
        // Rows written straight to the database (not through the screen): the localizer must not format them.
        await f.DbAsync(async db =>
        {
            db.SiteContents.Add(new Models.SiteContent { SectionKey = PublicTexts.Prefix + MessageKeys.Required, ContentAr = "{0,999999}", ContentEn = "{0:D9999999}" });
            db.SiteContents.Add(new Models.SiteContent { SectionKey = PublicTexts.Prefix + MessageKeys.ContactSuccess, ContentAr = "{1:D9999999}", ContentEn = "{0,999999}" });
            return await db.SaveChangesAsync();
        });
        try
        {
            foreach (var (page, post, english) in new[] { ("/contact", "/forms/contact", false), ("/en/contact", "/en/forms/contact", true) })
            {
                var client = f.NewClient();
                var token = await client.GetTokenAsync(page);
                var email = Unique("fs") + "@example.com";
                var ok = await client.PostAsync(post, Form(token, Contact(email)));
                var okBody = await ok.Content.ReadAsStringAsync();
                Assert.True(okBody.Length < 400, $"reply of {okBody.Length} bytes");
                Assert.Equal(english ? Msg.En(MessageKeys.ContactSuccess) : Msg.Ar(MessageKeys.ContactSuccess), (await ok.JsonAsync()).GetProperty("message").GetString());

                var bad = await client.PostAsync(post, Form(token, Contact(email, "")));
                var badBody = await bad.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
                Assert.True(badBody.Length < 400, $"reply of {badBody.Length} bytes");
                var field = english ? Msg.En(MessageKeys.Fields.FullName) : Msg.Ar(MessageKeys.Fields.FullName);
                Assert.Equal(english ? Msg.En(MessageKeys.Required, field) : Msg.Ar(MessageKeys.Required, field), (await bad.JsonAsync()).GetProperty("message").GetString());
            }
        }
        finally
        {
            await DeleteRowsAsync(MessageKeys.Required, MessageKeys.ContactSuccess);
        }
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript :alert(1)")]
    [InlineData("vbscript:msgbox")]
    [InlineData("data:text/html,x")]
    public async Task Admin_refuses_script_and_data_schemes(string value)
    {
        var key = MessageKeys.Ui.ViewAll;
        var (c, t) = await AdminAsync();
        try
        {
            var r = await c.PostAsync(Screen, Form(t, (En(key), value)));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal(0, await RowsAsync(key));
        }
        finally
        {
            await DeleteRowsAsync(key);
        }
    }

    [Fact]
    public async Task Middleware_continues_with_resx_when_the_snapshot_cannot_be_loaded()
    {
        var called = false;
        var middleware = new PublicTextsMiddleware(_ => { called = true; return Task.CompletedTask; },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PublicTextsMiddleware>.Instance);
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = "/contact";
        await middleware.InvokeAsync(context, new ThrowingCache());
        Assert.True(called);
        Assert.False(context.Items.ContainsKey(PublicTextsMiddleware.ItemKey)); // no overrides: the localizer reads the resx
    }

    private sealed class ThrowingCache : ISiteContentCache
    {
        public Task<SiteSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("database unavailable");
        public void Invalidate() { }
    }

    /// <summary>Message keys the public code names that are deliberately NOT overridable (the login page's messages).</summary>
    private static readonly HashSet<string> NotPublic = [MessageKeys.LoginLocked, MessageKeys.LoginInvalid];

    /// <summary>The public source files: public controllers, public view models, validation attributes, form/site/SEO
    /// infrastructure, the rate limiter, model-binding messages, and every view outside Admin/Account.</summary>
    private static IEnumerable<string> PublicSources(string root)
    {
        var sep = Path.DirectorySeparatorChar.ToString();
        IEnumerable<string> Files(string dir, string pattern) =>
            Directory.Exists(Path.Combine(root, dir)) ? Directory.EnumerateFiles(Path.Combine(root, dir), pattern, SearchOption.AllDirectories) : [];
        return Files("Controllers", "*.cs").Where(p => !p.Contains(sep + "Admin" + sep) && !p.EndsWith("AccountController.cs"))
            .Concat(Files(Path.Combine("ViewModels", "Public"), "*.cs"))
            .Concat(new[] { "LeadFormViewModels.cs", "SiteViewModels.cs", "HomeViewModel.cs" }.Select(n => Path.Combine(root, "ViewModels", n)))
            .Concat(new[] { "RateLimitPolicies.cs", "HttpsUrlAttribute.cs", "MustBeTrueAttribute.cs", "SafeHrefAttribute.cs" }.Select(n => Path.Combine(root, "Infrastructure", n)))
            .Concat(Files(Path.Combine("Infrastructure", "Forms"), "*.cs"))
            .Concat(Files(Path.Combine("Infrastructure", "Site"), "*.cs"))
            .Concat(Files(Path.Combine("Infrastructure", "Seo"), "*.cs"))
            .Concat([Path.Combine(root, "Infrastructure", "Localization", "ModelBindingMessagesSetup.cs")])
            .Concat(Files("Views", "*.cshtml").Where(p => !p.Contains(sep + "Admin" + sep) && !p.Contains(sep + "Account" + sep) && !p.EndsWith("_AdminLayout.cshtml")))
            .Where(File.Exists).Distinct();
    }

    /// <summary>Every resx key the public source files name, resolved through the MessageKeys constants.</summary>
    private static (IReadOnlyCollection<string> Keys, int Files) PublicKeysUsed()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InnovestHolding.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(Type type, string prefix)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => x.IsLiteral))
                constants[prefix + field.Name] = (string)field.GetValue(null)!;
        }
        Add(typeof(MessageKeys), "");
        Add(typeof(MessageKeys.Ui), "Ui.");
        Add(typeof(MessageKeys.Fields), "Fields.");

        var files = PublicSources(dir!.FullName).ToList();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var main = new System.Text.RegularExpressions.Regex(@"\b(?:MessageKeys|K)\.((?:Ui|Fields|Admin)\.)?([A-Z]\w*)");
        var aliasF = new System.Text.RegularExpressions.Regex(@"\bF\.([A-Z]\w*)");
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match m in main.Matches(text))
            {
                if (m.Groups[1].Value == "Admin.") continue;
                if (constants.TryGetValue(m.Groups[1].Value + m.Groups[2].Value, out var key)) keys.Add(key);
            }
            if (text.Contains("MessageKeys.Fields;", StringComparison.Ordinal))
                foreach (System.Text.RegularExpressions.Match m in aliasF.Matches(text))
                    if (constants.TryGetValue("Fields." + m.Groups[1].Value, out var key)) keys.Add(key);
        }
        return (keys, files.Count);
    }

    [Fact]
    public void Every_message_and_field_key_named_by_public_code_is_overridable()
    {
        var (keys, files) = PublicKeysUsed();
        Assert.True(files >= 40, $"only {files} public source files scanned"); // an empty scan cannot pass
        Assert.Contains(MessageKeys.JoinSuccess, keys);                         // the scan sees the lead-form replies
        Assert.Contains(MessageKeys.Fields.FullName, keys);                     // and the [Display] names
        var missing = keys.Where(k => !NotPublic.Contains(k) && PublicTexts.Find(k) is null).Order().ToList();
        Assert.True(missing.Count == 0, "Not overridable: " + string.Join(", ", missing));
    }
}
