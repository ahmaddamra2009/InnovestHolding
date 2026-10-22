namespace InnovestHolding.Tests;

/// <summary>The test-database suffix (two checkouts can run the suite side by side): a plain tag only.</summary>
public class TestDbTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("_C2", "_C2")]
    [InlineData("_wt7", "_wt7")]
    public void Suffix_is_accepted(string? raw, string expected) => Assert.Equal(expected, TestDb.ReadSuffix(raw));

    [Theory]
    [InlineData("C2")]
    [InlineData("_C2;Server=example.invalid")]
    [InlineData("_a b")]
    [InlineData("_123456789012345678901")]
    public void Anything_else_is_refused(string raw) => Assert.Throws<InvalidOperationException>(() => TestDb.ReadSuffix(raw));

    [Fact]
    public void The_suffix_reaches_every_test_database_and_the_server_stays_localdb()
    {
        Assert.Equal("InnovestHolding_Tests" + TestDb.Suffix, TestAppFactory.TestDatabase);
        Assert.Equal("InnovestHolding_MigrationTests" + TestDb.Suffix, MigrationTests.Database);
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(TestAppFactory.TestConnectionString);
        Assert.Equal(@"(localdb)\InnovestLocal", b.DataSource);
        Assert.Equal(TestAppFactory.TestDatabase, b.InitialCatalog);
    }
}
