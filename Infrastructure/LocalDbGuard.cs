using Microsoft.Data.SqlClient;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Refuses to let a Development or Testing process use any database that is not a LocalDB instance.
/// The connection string and its host are never echoed in the exception or the log.
/// </summary>
public static class LocalDbGuard
{
    private const string LocalDbPrefix = @"(localdb)\";

    /// <summary>Environments in which only a LocalDB connection is accepted.</summary>
    public static bool AppliesTo(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    public static bool IsLocalDb(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        try
        {
            var dataSource = new SqlConnectionStringBuilder(connectionString).DataSource?.Trim() ?? "";
            return dataSource.StartsWith(LocalDbPrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false; // unparseable — treat as not LocalDB
        }
    }

    public static void EnsureLocalDb(string? connectionString, string environmentName, string source)
    {
        if (!IsLocalDb(connectionString))
            throw new InvalidOperationException(
                $"Environment '{environmentName}' requires a LocalDB connection (source: {source}).");
    }

    /// <summary>"(localdb)\Instance / Database" — the only parts of the string that are ever logged.</summary>
    public static string Describe(string? connectionString)
    {
        var b = new SqlConnectionStringBuilder(connectionString);
        return $"{b.DataSource} / {b.InitialCatalog}";
    }
}
