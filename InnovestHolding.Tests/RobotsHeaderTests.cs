using System.Net;
using InnovestHolding.Infrastructure.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace InnovestHolding.Tests;

/// <summary>
/// The X-Robots-Tag rule on its own pipeline: the header follows the decision the page put in
/// HttpContext.Items[SeoPolicy.ItemKey]; no decision, or a status other than 200, is noindex, nofollow (fail-closed);
/// static types never get the header.
/// </summary>
public class RobotsHeaderTests
{
    private static async Task<HttpResponseMessage> SendAsync(string path, int status, string contentType, RobotsDecision? decision = null)
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(w => w.UseTestServer().Configure(app =>
            {
                app.UseRobotsHeader();
                app.Run(async ctx =>
                {
                    if (decision is { } d) ctx.Items[SeoPolicy.ItemKey] = d;
                    ctx.Response.StatusCode = status;
                    ctx.Response.ContentType = contentType;
                    await ctx.Response.WriteAsync("x");
                });
            }))
            .StartAsync();
        var response = await host.GetTestClient().GetAsync(path);
        await host.StopAsync();
        return response;
    }

    private static string? Robots(HttpResponseMessage r) =>
        r.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : null;

    [Theory]
    [InlineData("/", 404, "text/html; charset=utf-8")]
    [InlineData("/", 500, "text/html; charset=utf-8")]
    [InlineData("/en", 400, "application/json; charset=utf-8")]
    [InlineData("/en", 429, "application/json")]
    public async Task Non_200_html_or_json_is_noindex_even_with_an_index_decision(string path, int status, string type) =>
        Assert.Equal(RobotsHeader.Value, Robots(await SendAsync(path, status, type, RobotsDecision.Index)));

    [Theory]
    [InlineData("/", 200, "text/html; charset=utf-8", RobotsDecision.Index)]
    [InlineData("/x", 404, "text/css", null)]
    public async Task Indexable_200_and_non_page_types_have_no_header(string path, int status, string type, RobotsDecision? decision) =>
        Assert.Null(Robots(await SendAsync(path, status, type, decision)));

    [Fact]
    public async Task A_thin_page_is_noindex_follow() =>
        Assert.Equal(RobotsHeader.FollowValue, Robots(await SendAsync("/forum/speakers", 200, "text/html", RobotsDecision.NoIndexFollow)));

    [Theory] // no decision = a page someone forgot, the login page, the admin: fail-closed
    [InlineData("/Account/Login")]
    [InlineData("/")]
    public async Task A_page_without_a_decision_is_noindex_nofollow(string path) =>
        Assert.Equal(RobotsHeader.Value, Robots(await SendAsync(path, 200, "text/html")));
}

[Collection("App")]
public class NoindexMetaTests(TestAppFactory f)
{
    [Fact]
    public async Task Error_page_carries_noindex_meta_and_header()
    {
        var r = await f.NewClient().GetAsync("/Home/Error");
        Assert.Equal(RobotsHeader.Value, string.Join(",", r.Headers.GetValues("X-Robots-Tag")));
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\">", await r.Content.ReadAsStringAsync());
    }
}
