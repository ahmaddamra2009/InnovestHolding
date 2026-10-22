using System.Net;
using System.Text;
using InnovestHolding.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>H1 (Stage 4, re-run 1): the public-form and login limits cannot be walked around by path variants.</summary>
[Collection("App")]
public class RateLimitPathBypassTests(TestAppFactory f)
{
    /// <summary>Every URL that reached the action (or might) before the fix. Each must be 429 or 404/405 — never a fresh bucket.</summary>
    private static readonly string[] Variants =
    [
        "/Home/{0}/1", "/Home/{0}/77", "/Home/{0}/", "/home/{0}", "/HOME/{0}/",
        "/Admin/Home/{0}", "/Admin/Home/{0}/5", "/Home/{0}?x=1"
    ];

    private static readonly HttpStatusCode[] Refused =
        [HttpStatusCode.TooManyRequests, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed];

    [Fact]
    public async Task Contact_limit_is_not_reset_by_a_different_path()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("h1c") + "@example.com";
        FormUrlEncodedContent Body() => Form(token, ("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m"));

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Home/Contact", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/Home/Contact", Body())).StatusCode);

        foreach (var v in Variants)
        {
            var path = string.Format(v, "Contact");
            var status = (await client.PostAsync(path, Body())).StatusCode;
            Assert.True(Refused.Contains(status), $"{path} → {(int)status}");
        }
        Assert.Equal(5, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact]
    public async Task Register_limit_is_not_reset_by_a_different_path()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("h1r") + "@example.com";
        FormUrlEncodedContent Body() => Form(token, ("FullName", "QA"), ("Email", email), ("Interest", "x"));

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Home/Register", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/Home/Register", Body())).StatusCode);

        foreach (var v in Variants)
        {
            var path = string.Format(v, "Register");
            var status = (await client.PostAsync(path, Body())).StatusCode;
            Assert.True(Refused.Contains(status), $"{path} → {(int)status}");
        }
        Assert.Equal(5, await f.DbAsync(db => db.Registrations.CountAsync(r => r.Email == email)));
    }

    [Fact]
    public async Task Fresh_ip_on_a_path_variant_never_writes()
    {
        // Even with an untouched bucket, the id/admin variants must not reach the action at all.
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("h1v") + "@example.com";
        foreach (var path in new[] { "/Home/Contact/77", "/Admin/Home/Contact" })
        {
            var status = (await client.PostAsync(path,
                Form(token, ("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m")))).StatusCode;
            Assert.True(status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed, $"{path} → {(int)status}");
        }
        Assert.Equal(0, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact]
    public async Task Login_limit_is_not_reset_by_a_different_path()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/Account/Login");
        FormUrlEncodedContent Body() => Form(token, ("Username", Unique("nobody")), ("Password", "x"));
        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Account/Login", Body())).StatusCode);

        foreach (var path in new[] { "/Account/Login", "/Account/Login/1", "/account/login/", "/Admin/Account/Login", "/Admin/Account/Login/3" })
        {
            var status = (await client.PostAsync(path, Body())).StatusCode;
            Assert.True(Refused.Contains(status), $"{path} → {(int)status}");
        }
    }
}

/// <summary>H2 (Stage 4, re-run 1): collation-equivalent usernames share the account's lockout.</summary>
[Collection("App")]
public class LockoutVariantTests(TestAppFactory f)
{
    /// <summary>ASCII letter → its full-width form (U+FF41…), which SQL Server's CI collation treats as equal.</summary>
    private static string FullWidth(string s, int onlyIndex = -1)
    {
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            var convert = (onlyIndex < 0 || onlyIndex == i) && c is >= 'a' and <= 'z';
            sb.Append(convert ? (char)(c + 0xFEE0) : c);
        }
        return sb.ToString();
    }

    private static async Task<string?> PostAsync(HttpClient client, string user, string password)
    {
        var response = await client.PostLoginAsync(user, password);
        return response.StatusCode == HttpStatusCode.Redirect
            ? "SIGNED-IN"
            : LoginError(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Full_width_variant_of_a_locked_username_is_locked_too()
    {
        var (user, password) = await f.CreateAdminAsync();
        // Precondition: the database really resolves the variant to this account.
        var variant = FullWidth(user, 0);
        Assert.NotEqual(user, variant);
        Assert.Equal(1, await f.DbAsync(db => db.Users.CountAsync(u => u.Username == variant)));

        var attacker = f.NewClient();
        for (var i = 0; i < 5; i++)
            Assert.Equal(UserMessages.LoginInvalid, await PostAsync(attacker, user, "wrong-password"));

        Assert.Equal(UserMessages.LoginLocked, await PostAsync(f.NewClient(), variant, password));
        Assert.Equal(UserMessages.LoginLocked, await PostAsync(f.NewClient(), FullWidth(user), password));
        Assert.Equal(UserMessages.LoginLocked, await PostAsync(f.NewClient(), user.ToUpperInvariant(), password));
    }

    [Fact]
    public async Task Collation_ignorable_character_variant_shares_the_account_lockout()
    {
        // U+200D (zero-width joiner) is ignored by SQL_Latin1_General_CP1_CI_AS but NOT removed by NFKC,
        // so only keying on the ACCOUNT (not on any normalised spelling) closes this one.
        var (user, password) = await f.CreateAdminAsync();
        var variant = user.Insert(2, "\u200D");
        Assert.NotEqual(user, variant); // the joiner is really there (an editor cannot strip an escape)
        Assert.Equal(1, await f.DbAsync(db => db.Users.CountAsync(u => u.Username == variant)));

        for (var i = 0; i < 5; i++)
            Assert.Equal(UserMessages.LoginInvalid, await PostAsync(f.NewClient(), user, "wrong-password"));
        Assert.Equal(UserMessages.LoginLocked, await PostAsync(f.NewClient(), variant, password));
    }

    [Fact]
    public async Task Failures_spread_over_variants_count_against_one_account()
    {
        var (user, password) = await f.CreateAdminAsync();
        for (var i = 0; i < 5; i++)
            Assert.Equal(UserMessages.LoginInvalid, await PostAsync(f.NewClient(), FullWidth(user, i), "wrong-password"));

        Assert.Equal(UserMessages.LoginLocked, await PostAsync(f.NewClient(), user, password));
    }

    [Fact]
    public async Task Variant_login_succeeds_when_not_locked_and_signs_in_the_canonical_name()
    {
        var (user, password) = await f.CreateAdminAsync();
        var client = f.NewClient();
        Assert.Equal("SIGNED-IN", await PostAsync(client, FullWidth(user, 0), password));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/Dashboard")).StatusCode);
    }
}
