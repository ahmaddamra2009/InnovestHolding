using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Refuses to start the app — in EVERY environment, Production included — when the database connection is not LocalDB
/// and the process is not the production host. A developer machine that runs the site (or <c>dotnet ef</c>) with the
/// production connection string therefore stops before any connection, any migration and any seeding.
///
/// A non-LocalDB connection is accepted only when:
/// <list type="bullet">
/// <item>the process is hosted by IIS: any of <see cref="IisMarkers"/> is set (IIS sets <c>APP_POOL_ID</c> for every w3wp
/// worker; the ASP.NET Core Module sets <c>ASPNETCORE_IIS_PHYSICAL_PATH</c>, and <c>ASPNETCORE_TOKEN</c> when out-of-process);
/// none of them is set by <c>dotnet run</c> or <c>dotnet app.dll</c>; or</item>
/// <item>the operator set <see cref="AllowVariable"/> to exactly "1" (a production host that is not IIS).</item>
/// </list>
/// Pure: reads the connection string and the environment variables it is given, opens nothing, and never puts the
/// connection string (or its host) in the exception or the log.
/// </summary>
public static class ProductionDbGuard
{
    /// <summary>Set by the ASP.NET Core Module (IIS) to the site's physical path.</summary>
    public const string IisMarker = "ASPNETCORE_IIS_PHYSICAL_PATH";

    /// <summary>
    /// Environment variables that only an IIS-hosted process has, checked in this order: the app pool identity IIS gives
    /// every worker process (in-process and out-of-process alike), the ANCM physical path, the ANCM out-of-process token.
    /// </summary>
    public static readonly IReadOnlyList<string> IisMarkers = ["APP_POOL_ID", IisMarker, "ASPNETCORE_TOKEN"];

    /// <summary>Explicit opt-in for a production host that is not IIS. Only the exact value "1" counts.</summary>
    public const string AllowVariable = "INNOVEST_ALLOW_REMOTE_DB";

    public enum Decision { LocalDb, IisHost, ExplicitlyAllowed, Refused }

    /// <summary>The decision and the variable that allowed it (null for LocalDB and for a refusal). Never the connection string.</summary>
    public sealed record Verdict(Decision Decision, string? Marker)
    {
        public override string ToString() => Marker is null ? Decision.ToString() : $"{Decision} ({Marker})";
    }

    public static Verdict Evaluate(string? connectionString, Func<string, string?> environmentVariable)
    {
        if (LocalDbGuard.IsLocalDb(connectionString)) return new(Decision.LocalDb, null);
        foreach (var marker in IisMarkers)
            if (!string.IsNullOrWhiteSpace(environmentVariable(marker))) return new(Decision.IisHost, marker);
        if (environmentVariable(AllowVariable) == "1") return new(Decision.ExplicitlyAllowed, AllowVariable);
        return new(Decision.Refused, null);
    }

    /// <summary>Throws <see cref="ProductionDbGuardException"/> when <see cref="Evaluate"/> refuses; returns the verdict otherwise.</summary>
    public static Verdict Ensure(string? connectionString, Func<string, string?> environmentVariable, string source)
    {
        var decision = Evaluate(connectionString, environmentVariable);
        if (decision.Decision == Decision.Refused)
            throw new ProductionDbGuardException(
                $"Refusing the database connection ({source}): it is not LocalDB and this process is not the production " +
                $"host. Use a LocalDB connection on a development machine. On a production host that does not run under IIS, " +
                $"set {AllowVariable}=1.");
        return decision;
    }
}

/// <summary>The guard's refusal (its own type, so tests can tell it apart from a failed connection).</summary>
public sealed class ProductionDbGuardException(string message) : InvalidOperationException(message);

/// <summary>
/// Applies <see cref="ProductionDbGuard"/> to the connection EF Core is about to open, for every context built through
/// <see cref="Data.DbContextRegistration.AddAppDbContext"/> — whatever set its string (configuration,
/// <c>dotnet ef --connection</c>, <c>SetConnectionString</c>). A hand-built <c>DbContextOptionsBuilder</c> is not covered.
/// Runs before <c>SqlConnection.Open</c>, so a refusal happens before any name resolution or TCP connect.
/// Registered with the DbContext in <see cref="Data.DbContextRegistration.AddAppDbContext"/>.
/// </summary>
public sealed class ProductionDbGuardInterceptor : DbConnectionInterceptor
{
    private readonly Func<string, string?> _environmentVariable;

    public ProductionDbGuardInterceptor() : this(Environment.GetEnvironmentVariable) { }

    public ProductionDbGuardInterceptor(Func<string, string?> environmentVariable) => _environmentVariable = environmentVariable;

    public override InterceptionResult ConnectionOpening(
        DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result)
    {
        ProductionDbGuard.Ensure(connection.ConnectionString, _environmentVariable, "connection");
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        ProductionDbGuard.Ensure(connection.ConnectionString, _environmentVariable, "connection");
        return ValueTask.FromResult(result);
    }
}
