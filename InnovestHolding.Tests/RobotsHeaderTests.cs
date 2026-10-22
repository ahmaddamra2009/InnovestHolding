using System.Net;
using InnovestHolding.Infrastructure.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace InnovestHolding.Tests;

/// <summary>
/// The X-Robots-Tag rule on its own pipeline: an indexable path (/ or /en) is noindex unless the response is 200, so an
/// error page served under the home URL can never be indexed; static types never get the header.
/// </summary>
public class RobotsHeaderTests
{
    private static async Task<HttpResponseMessage> SendAsync(string path, int status, string contentType)
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(w => w.UseTestServer().Configure(app =>
            {
                app.UseRobotsHeader();
                app.Run(async ctx =>
                {
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
    public async Task Non_200_html_or_json_on_an_indexable_path_is_noindex(string path, int status, string type) =>
        Assert.Equal(RobotsHeader.Value, Robots(await SendAsync(path, status, type)));

    [Theory]
    [InlineData("/", 200, "text/html; charset=utf-8")]
    [InlineData("/en", 200, "text/html; charset=utf-8")]
    [InlineData("/x", 404, "text/css")]
    public async Task Indexable_200_and_non_page_types_have_no_header(string path, int status, string type) =>
        Assert.Null(Robots(await SendAsync(path, status, type)));

    [Fact]
    public async Task Other_html_paths_are_noindex_even_with_200() =>
        Assert.Equal(RobotsHeader.Value, Robots(await SendAsync("/Account/Login", 200, "text/html")));
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
