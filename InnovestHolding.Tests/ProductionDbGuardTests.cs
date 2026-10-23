using System.Diagnostics;
using InnovestHolding.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InnovestHolding.Tests;

/// <summary>
/// The production-database guard. The remote host used here is <c>example.invalid</c> (RFC 2606: never resolves); the
/// real production server is never named or contacted.
/// </summary>
public class ProductionDbGuardTests
{
    private const string Remote = "Server=example.invalid;Database=SecretDb;User Id=someone;Password=hunter2;Connect Timeout=5";
    private const string LocalDb = @"Server=(localdb)\InnovestLocal;Database=InnovestHolding_Local;Trusted_Connection=True";

    private static Func<string, string?> Env(params (string Name, string Value)[] vars) =>
        name => vars.FirstOrDefault(v => v.Name == name).Value;

    private const ProductionDbGuard.Decision Local = ProductionDbGuard.Decision.LocalDb, Iis = ProductionDbGuard.Decision.IisHost,
        Allowed = ProductionDbGuard.Decision.ExplicitlyAllowed, Refused = ProductionDbGuard.Decision.Refused;

    public static TheoryData<string?, (string, string)[], ProductionDbGuard.Decision, string?> Cases() => new()
    {
        { LocalDb, [], Local, null },
        { LocalDb, [(ProductionDbGuard.AllowVariable, "1")], Local, null },
        { Remote, [], Refused, null },                                                                  // a dev machine: no marker at all
        // Each IIS marker on its own allows the start, and the verdict names it.
        { Remote, [("APP_POOL_ID", "innovest-holding.com")], Iis, "APP_POOL_ID" },
        { Remote, [("ASPNETCORE_IIS_PHYSICAL_PATH", @"C:\inetpub\site\")], Iis, "ASPNETCORE_IIS_PHYSICAL_PATH" },
        { Remote, [("ASPNETCORE_TOKEN", "f1a2b3c4-pairing-token")], Iis, "ASPNETCORE_TOKEN" },
        { Remote, [("ASPNETCORE_TOKEN", "t"), ("APP_POOL_ID", "pool")], Iis, "APP_POOL_ID" },             // first marker found is named
        { Remote, [("APP_POOL_ID", "  "), ("ASPNETCORE_IIS_PHYSICAL_PATH", ""), ("ASPNETCORE_TOKEN", " ")], Refused, null }, // blank markers
        { Remote, [("ASPNETCORE_PORT", "5000"), ("ASPNETCORE_APPL_PATH", "/")], Refused, null },          // other ANCM-like names do not count
        { Remote, [(ProductionDbGuard.AllowVariable, "1")], Allowed, ProductionDbGuard.AllowVariable },
        { Remote, [(ProductionDbGuard.AllowVariable, "true")], Refused, null },                           // only the exact "1"
        { Remote, [(ProductionDbGuard.AllowVariable, " 1")], Refused, null },
        { Remote, [(ProductionDbGuard.AllowVariable, "0")], Refused, null },
        { "Server=(local);Database=x;Trusted_Connection=True", [], Refused, null },                       // a full SQL Server is not LocalDB
        // A LocalDB data source with a second server SqlClient may use is not "LocalDB only".
        { LocalDb + ";Failover Partner=tcp:example.invalid", [], Refused, null },
        { LocalDb + ";MultiSubnetFailover=True", [], Refused, null },
        { LocalDb + ";Server=tcp:example.invalid", [], Refused, null },                                    // the last server key wins
        { LocalDb + ";Failover Partner=tcp:example.invalid", [("APP_POOL_ID", "pool")], Iis, "APP_POOL_ID" }, // still a host decision
        { "not a connection string", [], Refused, null },
        { null, [], Refused, null },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Decides_from_the_connection_and_the_host_markers_only(string? cs, (string, string)[] vars,
        ProductionDbGuard.Decision expected, string? marker)
    {
        var verdict = ProductionDbGuard.Evaluate(cs, Env(vars));
        Assert.Equal(expected, verdict.Decision);
        Assert.Equal(marker, verdict.Marker);
    }

    [Fact] // the start-up log line names the marker, and only the marker
    public void The_verdict_text_names_the_marker_and_never_the_connection()
    {
        var verdict = ProductionDbGuard.Ensure(Remote, Env(("APP_POOL_ID", "innovest-holding.com")), "configuration");
        Assert.Equal("IisHost (APP_POOL_ID)", verdict.ToString());
        Assert.Equal("LocalDb", ProductionDbGuard.Ensure(LocalDb, Env(), "configuration").ToString());
    }

    [Fact] // the markers are exactly the three IIS-only variables
    public void The_iis_markers_are_the_three_iis_only_variables() =>
        Assert.Equal(["APP_POOL_ID", "ASPNETCORE_IIS_PHYSICAL_PATH", "ASPNETCORE_TOKEN"], ProductionDbGuard.IisMarkers);

    [Fact]
    public void The_refusal_never_echoes_the_connection_string()
    {
        var e = Assert.Throws<ProductionDbGuardException>(() => ProductionDbGuard.Ensure(Remote, Env(), "configuration"));
        foreach (var secret in new[] { "example.invalid", "SecretDb", "someone", "hunter2" })
            Assert.DoesNotContain(secret, e.Message);
        Assert.Contains(ProductionDbGuard.AllowVariable, e.Message);
    }

    /// <summary>The app itself, in the Production environment, with a (fake) remote database and no host marker.</summary>
    private sealed class ProductionWithRemoteDb : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", Remote);
        }
    }

    [Fact] // the real startup path: refused at configuration time, before Build — so before any DNS lookup or TCP connect
    public void A_production_run_with_a_remote_database_outside_iis_refuses_before_connecting()
    {
        // The premise: this test process is not an IIS host and has no explicit opt-in.
        foreach (var marker in ProductionDbGuard.IisMarkers.Append(ProductionDbGuard.AllowVariable))
            Assert.Null(Environment.GetEnvironmentVariable(marker));

        using var factory = new ProductionWithRemoteDb();
        var clock = Stopwatch.StartNew();
        var thrown = Record.Exception(() => factory.CreateClient());
        clock.Stop();

        // A connection attempt would surface as a SqlException (name resolution of example.invalid fails), never as the
        // guard's own type; the guard throws from the configuration check that runs before the host is built.
        var guard = Assert.IsType<ProductionDbGuardException>(Unwrap(thrown));
        Assert.Contains("(configuration)", guard.Message);
        Assert.DoesNotContain("example.invalid", guard.Message);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"refusal took {clock.Elapsed}");
    }

    private static Exception? Unwrap(Exception? e)
    {
        while (e is not null and not ProductionDbGuardException && e.InnerException is not null) e = e.InnerException;
        return e;
    }
}
