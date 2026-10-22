using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using InnovestHolding.Data;
using InnovestHolding.Models;
using Microsoft.Extensions.DependencyInjection;

namespace InnovestHolding.Tests;

public static partial class TestHelpers
{
    private static int _ipCounter;

    /// <summary>A client IP no other test uses → its own rate-limit and lockout partition.</summary>
    public static string NewIp()
    {
        var n = Interlocked.Increment(ref _ipCounter);
        return $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}";
    }

    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    public static HttpClient NewClient(this TestAppFactory f, string? ip = null)
    {
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, ip ?? NewIp());
        return client;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    public static string ExtractToken(string html)
    {
        var m = TokenRegex().Match(html);
        if (!m.Success) throw new InvalidOperationException("No antiforgery token in the page.");
        return m.Groups[1].Value;
    }

    public static async Task<string> GetTokenAsync(this HttpClient client, string path = "/Account/Login")
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractToken(await response.Content.ReadAsStringAsync());
    }

    public static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    public static FormUrlEncodedContent Form(string? token, params (string Key, string Value)[] fields) =>
        Form(token is null ? fields : fields.Append(("__RequestVerificationToken", token)).ToArray());

    public static async Task<T> DbAsync<T>(this TestAppFactory f, Func<AppDbContext, Task<T>> query)
    {
        using var scope = f.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>
    /// Inserts a dedicated admin with a random password generated now. The credentials stay in memory;
    /// the seeded admin and its password are never used by the tests.
    /// </summary>
    public static async Task<(string User, string Password)> CreateAdminAsync(this TestAppFactory f)
    {
        var user = Unique("test-admin");
        var password = Guid.NewGuid().ToString("N");
        await f.DbAsync(async db =>
        {
            db.Users.Add(new AppUser { Username = user, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4) });
            return await db.SaveChangesAsync();
        });
        return (user, password);
    }

    public static async Task<HttpResponseMessage> PostLoginAsync(this HttpClient client, string user, string password)
    {
        var token = await client.GetTokenAsync("/Account/Login");
        return await client.PostAsync("/Account/Login", Form(token, ("Username", user), ("Password", password)));
    }

    /// <summary>Signs in and asserts the redirect away from the login page.</summary>
    public static async Task<HttpClient> SignedInClientAsync(this TestAppFactory f, string? ip = null)
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient(ip);
        var response = await client.PostLoginAsync(user, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("Login", response.Headers.Location!.OriginalString);
        return client;
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response)
    {
        Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString() ?? "");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    /// <summary>The text of the login page's error box, or null.</summary>
    public static string? LoginError(string html)
    {
        var m = Regex.Match(html, "<div class=\"login-error\">([^<]*)</div>");
        return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;
    }
}
