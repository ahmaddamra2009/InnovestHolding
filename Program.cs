using System.Net;
using InnovestHolding;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Infrastructure.Seo;
using InnovestHolding.Infrastructure.Site;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var isTesting = builder.Environment.IsEnvironment("Testing");

// ── Database safety (item 1) ──
// Development and Testing may only ever use LocalDB. This runs before Build(), so it also runs under
// `dotnet ef` (whose host resolver stops the program at Build()). The guard never echoes the string.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (LocalDbGuard.AppliesTo(builder.Environment))
    LocalDbGuard.EnsureLocalDb(connectionString, builder.Environment.EnvironmentName, "configuration");
// In EVERY environment (Production included): a non-LocalDB database only on the production host (IIS, or the explicit
// INNOVEST_ALLOW_REMOTE_DB=1). A developer machine pointed at the production database stops here, before any connection.
ProductionDbGuard.Ensure(connectionString, Environment.GetEnvironmentVariable, "configuration");

// ── Services ──
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();

// ── Localization (ANTIGRAVITY §9): ar (default) + en, the URL is the only input (Cultures.Configure) ──
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(Cultures.Configure);
// Framework model-binding messages from the resx, in the request's language.
builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ModelBindingMessagesSetup>();

// Razor writes Arabic (and every other non-ASCII letter) as is instead of &#x…; entities. HTML-sensitive characters
// (< > & " ') are still encoded — the allowed ranges never include them.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

builder.Services.AddControllersWithViews(options =>
    {
        // Every POST/PUT/PATCH/DELETE in the app validates an antiforgery token (fail-closed for new actions).
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddInnovestRateLimiting();

// Uploaded images (Infrastructure/Media): root from Uploads:RootPath (env Uploads__RootPath), default App_Data/uploads.
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.AddSingleton<MediaStore>();
builder.Services.AddStaticAssetCompression();

// AppDbContext + the site content cache and its invalidation interceptor (one helper, shared with the tests).
builder.Services.AddAppDbContext(connectionString);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "Innovest.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // A password change ends every other session: the cookie's password fingerprint must match.
        options.Events.OnValidatePrincipal = AdminSession.ValidateAsync;
    });

// Antiforgery tokens and the auth cookie are encrypted with these keys. Persisted outside Testing so
// that an app-pool recycle on the host does not invalidate every open form and admin session.
// On Windows the key files are encrypted at rest with DPAPI, machine scope: a copied key
// file is useless off that machine. Elsewhere they stay unencrypted — protect the folder by ACL.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("InnovestHolding");
if (!isTesting)
{
    dataProtection.PersistKeysToFileSystem(
        new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")));
    if (OperatingSystem.IsWindows())
        dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

// Forwarded headers: OFF unless proxies are explicitly listed (ForwardedHeaders__KnownProxies__0=<ip>).
// Never trust every proxy — that would let any client fake its IP and walk around the rate limits.
var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var useForwardedHeaders = knownProxies.Length > 0;
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (var proxy in knownProxies)
            options.KnownProxies.Add(IPAddress.Parse(proxy.Trim()));
    });
}

var app = builder.Build();

// ── Migrate + seed ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // The EFFECTIVE connection (a DI override included) is checked again, in every environment, before the migration;
    // neither call connects. This final verdict is the one logged; every connection open is checked again by
    // ProductionDbGuardInterceptor.
    var finalVerdict = ProductionDbGuard.Ensure(db.Database.GetConnectionString(), Environment.GetEnvironmentVariable, "DbContext");
    // A remote database is logged as a Warning because Production's minimum log level is Warning (an Information line
    // would be dropped). Where that line ends up on the host (Event Log, stdout log) has not been verified. Never the
    // connection string.
    if (finalVerdict.Decision == ProductionDbGuard.Decision.LocalDb)
        app.Logger.LogInformation("Database guard: {Decision}", finalVerdict.ToString());
    else
        app.Logger.LogWarning("Database guard: remote database allowed by {Decision}", finalVerdict.ToString()); // e.g. "IisHost (APP_POOL_ID)"
    if (LocalDbGuard.AppliesTo(app.Environment))
    {
        // The EFFECTIVE connection the migration will use (catches a DI override too). Does not connect.
        var effective = db.Database.GetConnectionString();
        LocalDbGuard.EnsureLocalDb(effective, app.Environment.EnvironmentName, "DbContext");
        app.Logger.LogInformation("Database: {Database}", LocalDbGuard.Describe(effective));
    }
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);

    // One-time: timestamps written by the old code in the server's local time become UTC (marker-guarded).
    await TimestampUtcConversion.RunAsync(db, TimeZoneInfo.Local, app.Logger);

    // Uploads: the folder exists (a missing one would only break uploads, never the public site), and orphans of a
    // failed delete or an interrupted save are moved to the quarantine (never deleted: MediaStore.SweepAsync).
    var media = scope.ServiceProvider.GetRequiredService<MediaStore>();
    try
    {
        Directory.CreateDirectory(media.Root);
        var sweep = await media.SweepAsync(db);
        app.Logger.LogInformation("Uploads folder ready. Orphan sweep: {Outcome}; {Moved} of {Candidates} file(s) moved to the quarantine.",
            sweep.Outcome, sweep.Moved, sweep.Candidates);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        app.Logger.LogError(ex, "The uploads folder is not usable: image uploads will fail until the app pool can write to it.");
    }

    if (!app.Environment.IsDevelopment()
        && !SiteUrl.TryNormalize(await db.SiteContents.Where(c => c.SectionKey == SiteUrl.ContentKey)
               .Select(c => c.ContentAr).FirstOrDefaultAsync(), out _))
    {
        app.Logger.LogWarning("The site base URL is not set (admin → تعديل المحتوى → SEO): pages are served without " +
                              "canonical/hreflang/og:url and /sitemap.xml answers 404.");
    }
}

// ── Middleware ──
if (useForwardedHeaders)
    app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();
app.UseRobotsHeader();
app.UseMediaFiles();     // /media/{key}.{ext}: uploaded images, before anything else can answer that path
app.UseStaticAssets();   // compression for static paths only; immutable caching for versioned URLs
app.UseCultureUrlNormalizer();
app.UseRouting();
app.UseRequestLocalization();   // after routing (route values exist), before the limiter (its messages are localized)
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ──
// Admin controllers only: /Admin/Home/* and /Admin/Account/* do not resolve.
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "" },
    constraints: new { controller = new AdminControllerConstraint() });

// The public pages: every entry of the route table, in Arabic and under /en (Infrastructure/Site/SiteRoutes).
foreach (var page in SiteRoutes.All)
{
    var path = page.Path.TrimStart('/');
    var defaults = new { controller = page.Controller, action = page.Action };
    app.MapControllerRoute(page.Key + ".ar", path, defaults);
    app.MapControllerRoute(page.Key + ".en", path.Length == 0 ? "en" : "en/" + path, defaults);
}

// Detail pages: /news/{slug}, /forum/events/{slug} and their /en twins; the slug must already be canonical (lower-case
// words joined by hyphens — the normaliser 301s other spellings first).
foreach (var detail in SiteRoutes.Details)
{
    var prefix = detail.Prefix.Trim('/');
    var defaults = new { controller = detail.Controller, action = detail.Action };
    var constraints = new { slug = new Microsoft.AspNetCore.Routing.Constraints.RegexRouteConstraint(Slugs.RouteRegex) };
    app.MapControllerRoute(detail.Key + ".ar", prefix + "/{slug}", defaults, constraints);
    app.MapControllerRoute(detail.Key + ".en", "en/" + prefix + "/{slug}", defaults, constraints);
}

// Conventional routes reach ONLY Home (Error, and /Home → 301 by the normaliser) and Account (login/logout). Admin
// controllers are reachable under /Admin/… only: /Dashboard, /Events, /News… now answer the 404 page instead of the
// admin controller (or a redirect to the login page). No defaults: "/" is the home route above.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}",
    defaults: null,
    constraints: new { controller = "^(?i:home|account)$" });

// Everything else: the branded 404 in the URL's language. ":nonfile" keeps a missing static file a plain 404.
app.MapFallbackToController("{*path:nonfile}", nameof(InnovestHolding.Controllers.HomeController.NotFoundPage), "Home");

app.Run();

// Makes the compiler-generated Program class public for WebApplicationFactory<Program> in the tests.
public partial class Program { }
