using System.Net;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>Bounded stores fail CLOSED; existing accounts are always tracked.</summary>
public class LoginThrottleStoreTests
{
    private sealed class Monitor(SecurityOptions value) : IOptionsMonitor<SecurityOptions>
    {
        public SecurityOptions CurrentValue => value;
        public SecurityOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<SecurityOptions, string?> listener) => null;
    }

    private static (InMemoryLoginThrottle Throttle, FakeTimeProvider Time) Create(int maxNames = 3, int maxIps = 3)
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var options = new SecurityOptions { Login = { MaxTrackedKeys = maxNames, MaxTrackedIpKeys = maxIps } };
        return (new InMemoryLoginThrottle(time, new Monitor(options)), time);
    }

    [Fact]
    public void Full_unknown_name_store_refuses_a_new_name_instead_of_ignoring_it()
    {
        var (t, _) = Create(maxNames: 3, maxIps: 100);
        for (var i = 0; i < 3; i++) t.RecordFailure(LoginKey.ForUnknownName("junk" + i), "10.0.0." + i);

        var fresh = LoginKey.ForUnknownName("never-seen");
        Assert.True(t.IsLocked(fresh, "10.9.9.9"));             // fail closed
        t.RecordFailure(fresh, "10.9.9.9");
        Assert.True(t.IsLocked(fresh, "10.9.9.9"));
        Assert.False(t.IsLocked(LoginKey.ForUnknownName("junk0"), "10.9.9.8")); // a tracked key still answers normally
    }

    [Fact]
    public void Account_keys_are_always_tracked_even_when_the_name_store_is_full()
    {
        var (t, _) = Create(maxNames: 2, maxIps: 1000);
        for (var i = 0; i < 2; i++) t.RecordFailure(LoginKey.ForUnknownName("junk" + i), "10.0.1." + i);

        var admin = LoginKey.ForAccount(1);
        Assert.False(t.IsLocked(admin, "10.1.0.0"));
        for (var i = 0; i < 5; i++) t.RecordFailure(admin, "10.1.0." + (i + 1));
        Assert.True(t.IsLocked(admin, "10.2.0.0"));               // locked from any IP: the user dimension is on
    }

    [Fact]
    public void Full_ip_store_refuses_a_new_ip()
    {
        var (t, _) = Create(maxNames: 100, maxIps: 3);
        for (var i = 0; i < 3; i++) t.RecordFailure(LoginKey.ForUnknownName("n" + i), "10.0.2." + i);

        Assert.True(t.IsLocked(LoginKey.ForAccount(7), "10.0.2.99"));  // new IP: fail closed
        Assert.False(t.IsLocked(LoginKey.ForAccount(7), "10.0.2.0"));  // tracked IP, 1 failure: open
    }

    [Fact]
    public void Store_frees_room_once_entries_expire()
    {
        var (t, time) = Create(maxNames: 2, maxIps: 2);
        t.RecordFailure(LoginKey.ForUnknownName("a"), "10.0.3.1");
        t.RecordFailure(LoginKey.ForUnknownName("b"), "10.0.3.2");
        Assert.True(t.IsLocked(LoginKey.ForUnknownName("c"), "10.0.3.1"));

        time.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(2));
        Assert.False(t.IsLocked(LoginKey.ForUnknownName("c"), "10.0.3.3"));
    }

    [Fact]
    public void Lock_held_in_a_full_store_is_not_purged_early()
    {
        var (t, time) = Create(maxNames: 1, maxIps: 100);
        var key = LoginKey.ForUnknownName("x");
        for (var i = 0; i < 5; i++) t.RecordFailure(key, "10.0.4." + i);
        time.Advance(TimeSpan.FromMinutes(14));
        Assert.True(t.IsLocked(key, "10.0.4.99"));
    }

    [Fact]
    public void Success_resets_the_user_but_not_the_ip()
    {
        var (t, _) = Create(maxNames: 100, maxIps: 100);
        for (var i = 0; i < 19; i++) t.RecordFailure(LoginKey.ForUnknownName("g" + i), "10.0.5.1");
        t.ResetUser(LoginKey.ForAccount(3));
        t.RecordFailure(LoginKey.ForUnknownName("g-last"), "10.0.5.1");
        Assert.True(t.IsLocked(LoginKey.ForAccount(3), "10.0.5.1"));
    }

    [Theory]
    [InlineData("Admin", "ＡＤＭＩＮ")]
    [InlineData(" admin ", "ａｄｍｉｎ")]
    public void Unknown_name_key_folds_case_and_width(string a, string b) =>
        Assert.Equal(LoginKey.ForUnknownName(a), LoginKey.ForUnknownName(b));

    [Fact]
    public void Invalid_unicode_name_does_not_throw() =>
        Assert.StartsWith("n:", LoginKey.ForUnknownName("ab\uD800").Value);
}

/// <summary>Throttle, session invalidation, admin route scoping and the error page over HTTP.</summary>
[Collection("App")]
public class SessionAndRouteTests(TestAppFactory f)
{
    private static async Task<string?> LoginResult(HttpClient client, string user, string password)
    {
        var response = await client.PostLoginAsync(user, password);
        return response.StatusCode == HttpStatusCode.Redirect ? "SIGNED-IN" : LoginError(await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> TextAsync(HttpResponseMessage r) =>
        WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());

    [Fact]
    public async Task Successful_login_does_not_reset_the_ip_counter()
    {
        var (user, password) = await f.CreateAdminAsync();
        var (other, otherPassword) = await f.CreateAdminAsync();
        var ip = NewIp();
        var client = f.NewClient(ip);
        for (var i = 0; i < 19; i++)
            Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await LoginResult(client, Unique("ghost"), "x"));
        Assert.Equal("SIGNED-IN", await LoginResult(f.NewClient(ip), user, password));
        Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await LoginResult(f.NewClient(ip), Unique("ghost"), "x")); // 20th
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), await LoginResult(f.NewClient(ip), other, otherPassword));
    }

    private static async Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string current, string next)
    {
        var token = await client.GetTokenAsync("/Admin/Settings");
        return await client.PostAsync("/Admin/Settings/ChangePassword", Form(token,
            ("CurrentPassword", current), ("NewPassword", next), ("ConfirmPassword", next)));
    }

    [Fact]
    public async Task Password_change_ends_other_sessions_and_keeps_this_one()
    {
        var (user, password) = await f.CreateAdminAsync();
        var a = f.NewClient();
        var b = f.NewClient();
        Assert.Equal("SIGNED-IN", await LoginResult(a, user, password));
        Assert.Equal("SIGNED-IN", await LoginResult(b, user, password));
        Assert.Equal(HttpStatusCode.OK, (await b.GetAsync("/Admin/Dashboard")).StatusCode);

        var newPassword = "N" + Guid.NewGuid().ToString("N")[..20];
        var change = await ChangePasswordAsync(a, password, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, change.StatusCode);

        var stale = await b.GetAsync("/Admin/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        Assert.Contains("/Account/Login", stale.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/Admin/Dashboard")).StatusCode);

        Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await LoginResult(f.NewClient(), user, password));
        Assert.Equal("SIGNED-IN", await LoginResult(f.NewClient(), user, newPassword));
    }

    [Fact] // a cookie for a deleted account dies on the next request
    public async Task Session_of_a_deleted_account_is_rejected()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        Assert.Equal("SIGNED-IN", await LoginResult(client, user, password));
        await f.DbAsync(db => db.Users.Where(u => u.Username == user).ExecuteDeleteAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task Change_password_current_check_is_throttled()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        Assert.Equal("SIGNED-IN", await LoginResult(client, user, password));
        var hashBefore = await f.DbAsync(db => db.Users.Where(u => u.Username == user).Select(u => u.PasswordHash).SingleAsync());

        for (var i = 0; i < 5; i++)
        {
            var r = await ChangePasswordAsync(client, "wrong-" + i, "Abcdefghij12");
            Assert.Contains(Msg.Ar(MessageKeys.CurrentPasswordWrong), await TextAsync(r));
        }

        var locked = await ChangePasswordAsync(client, password, "Abcdefghij12");
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
        Assert.Contains(Msg.Ar(MessageKeys.LoginLocked), await TextAsync(locked));
        Assert.Equal(hashBefore, await f.DbAsync(db => db.Users.Where(u => u.Username == user).Select(u => u.PasswordHash).SingleAsync()));
        // Same account key as the login: the login is locked too.
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), await LoginResult(f.NewClient(), user, password));
    }

    [Theory]
    [InlineData("/Admin/Home")]
    [InlineData("/Admin/Home/Index")]
    [InlineData("/Admin/Account/Login")]
    [InlineData("/Admin/Home/Error")]
    public async Task Admin_route_does_not_reach_public_controllers(string path) =>
        Assert.Equal(HttpStatusCode.NotFound, (await f.NewClient().GetAsync(path)).StatusCode);

    [Fact] // admin pages and links still work, logout lands on /Account/Login
    public async Task Admin_route_still_serves_admin_controllers_and_logout_url_is_canonical()
    {
        var client = await f.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/admin/events")).StatusCode);
        var html = await (await client.GetAsync("/Admin/Dashboard")).Content.ReadAsStringAsync();
        Assert.Contains("action=\"/Account/Logout\"", html);
        var token = ExtractToken(html);
        var logout = await client.PostAsync("/Account/Logout", Form(token));
        Assert.Equal("/Account/Login", logout.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Error_page_shows_no_developer_text()
    {
        var html = await TextAsync(await f.NewClient().GetAsync("/Home/Error"));
        Assert.DoesNotContain("Development", html);
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT", html);
        Assert.Contains(Msg.Ar(MessageKeys.ErrorGeneric), html);
    }

    [Fact] // the login form still posts to the pinned route
    public async Task Login_form_posts_to_the_pinned_route()
    {
        var html = await (await f.NewClient().GetAsync("/Account/Login")).Content.ReadAsStringAsync();
        Assert.Contains("action=\"/Account/Login\"", html);
    }
}
