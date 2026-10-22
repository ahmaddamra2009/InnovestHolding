using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Seo;

namespace InnovestHolding.Tests;

/// <summary>Bilingual / SEO / time pure-code tests (no database, no host).</summary>
public class BilingualUnitTests
{
    // ── The one fallback rule ──
    [Theory]
    [InlineData("عربي", null, true, "عربي", true)]
    [InlineData("عربي", "", true, "عربي", true)]
    [InlineData("عربي", "   ", true, "عربي", true)]
    [InlineData("عربي", " English ", true, "English", false)]
    [InlineData("عربي", "English", false, "عربي", false)]   // the Arabic page never shows English
    [InlineData("", null, true, "", false)]                 // nothing to fall back to: empty, not marked
    public void Pick_falls_back_to_Arabic_only_on_the_English_page(string ar, string? en, bool english, string value, bool fallback)
    {
        var t = Bilingual.Pick(ar, en, english);
        Assert.Equal(value, t.Value);
        Assert.Equal(fallback, t.IsFallback);
        Assert.Equal(fallback ? "ar" : null, t.Lang);
        Assert.Equal(fallback ? "rtl" : null, t.Dir);
    }

    [Theory]
    [InlineData("INNOVEST HOLDING", null, true, "")]
    [InlineData("INNOVEST HOLDING", null, false, "INNOVEST HOLDING")]
    [InlineData("x", "Eyebrow", true, "Eyebrow")]
    public void Optional_never_falls_back(string ar, string? en, bool english, string value)
    {
        var t = Bilingual.Optional(ar, en, english);
        Assert.Equal(value, t.Value);
        Assert.False(t.IsFallback);
    }

    // ── Normaliser table (the HTTP side is in CultureRoutingTests) ──
    [Theory]
    [InlineData("/ar", "/")]
    [InlineData("/ar/", "/")]
    [InlineData("/AR", "/")]
    [InlineData("/Home", "/")]
    [InlineData("/home/", "/")]
    [InlineData("/Home/Index", "/")]
    [InlineData("/HOME/INDEX/", "/")]
    [InlineData("/en/", "/en")]
    [InlineData("/EN", "/en")]
    [InlineData("/En/", "/en")]
    [InlineData("/en/Index", "/en")]
    [InlineData("/en/index/", "/en")]
    [InlineData("/en", null)]
    [InlineData("/", null)]
    [InlineData("/Home/Contact", null)]
    [InlineData("/Home/Error", null)]
    [InlineData("/Admin", null)]
    public void Normalizer_targets(string path, string? target) =>
        Assert.Equal(target, CultureUrlNormalizer.Target(path));

    // ── IPv6 /64 keying: one subscriber holds a whole /64 and can rotate the low 64 bits ──
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8:1:2:aaaa:bbbb:cccc:dddd", "2001:db8:1:2::/64")]
    [InlineData("2001:db8:1:2::1", "2001:db8:1:2::/64")]
    [InlineData("::1", "::/64")]
    public void Client_key_is_the_ipv4_address_or_the_ipv6_slash_64(string ip, string key) =>
        Assert.Equal(key, RateLimitPolicies.ClientKey(IPAddress.Parse(ip)));

    [Fact]
    public void Two_addresses_of_one_ipv6_slash_64_share_a_key_and_another_prefix_does_not()
    {
        var a = RateLimitPolicies.ClientKey(IPAddress.Parse("2001:db8:aa:bb:1::1"));
        var b = RateLimitPolicies.ClientKey(IPAddress.Parse("2001:db8:aa:bb:ffff:ffff:ffff:fffe"));
        var c = RateLimitPolicies.ClientKey(IPAddress.Parse("2001:db8:aa:bc:1::1"));
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    // ── Timestamps ──
    private static readonly TimeZoneInfo Plus3 = TimeZoneInfo.CreateCustomTimeZone("Test+3", TimeSpan.FromHours(3), "Test+3", "Test+3");

    [Fact]
    public void Local_to_utc_uses_the_zone_offset()
    {
        var utc = TimestampUtcConversion.ToUtc(new DateTime(2026, 7, 1, 12, 0, 0), Plus3);
        Assert.Equal(new DateTime(2026, 7, 1, 9, 0, 0), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void Local_to_utc_applies_each_dates_own_dst_offset()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); // CET/CEST
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0), TimestampUtcConversion.ToUtc(new DateTime(2026, 7, 1, 12, 0, 0), zone));  // summer +2
        Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0), TimestampUtcConversion.ToUtc(new DateTime(2026, 1, 15, 12, 0, 0), zone)); // winter +1
        // 2026-03-29 02:30 does not exist in CET (spring-forward gap): converted with the base offset, never an exception.
        Assert.Equal(new DateTime(2026, 3, 29, 1, 30, 0), TimestampUtcConversion.ToUtc(new DateTime(2026, 3, 29, 2, 30, 0), zone));
    }

    [Fact]
    public void Time_markup_is_iso_8601_utc_with_z_and_a_utc_fallback_text()
    {
        using var w = new StringWriter();
        TimeDisplay.Utc(new DateTime(2026, 10, 22, 9, 30, 0)).WriteTo(w, System.Text.Encodings.Web.HtmlEncoder.Default);
        Assert.Equal("<time class=\"utc-time\" datetime=\"2026-10-22T09:30:00Z\">2026/10/22 09:30 UTC</time>", w.ToString());
    }

    // ── Content text → markup ──
    [Theory]
    [InlineData("آخر {الأخبار} والتحديثات", true, "آخر ", "الأخبار", " والتحديثات")]
    [InlineData("بدون تمييز", true, "بدون تمييز", "", "")]
    [InlineData("{a} {b}", false, null, null, null)]
    [InlineData("a } b {", false, null, null, null)]
    [InlineData("a { b", false, null, null, null)]
    public void Highlight_parser_accepts_one_pair_only(string value, bool ok, string? prefix, string? h, string? suffix)
    {
        Assert.Equal(ok, ContentText.TryParseHighlight(value, out var p, out var hh, out var s));
        if (ok) { Assert.Equal(prefix, p); Assert.Equal(h, hh); Assert.Equal(suffix, s); }
    }

    [Fact]
    public void Highlight_and_lines_encode_every_part()
    {
        static string Html(Microsoft.AspNetCore.Html.IHtmlContent c)
        {
            using var w = new StringWriter();
            c.WriteTo(w, System.Text.Encodings.Web.HtmlEncoder.Default);
            return w.ToString();
        }
        var title = Html(ContentText.Highlight(new LText("<b>x</b> {<script>alert(1)</script>}", false)));
        Assert.DoesNotContain("<script>", title);
        Assert.DoesNotContain("<b>", title);
        Assert.Contains("<span class=\"gold gold-underline\">&lt;script&gt;", title);
        Assert.Equal("a&lt;i&gt;<br>b", Html(ContentText.Lines(new LText("a<i>\nb", false))));
    }

    // ── Base URL rule ──
    [Theory]
    [InlineData("https://www.innovest-holding.com", true, "https://www.innovest-holding.com")]
    [InlineData("https://www.innovest-holding.com/", true, "https://www.innovest-holding.com")]
    [InlineData("  https://example.org:8443/ ", true, "https://example.org:8443")]
    [InlineData("http://example.org", false, null)]
    [InlineData("https://example.org/path", false, null)]
    [InlineData("https://example.org/?q=1", false, null)]
    [InlineData("https://example.org/#x", false, null)]
    [InlineData("https://user:pw@example.org", false, null)]
    [InlineData("javascript:alert(1)", false, null)]
    [InlineData("//example.org", false, null)]
    [InlineData("", false, null)]
    public void Base_url_accepts_https_host_only(string value, bool ok, string? normalized)
    {
        Assert.Equal(ok, SiteUrl.TryNormalize(value, out var n));
        if (ok) Assert.Equal(normalized, n);
    }

    // ── Resx parity ──
    private static Dictionary<string, string> ReadResx(string file) =>
        System.Xml.Linq.XDocument.Load(file).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? "");

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InnovestHolding.csproj"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static IEnumerable<string> AllMessageKeys() =>
        new[] { typeof(MessageKeys) }.Concat(typeof(MessageKeys).GetNestedTypes())
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral))
            .Select(f => (string)f.GetRawConstantValue()!);

    [Fact]
    public void Resx_files_have_the_same_non_empty_keys_and_cover_every_MessageKey()
    {
        var root = RepoRoot();
        var ar = ReadResx(Path.Combine(root, "Resources", "SharedResource.resx"));
        var en = ReadResx(Path.Combine(root, "Resources", "SharedResource.en.resx"));
        var keys = AllMessageKeys().ToList();

        Assert.True(ar.Count > 100, $"only {ar.Count} keys read — wrong file?");
        Assert.Empty(ar.Keys.Except(en.Keys));
        Assert.Empty(en.Keys.Except(ar.Keys));
        Assert.Empty(ar.Where(p => string.IsNullOrWhiteSpace(p.Value)).Select(p => p.Key));
        Assert.Empty(en.Where(p => string.IsNullOrWhiteSpace(p.Value)).Select(p => p.Key));
        Assert.Empty(keys.Except(ar.Keys));
        Assert.Equal(keys.Count, ar.Count); // no orphan resource without a constant
    }

    [Fact]
    public void Compiled_resources_resolve_per_culture_with_arabic_as_the_fallback()
    {
        Assert.Equal("EN", Msg.Ar(MessageKeys.Ui.LangSwitchText));
        Assert.Equal("ع", Msg.En(MessageKeys.Ui.LangSwitchText));
        // A culture with no satellite falls back to the neutral (Arabic) resource.
        Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), Msg.Resources.GetString(MessageKeys.LoginInvalid, new CultureInfo("fr")));
    }

    // ── No Arabic literal left in the public templates ──
    [Fact]
    public void Public_views_contain_no_arabic_letters()
    {
        var root = RepoRoot();
        var files = Directory.GetFiles(Path.Combine(root, "Views", "Home"), "*.cshtml")
            .Concat([Path.Combine(root, "Views", "Shared", "_Layout.cshtml"), Path.Combine(root, "Views", "Shared", "_SeoHead.cshtml"),
                     Path.Combine(root, "Views", "Shared", "Error.cshtml")])
            .ToList();
        Assert.True(files.Count >= 6, $"scanned {files.Count} files");
        var hits = files.SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line)))
            .Where(x => Regex.IsMatch(x.line, "[؀-ۿ]"))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.Empty(hits);
    }
}
