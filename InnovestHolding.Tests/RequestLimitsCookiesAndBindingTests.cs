using System.Net;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// The 32 KB cap on the public forms. On .NET 9 the framework's EndpointRoutingMiddleware applies an
/// endpoint's [RequestSizeLimit] to the server's body-size feature at endpoint selection — before the antiforgery
/// filter reads the form. TestServer exposes no IHttpMaxRequestBodySizeFeature, so a fake one is installed.
/// (A custom UseEndpointRequestSizeLimit middleware was measured redundant and is not used.)
/// </summary>
[Collection("App")]
public class RequestSizeLimitTests(TestAppFactory f)
{
    private sealed class FakeBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => false;
        public long? MaxRequestBodySize { get; set; } = 30_000_000; // Kestrel's default
    }

    /// <summary>Every POST endpoint of the action (Register/Contact have three: /forms/…, /en/forms/… and /Home/…).</summary>
    private List<Endpoint> EndpointsOf(string controller, string action) =>
        f.Services.GetRequiredService<EndpointDataSource>().Endpoints.Where(e =>
            e.Metadata.GetMetadata<ControllerActionDescriptor>() is { } a
            && a.ControllerName == controller && a.ActionName == action
            && e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true
            // Routable endpoints only: MapFallbackToController adds an inert, non-route copy of every action.
            && e is RouteEndpoint).ToList();

    [Theory]
    [InlineData("Contact")]
    [InlineData("Register")]
    public void Public_form_endpoint_carries_a_32768_size_limit(string action)
    {
        var endpoints = EndpointsOf("Forms", action);
        Assert.Equal(3, endpoints.Count); // /forms/{name}, /en/forms/{name} and the /Home/{action} alias
        foreach (var endpoint in endpoints)
        {
            var limit = endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>();
            Assert.NotNull(limit);
            Assert.Equal(32_768, limit.MaxRequestBodySize);
            Assert.Equal(InputLimits.PublicBodyBytes, limit.MaxRequestBodySize);
        }
    }

    /// <summary>
    /// End to end through Program's real pipeline: a test-only startup filter installs a fake server feature and
    /// records the limit it holds at the FIRST read of the body (the antiforgery filter's form read). Red when the
    /// action's [RequestSizeLimit] is removed.
    /// </summary>
    [Fact]
    public async Task Program_pipeline_applies_the_32768_limit_before_the_form_is_read()
    {
        var probes = new System.Collections.Concurrent.ConcurrentDictionary<string, Probe>();
        await using var app = f.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new ProbeStartupFilter(probes))));
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, NewIp());

        var token = await client.GetTokenAsync("/");
        async Task<long?> LimitSeenBy(string path, FormUrlEncodedContent body)
        {
            var id = Guid.NewGuid().ToString("N");
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = body };
            request.Headers.Add(ProbeStartupFilter.Header, id);
            await client.SendAsync(request);
            return probes[id].LimitAtFirstRead;
        }

        var email = Unique("cap") + "@example.com";
        Assert.Equal(32_768, await LimitSeenBy("/Home/Contact", Form(token,
            ("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m"))));
        Assert.Equal(InputLimits.LoginBodyBytes, await LimitSeenBy("/Account/Login", Form(token, ("Username", "x"), ("Password", "x"))));
    }

    private sealed class ProbeStartupFilter(
        System.Collections.Concurrent.ConcurrentDictionary<string, Probe> probes)
        : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public const string Header = "X-Test-Size-Probe";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var id))
                {
                    var feature = new FakeBodySizeFeature();
                    var probe = new Probe();
                    context.Features.Set<IHttpMaxRequestBodySizeFeature>(feature);
                    context.Request.Body = new FirstReadStream(context.Request.Body,
                        () => probe.LimitAtFirstRead ??= feature.MaxRequestBodySize);
                    probes[id.ToString()] = probe;
                }
                await nextMiddleware();
            });
            next(app);
        };
    }

    private sealed class Probe
    {
        public long? LimitAtFirstRead { get; set; }
    }

    /// <summary>Pass-through body stream that runs a callback before the first read.</summary>
    private sealed class FirstReadStream(Stream inner, Action onFirstRead) : Stream
    {
        private bool _read;
        private void Touch() { if (!_read) { _read = true; onFirstRead(); } }
        public override int Read(byte[] buffer, int offset, int count) { Touch(); return inner.Read(buffer, offset, count); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        { Touch(); return await inner.ReadAsync(buffer, ct); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        { Touch(); return inner.ReadAsync(buffer, offset, count, ct); }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}

/// <summary>
/// The cookie policy as CONFIGURED (Testing = non-Development, i.e. the production branch).
/// The cookie-flags test sees the emitted flags over an https client, where SameAsRequest would also emit "secure" and Lax/Strict
/// are the framework defaults — so it cannot prove these lines. This reads the options the app really uses.
/// </summary>
[Collection("App")]
public class CookiePolicyTests(TestAppFactory f)
{
    [Fact]
    public void Auth_cookie_is_always_secure_httponly_and_lax()
    {
        var cookie = f.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme).Cookie;
        Assert.Equal("Innovest.Auth", cookie.Name);
        Assert.Equal(CookieSecurePolicy.Always, cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        Assert.True(cookie.HttpOnly);
    }

    [Fact]
    public void Antiforgery_cookie_is_always_secure_httponly_and_strict()
    {
        var cookie = f.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie;
        Assert.Equal(CookieSecurePolicy.Always, cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
        Assert.True(cookie.HttpOnly);
    }
}

/// <summary>Model-binding messages with no field name read cleanly (no "حقل  غير").</summary>
[Collection("App")]
public class ModelBindingMessageTests(TestAppFactory f)
{
    [Fact]
    public void Messages_without_a_field_name_have_no_double_space()
    {
        // The accessors resolve the resx per call in the CURRENT UI culture (an Arabic request here).
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("ar");
        var m = f.Services.GetRequiredService<IOptions<MvcOptions>>().Value.ModelBindingMessageProvider;
        var noField = new[]
        {
            m.NonPropertyAttemptedValueIsInvalidAccessor("x"),
            m.NonPropertyUnknownValueIsInvalidAccessor(),
            m.ValueIsInvalidAccessor("x"),
            m.NonPropertyValueMustBeANumberAccessor(),
            m.ValueMustNotBeNullAccessor("x"),
            m.MissingKeyOrValueAccessor(),
            m.MissingRequestBodyRequiredValueAccessor(),
        };
        foreach (var message in noField)
        {
            Assert.DoesNotContain("  ", message);
            Assert.Contains(message, new[] { Msg.Ar(MessageKeys.InvalidValueNoField), Msg.Ar(MessageKeys.ValueRequiredNoField) });
        }

        // The accessors that DO receive a field name still show it.
        Assert.Contains("FIELD", m.AttemptedValueIsInvalidAccessor("x", "FIELD"));
        Assert.Contains("FIELD", m.MissingBindRequiredValueAccessor("FIELD"));
    }
}

/// <summary>Over HTTP: the nav editor now accepts mailto:, tel: and relative links.</summary>
[Collection("App")]
public class NavHrefTests(TestAppFactory f)
{
    [Theory]
    [InlineData("mailto:info@example.com")]
    [InlineData("tel:+966500000000")]
    [InlineData("page.html")]
    public async Task Nav_accepts_mailto_tel_and_relative_links(string href)
    {
        var client = await f.SignedInClientAsync();
        var token = await client.GetTokenAsync("/Admin/Nav");
        var title = Unique("nav")[..30];
        var response = await client.PostAsync("/Admin/Nav/Create", Form(token, ("TitleAr", title), ("Href", href)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(href, await f.DbAsync(db => db.NavItems.Where(n => n.TitleAr == title).Select(n => n.Href).SingleAsync()));
    }
}
