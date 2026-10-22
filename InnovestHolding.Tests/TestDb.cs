using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace InnovestHolding.Tests;

/// <summary>
/// Names of the LocalDB databases the tests drop and recreate. Two checkouts (worktrees) can run `dotnet test` at
/// the same time when each sets its own <c>INNOVEST_TEST_DB_SUFFIX</c> (e.g. <c>_C2</c>): every test database name gets
/// that suffix. The suffix is restricted to <c>_</c> + letters/digits so it can never change the server or inject
/// connection-string syntax; the fixtures still refuse anything that is not LocalDB.
/// </summary>
internal static partial class TestDb
{
    public const string SuffixVariable = "INNOVEST_TEST_DB_SUFFIX";

    public static string Suffix { get; } = ReadSuffix(Environment.GetEnvironmentVariable(SuffixVariable));

    internal static string ReadSuffix(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        if (!SuffixPattern().IsMatch(raw))
            throw new InvalidOperationException($"{SuffixVariable} must match ^_[A-Za-z0-9]{{1,20}}$.");
        return raw;
    }

    public static string Name(string baseName) => baseName + Suffix;

    public static string ConnectionString(string database) =>
        new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\InnovestLocal",
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
        }.ConnectionString;

    [GeneratedRegex("^_[A-Za-z0-9]{1,20}$")]
    private static partial Regex SuffixPattern();
}
