using System.Diagnostics;
using InnovestHolding.Data;
using InnovestHolding.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InnovestHolding.Tests;

/// <summary>
/// The guard at connection-open time: whatever replaced the connection string (as <c>dotnet ef --connection</c> does with
/// <c>SetConnectionString</c>), the open is refused before SqlClient runs — so before any name resolution or TCP connect.
/// The only remote host is <c>example.invalid</c>, which never resolves.
/// </summary>
public class ProductionDbGuardConnectionTests
{
    private const string LocalDb = @"Server=(localdb)\InnovestLocal;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    private const string Remote = "Server=tcp:example.invalid,1433;Database=SecretDb;User Id=someone;Password=hunter2;Connect Timeout=10";

    /// <summary>The app's own registration (the one the app, the tests and the EF tools use).</summary>
    private static AppDbContext NewContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppDbContext(LocalDb);
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static void AssertNoMarkers()
    {
        foreach (var marker in ProductionDbGuard.IisMarkers.Append(ProductionDbGuard.AllowVariable))
            Assert.Null(Environment.GetEnvironmentVariable(marker));
    }

    [Fact] // `dotnet ef … --connection <remote>`: the string is replaced on the DbContext after start-up, then opened
    public void A_connection_string_replaced_on_the_context_is_refused_at_open()
    {
        AssertNoMarkers();
        using var db = NewContext();
        db.Database.SetConnectionString(Remote);
        var clock = Stopwatch.StartNew();
        var e = Record.Exception(() => db.Database.OpenConnection());
        clock.Stop();
        // A real attempt surfaces as SqlException (Win32Exception 11001 "No such host is known"), never as the guard's type.
        var guard = Assert.IsType<ProductionDbGuardException>(e);
        Assert.Contains("(connection)", guard.Message);
        Assert.DoesNotContain("example.invalid", guard.Message);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2), $"refusal took {clock.Elapsed}");
    }

    [Fact] // the async open path (MigrateAsync, queries) is refused the same way
    public async Task The_async_open_is_refused_too()
    {
        AssertNoMarkers();
        await using var db = NewContext();
        db.Database.SetConnectionString(Remote);
        var e = await Record.ExceptionAsync(() => db.Database.OpenConnectionAsync());
        Assert.IsType<ProductionDbGuardException>(e);
    }

    [Fact] // a LocalDB string with a failover partner would let SqlClient connect to that remote partner
    public void A_failover_partner_on_a_localdb_string_is_refused_at_open()
    {
        AssertNoMarkers();
        using var db = NewContext();
        db.Database.SetConnectionString(@"Server=(localdb)\NoSuchInstance;Database=x;Trusted_Connection=True;Failover Partner=tcp:example.invalid");
        Assert.IsType<ProductionDbGuardException>(Record.Exception(() => db.Database.OpenConnection()));
    }

    [Fact] // positive control: the LocalDB test database still opens through the same registration
    public void The_localdb_test_database_opens()
    {
        using var db = NewContext();
        db.Database.OpenConnection();
        db.Database.CloseConnection();
    }
}
