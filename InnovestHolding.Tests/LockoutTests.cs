using System.Net;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Localization;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

[Collection("App")]
public class LockoutTests(TestAppFactory f)
{
    private static async Task<string?> FailAsync(HttpClient client, string user, string password = "wrong-password")
    {
        var response = await client.PostLoginAsync(user, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return LoginError(await response.Content.ReadAsStringAsync());
    }

    private static async Task<bool> SignedInAsync(HttpClient client) =>
        (await client.GetAsync("/Admin/Dashboard")).StatusCode == HttpStatusCode.OK;

    [Fact]
    public async Task Five_failures_lock_the_username_even_from_another_ip()
    {
        var (user, password) = await f.CreateAdminAsync();
        var attacker = f.NewClient();
        for (var i = 0; i < 5; i++)
            Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await FailAsync(attacker, user));

        var owner = f.NewClient(); // different IP
        var response = await owner.PostLoginAsync(user, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), LoginError(await response.Content.ReadAsStringAsync()));
        Assert.False(await SignedInAsync(owner));
    }

    [Fact]
    public async Task Lock_expires_after_the_lockout_period()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        for (var i = 0; i < 5; i++) await FailAsync(client, user);
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), LoginError(await (await client.PostLoginAsync(user, password)).Content.ReadAsStringAsync()));

        f.Time.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        var response = await client.PostLoginAsync(user, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(await SignedInAsync(client));
    }

    [Fact]
    public async Task Successful_login_resets_the_counter()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        for (var i = 0; i < 4; i++) await FailAsync(client, user);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostLoginAsync(user, password)).StatusCode);

        var second = f.NewClient();
        for (var i = 0; i < 4; i++) Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await FailAsync(second, user));
        Assert.Equal(HttpStatusCode.Redirect, (await second.PostLoginAsync(user, password)).StatusCode);
    }

    [Fact]
    public async Task Twenty_failures_from_one_ip_lock_that_ip()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        for (var i = 0; i < 20; i++) await FailAsync(client, Unique("ghost"));

        var response = await client.PostLoginAsync(user, password);
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), LoginError(await response.Content.ReadAsStringAsync()));
        Assert.False(await SignedInAsync(client));

        // The same account from a clean IP is not affected (its username has no failures).
        Assert.Equal(HttpStatusCode.Redirect, (await f.NewClient().PostLoginAsync(user, password)).StatusCode);
    }

    [Fact]
    public async Task Unknown_user_and_wrong_password_look_the_same()
    {
        var (user, _) = await f.CreateAdminAsync();
        var a = await f.NewClient().PostLoginAsync(Unique("ghost"), "wrong-password");
        var b = await f.NewClient().PostLoginAsync(user, "wrong-password");
        Assert.Equal(a.StatusCode, b.StatusCode);
        Assert.Equal(LoginError(await a.Content.ReadAsStringAsync()), LoginError(await b.Content.ReadAsStringAsync()));
        Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), LoginError(await b.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Non_existent_username_locks_the_same_way()
    {
        var ghost = Unique("ghost");
        var client = f.NewClient();
        for (var i = 0; i < 5; i++) Assert.Equal(Msg.Ar(MessageKeys.LoginInvalid), await FailAsync(client, ghost));
        Assert.Equal(Msg.Ar(MessageKeys.LoginLocked), await FailAsync(f.NewClient(), ghost));
    }
}
