using System.Net;
using InnovestHolding.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

[Collection("App")]
public class HeadersTests(TestAppFactory f)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/css/style.css")]
    public async Task Responses_carry_the_security_headers(string path)
    {
        var response = await f.NewClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", Header(response, "Referrer-Policy"));
        Assert.StartsWith("max-age=", Header(response, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task Http_is_redirected_to_https()
    {
        var response = await f.NewClient().GetAsync("http://innovest.test/");
        Assert.Contains((int)response.StatusCode, new[] { 307, 308 });
        Assert.StartsWith("https://innovest.test", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Auth_and_antiforgery_cookies_are_secure_httponly_samesite()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://innovest.test"), AllowAutoRedirect = false, HandleCookies = false
        });
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());

        var get = await client.GetAsync("/Account/Login");
        var antiforgery = SetCookies(get).Single(c => c.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        AssertCookieFlags(antiforgery, "strict");

        using var post = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = Form(ExtractToken(await get.Content.ReadAsStringAsync()), ("Username", user), ("Password", password))
        };
        post.Headers.Add("Cookie", antiforgery.Split(';')[0]);
        var response = await client.SendAsync(post);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertCookieFlags(SetCookies(response).Single(c => c.StartsWith("Innovest.Auth=", StringComparison.Ordinal)), "lax");
    }

    private static void AssertCookieFlags(string setCookie, string sameSite)
    {
        var lower = setCookie.ToLowerInvariant();
        Assert.Contains("; secure", lower);
        Assert.Contains("; httponly", lower);
        Assert.Contains($"; samesite={sameSite}", lower);
    }

    private static IEnumerable<string> SetCookies(HttpResponseMessage r) =>
        r.Headers.TryGetValues("Set-Cookie", out var v) ? v : [];

    private static string Header(HttpResponseMessage r, string name) =>
        r.Headers.TryGetValues(name, out var v) ? string.Join(",", v)
        : r.Content.Headers.TryGetValues(name, out var c) ? string.Join(",", c) : "";
}

[Collection("App")]
public class ErrorHandlingTests(TestAppFactory f)
{
    [Fact]
    public async Task Error_page_exists_for_get_and_tokenless_post()
    {
        var client = f.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Home/Error")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Home/Error", Form())).StatusCode);
    }
}

public class UnitTests
{
    [Theory]
    [InlineData(@"Server=(localdb)\InnovestLocal;Database=X;Trusted_Connection=True", true)]
    [InlineData(@"Data Source=(LocalDB)\Other;Initial Catalog=X", true)]
    [InlineData("Server=example.invalid;Database=X;User Id=u;Password=p", false)]
    [InlineData("Server=localhost;Database=X", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("this is not a connection string", false)]
    public void LocalDbGuard_recognises_localdb_only(string? cs, bool expected) =>
        Assert.Equal(expected, LocalDbGuard.IsLocalDb(cs));

    [Fact]
    public void LocalDbGuard_message_never_echoes_the_connection_string()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LocalDbGuard.EnsureLocalDb("Server=example.invalid;Database=Secret;Password=hunter2", "Development", "configuration"));
        Assert.DoesNotContain("example.invalid", ex.Message);
        Assert.DoesNotContain("hunter2", ex.Message);
        Assert.Contains("requires a LocalDB connection", ex.Message);
    }

    [Theory]
    [InlineData("#about", true)]
    [InlineData("/Home/Index", true)]
    [InlineData("https://example.org/x", true)]
    [InlineData("http://example.org", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData(" JavaScript:alert(1)", false)]
    [InlineData("data:text/html,x", false)]
    [InlineData("//evil.example", false)]
    [InlineData("/\\evil.example", false)]
    [InlineData("ftp://example.org", false)]
    [InlineData("vbscript:x", false)]
    // mailto:, tel: and relative paths are allowed; everything else stays refused.
    [InlineData("mailto:info@example.com", true)]
    [InlineData("MAILTO:info@example.com", true)]
    [InlineData("tel:+966500000000", true)]
    [InlineData("page.html", true)]
    [InlineData("about/team?x=1#y", true)]
    [InlineData("about/a:b", true)]
    [InlineData("?q=1", true)]
    [InlineData("mailto:", false)]
    [InlineData("tel:", false)]
    [InlineData("data:image/svg+xml,x", false)]
    [InlineData("VBScript:x", false)]
    [InlineData("javascript://%0aalert(1)", false)]
    [InlineData("\\\\evil.example", false)]
    [InlineData("\\/evil.example", false)]
    [InlineData("about\\..\\x", false)]
    [InlineData("java\tscript:alert(1)", false)]
    public void SafeHref_allows_fragments_paths_http_mailto_and_tel_only(string href, bool expected) =>
        Assert.Equal(expected, SafeHrefAttribute.IsSafe(href));
}
