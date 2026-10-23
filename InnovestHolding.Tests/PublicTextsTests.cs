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

            // Per-field reset: the row is deleted, both languages show the resx again.
            var reset = await c.PostAsync($"{Screen}/Reset?key={key}&tab=forms", Form(t));
            Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
            Assert.Equal(0, await RowsAsync(key));
            Assert.Contains(Msg.Ar(key), await Html(anon, "/contact"));
            Assert.Contains(Msg.En(key), await Html(anon, "/en/contact"));
        }
        finally
        {
            await DeleteRowsAsync(key);
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
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(Screen + "/Reset?key=" + MessageKeys.Ui.ContactSocial, Form())).StatusCode);
        Assert.Equal(0, await RowsAsync(MessageKeys.Ui.ContactSocial));
    }
}
