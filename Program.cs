using System.Net;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var isTesting = builder.Environment.IsEnvironment("Testing");

// ── Database safety (item 1) ──
// Development and Testing may only ever use LocalDB. This runs before Build(), so it also runs under
// `dotnet ef` (whose host resolver stops the program at Build()). The guard never echoes the string.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (LocalDbGuard.AppliesTo(builder.Environment))
    LocalDbGuard.EnsureLocalDb(connectionString, builder.Environment.EnvironmentName, "configuration");

// ── Services ──
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();

builder.Services.AddControllersWithViews(options =>
{
    // Every POST/PUT/PATCH/DELETE in the app validates an antiforgery token (fail-closed for new actions).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    var m = options.ModelBindingMessageProvider;
    m.SetAttemptedValueIsInvalidAccessor((_, field) => string.Format(UserMessages.InvalidValue, field));
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => UserMessages.InvalidValueNoField);
    m.SetUnknownValueIsInvalidAccessor(field => string.Format(UserMessages.InvalidValue, field));
    m.SetNonPropertyUnknownValueIsInvalidAccessor(() => UserMessages.InvalidValueNoField);
    m.SetValueIsInvalidAccessor(_ => UserMessages.InvalidValueNoField);
    m.SetValueMustBeANumberAccessor(field => string.Format(UserMessages.InvalidValue, field));
    m.SetNonPropertyValueMustBeANumberAccessor(() => UserMessages.InvalidValueNoField);
    m.SetValueMustNotBeNullAccessor(_ => UserMessages.ValueRequiredNoField);
    m.SetMissingBindRequiredValueAccessor(field => string.Format(UserMessages.ValueRequired, field));
    m.SetMissingKeyOrValueAccessor(() => UserMessages.ValueRequiredNoField);
    m.SetMissingRequestBodyRequiredValueAccessor(() => UserMessages.ValueRequiredNoField);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddInnovestRateLimiting();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

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
        // A password change ends every other session: the cookie's password fingerprint must match (L3).
        options.Events.OnValidatePrincipal = AdminSession.ValidateAsync;
    });

// Antiforgery tokens and the auth cookie are encrypted with these keys. Persisted outside Testing so
// that an app-pool recycle on the host does not invalidate every open form and admin session.
// On Windows the key files are encrypted at rest with DPAPI, machine scope (Stage 4 M1): a copied key
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
    if (LocalDbGuard.AppliesTo(app.Environment))
    {
        // The EFFECTIVE connection the migration will use (catches a DI override too). Does not connect.
        var effective = db.Database.GetConnectionString();
        LocalDbGuard.EnsureLocalDb(effective, app.Environment.EnvironmentName, "DbContext");
        app.Logger.LogInformation("Database: {Database}", LocalDbGuard.Describe(effective));
    }
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
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
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ──
// Admin controllers only (L5): /Admin/Home/* and /Admin/Account/* do not resolve.
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "" },
    constraints: new { controller = new AdminControllerConstraint() });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Makes the compiler-generated Program class public for WebApplicationFactory<Program> in the tests.
public partial class Program { }
