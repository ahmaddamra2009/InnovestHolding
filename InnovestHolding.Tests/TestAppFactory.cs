using System.Net;
using InnovestHolding.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace InnovestHolding.Tests;

/// <summary>
/// One in-process app for the whole run, on its OWN LocalDB database. The database name and server
/// are asserted BEFORE the host is built (independently of the app's LocalDbGuard), and the database
/// the app actually uses is asserted AFTER startup, by effect (SELECT DB_NAME()).
/// </summary>
public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    /// <summary>InnovestHolding_Tests + the optional INNOVEST_TEST_DB_SUFFIX (see <see cref="TestDb"/>).</summary>
    public static readonly string TestDatabase = TestDb.Name("InnovestHolding_Tests");
    public static readonly string TestConnectionString = TestDb.ConnectionString(TestDatabase);

    /// <summary>The uploads folder of the run (its own temp folder per database name, emptied at start).</summary>
    public static readonly string UploadsRoot = Path.Combine(Path.GetTempPath(), TestDatabase + "_uploads");

    /// <summary>Shared clock for the login throttle (and the rest of the app). Starts at real "now".</summary>
    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    public string EffectiveDatabase { get; }

    /// <summary>Id of an active interest option (written by the migration) — the registration form requires one.</summary>
    public string InterestId { get; }
    public string EffectiveDataSource { get; }

    public TestAppFactory()
    {
        AssertIsTestDatabase(TestConnectionString);

        // Every run starts from an empty test database; the app's own startup migrates + seeds it.
        using (var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseSqlServer(TestConnectionString).Options))
        {
            ctx.Database.EnsureDeleted();
        }

        if (Directory.Exists(UploadsRoot)) Directory.Delete(UploadsRoot, recursive: true);

        ClientOptions.BaseAddress = new Uri("https://innovest.test");
        ClientOptions.AllowAutoRedirect = false;
        ClientOptions.HandleCookies = true;

        _ = Server; // build + start the host now (migrate + seed happen here)

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        EffectiveDatabase = db.Database.SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]").AsEnumerable().Single();
        EffectiveDataSource = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (EffectiveDatabase != TestDatabase || !EffectiveDataSource.StartsWith(@"(localdb)\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The app under test is not using the LocalDB test database.");

        InterestId = db.InterestOptions.Where(o => o.IsActive).OrderBy(o => o.SortOrder).Select(o => o.Id).First()
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AssertIsTestDatabase(string connectionString)
    {
        var b = new SqlConnectionStringBuilder(connectionString);
        if (!b.DataSource.StartsWith(@"(localdb)\", StringComparison.OrdinalIgnoreCase) || b.InitialCatalog != TestDatabase)
            throw new InvalidOperationException("Tests may only run against the LocalDB test database.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Highest-precedence configuration for the entry point, so Program's pre-Build guard sees it.
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("https_port", "443");
        builder.UseSetting("Uploads:RootPath", UploadsRoot);

        builder.ConfigureTestServices(services =>
        {
            // Whatever the app registered (or an env var a developer set), the DbContext uses the test DB.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            // The app's own registration helper, so the home-cache invalidation interceptor is kept.
            services.AddAppDbContext(TestConnectionString);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);

            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }
}

/// <summary>Test-only: lets each test own its rate-limit / lockout partition via X-Test-Client-Ip.</summary>
internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public const string Header = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var value) && IPAddress.TryParse(value, out var ip))
                context.Connection.RemoteIpAddress = ip;
            await nextMiddleware();
        });
        next(app);
    };
}

[CollectionDefinition("App")]
public sealed class AppCollection : ICollectionFixture<TestAppFactory> { }
