using InnovestHolding.Infrastructure;
using System.Net;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

[Collection("App")]
public class PublicFormTests(TestAppFactory f)
{
    private async Task<(HttpClient Client, string Token)> PublicClientAsync()
    {
        var client = f.NewClient();
        return (client, await client.GetTokenAsync("/"));
    }

    private static (string, string)[] Contact(string email, string? fullName = "QA", string subject = "s", string message = "m") =>
        new[] { ("FullName", fullName ?? ""), ("Email", email), ("Subject", subject), ("Message", message) };

    private Task<int> ContactRows(string email) => f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email));
    private Task<int> RegistrationRows(string email) => f.DbAsync(db => db.Registrations.CountAsync(r => r.Email == email));

    private static async Task AssertJsonFailure(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.JsonAsync();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));
    }

    [Fact] // T12
    public async Task Missing_full_name_is_a_400_json_error()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("v") + "@example.com";
        await AssertJsonFailure(await c.PostAsync("/Home/Contact", Form(t, Contact(email, fullName: ""))));
        await AssertJsonFailure(await c.PostAsync("/Home/Register", Form(t, ("Email", email), ("Interest", "x"))));
        Assert.Equal(0, await ContactRows(email));
        Assert.Equal(0, await RegistrationRows(email));
    }

    [Fact] // T13
    public async Task Invalid_email_is_a_400()
    {
        var (c, t) = await PublicClientAsync();
        await AssertJsonFailure(await c.PostAsync("/Home/Contact", Form(t, Contact("not-an-email"))));
        await AssertJsonFailure(await c.PostAsync("/Home/Register",
            Form(t, ("FullName", "QA"), ("Email", "not-an-email"), ("Interest", "x"))));
    }

    [Fact] // T14
    public async Task Over_length_fields_are_400_never_500()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("len") + "@example.com";
        await AssertJsonFailure(await c.PostAsync("/Home/Contact", Form(t, Contact(email, fullName: new string('a', 201)))));
        await AssertJsonFailure(await c.PostAsync("/Home/Contact", Form(t, Contact(email, subject: new string('a', 301)))));
        await AssertJsonFailure(await c.PostAsync("/Home/Contact", Form(t, Contact(email, message: new string('a', 4001)))));
        // 4th request on this IP for Contact; Register is a separate partition.
        var (c2, t2) = await PublicClientAsync();
        await AssertJsonFailure(await c2.PostAsync("/Home/Register",
            Form(t2, ("FullName", new string('a', 201)), ("Email", email), ("Interest", "x"))));
        await AssertJsonFailure(await c2.PostAsync("/Home/Register",
            Form(t2, ("FullName", "QA"), ("Email", email), ("Interest", "x"), ("Phone", "<script>"))));
        Assert.Equal(0, await ContactRows(email));
        Assert.Equal(0, await RegistrationRows(email));
    }

    [Fact] // T15
    public async Task Message_of_exactly_4000_is_accepted()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("max") + "@example.com";
        var response = await c.PostAsync("/Home/Contact", Form(t, Contact(email, message: new string('a', 4000))));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await ContactRows(email));
    }

    // T16 — proves only that oversized FIELDS are refused ([StringLength] → 400) and nothing is written.
    // It does NOT prove the 32 KB body cap: TestServer has no IHttpMaxRequestBodySizeFeature (Stage 8 F2).
    // The cap is proven by RequestSizeLimitMiddlewareTests.
    [Fact]
    public async Task Oversized_fields_are_400_never_500_and_write_nothing()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("big") + "@example.com";
        var big = await c.PostAsync("/Home/Contact", Form(t, Contact(email, message: new string('a', 40_000))));
        Assert.Contains(big.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge });
        var huge = await c.PostAsync("/Home/Contact", Form(t, Contact(email, subject: new string('a', 1_048_576))));
        Assert.Contains(huge.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge });
        Assert.Equal(0, await ContactRows(email));
    }

    [Fact] // T17
    public async Task Honeypot_answers_success_and_writes_nothing()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("bot") + "@example.com";
        var contact = await c.PostAsync("/Home/Contact", Form(t, Contact(email).Append(("Website", "http://spam")).ToArray()));
        Assert.Equal(HttpStatusCode.OK, contact.StatusCode);
        Assert.True((await contact.JsonAsync()).GetProperty("success").GetBoolean());
        var register = await c.PostAsync("/Home/Register",
            Form(t, ("FullName", "QA"), ("Email", email), ("Interest", "x"), ("Website", "x")));
        Assert.True((await register.JsonAsync()).GetProperty("success").GetBoolean());
        Assert.Equal(0, await ContactRows(email));
        Assert.Equal(0, await RegistrationRows(email));
    }

    [Fact] // T18
    public async Task Register_ignores_over_posted_fields()
    {
        var (c, t) = await PublicClientAsync();
        var email = Unique("over") + "@example.com";
        var before = DateTime.Now;
        var response = await c.PostAsync("/Home/Register", Form(t,
            ("FullName", "QA"), ("Email", email), ("Interest", "x"),
            ("IsRead", "true"), ("Id", "999999"), ("CreatedAt", "2000-01-01")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = await f.DbAsync(db => db.Registrations.AsNoTracking().SingleAsync(r => r.Email == email));
        Assert.False(row.IsRead);
        Assert.NotEqual(999999, row.Id);
        Assert.True(row.CreatedAt >= before.AddMinutes(-1), $"CreatedAt {row.CreatedAt:o} was over-posted");
    }
}

[Collection("App")]
public class RateLimitTests(TestAppFactory f)
{
    private static FormUrlEncodedContent ContactForm(string token, string email) =>
        Form(token, ("FullName", "QA"), ("Email", email), ("Subject", "s"), ("Message", "m"));

    [Fact] // T19
    public async Task Sixth_contact_from_one_ip_is_429_with_a_json_message()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("rl") + "@example.com";
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Home/Contact", ContactForm(token, email))).StatusCode);

        var sixth = await client.PostAsync("/Home/Contact", ContactForm(token, email));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        var json = await sixth.JsonAsync();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));
        Assert.Equal(5, await f.DbAsync(db => db.ContactMessages.CountAsync(m => m.Email == email)));
    }

    [Fact] // T20 + T21
    public async Task Limit_is_per_ip_and_per_action()
    {
        var ip = NewIp();
        var client = f.NewClient(ip);
        var token = await client.GetTokenAsync("/");
        var email = Unique("rl2") + "@example.com";
        for (var i = 0; i < 5; i++)
            await client.PostAsync("/Home/Contact", ContactForm(token, email));
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/Home/Contact", ContactForm(token, email))).StatusCode);

        // Same IP, other action → its own partition.
        var register = await client.PostAsync("/Home/Register", Form(token, ("FullName", "QA"), ("Email", email), ("Interest", "x")));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        // Other IP, same action → allowed.
        var other = f.NewClient();
        var otherToken = await other.GetTokenAsync("/");
        Assert.Equal(HttpStatusCode.OK, (await other.PostAsync("/Home/Contact", ContactForm(otherToken, email))).StatusCode);
    }

    [Fact] // T22
    public async Task Thirty_first_login_post_from_one_ip_is_429()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/Account/Login");
        for (var i = 0; i < 30; i++)
        {
            var r = await client.PostAsync("/Account/Login", Form(token, ("Username", Unique("nobody")), ("Password", "x")));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }
        var last = await client.PostAsync("/Account/Login", Form(token, ("Username", "nobody"), ("Password", "x")));
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);

        // Stage 8 N5: the login page comes back (HTML, a fresh form) with the locked message — not bare text.
        Assert.Equal("text/html", last.Content.Headers.ContentType?.MediaType);
        var html = await last.Content.ReadAsStringAsync();
        Assert.Equal(UserMessages.LoginLocked, LoginError(html));
        Assert.NotEmpty(ExtractToken(html));
        Assert.DoesNotContain("value=\"nobody\"", html);
    }

    [Fact] // T23
    public async Task Spoofed_x_forwarded_for_does_not_reset_the_limit()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        var email = Unique("xff") + "@example.com";
        HttpStatusCode last = 0;
        for (var i = 0; i < 6; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/Home/Contact") { Content = ContactForm(token, email) };
            request.Headers.Add("X-Forwarded-For", $"203.0.113.{i + 1}");
            last = (await client.SendAsync(request)).StatusCode;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }
}
