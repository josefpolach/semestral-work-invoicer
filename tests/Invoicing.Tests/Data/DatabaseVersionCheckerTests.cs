using Invoicing.Data;

namespace Invoicing.Tests.Data;

public class DatabaseVersionCheckerTests
{
    [Fact]
    public async Task CheckAsync_MissingFile_ReturnsFileMissing()
    {
        using var database = new TemporaryDatabase();
        Assert.Equal(DatabaseStatus.FileMissing, await new DatabaseVersionChecker(database.Path).CheckAsync());
    }

    [Fact]
    public async Task CheckAsync_MissingFlywayHistory_ReturnsNotMigrated()
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync(false);
        Assert.Equal(DatabaseStatus.NotMigrated, await new DatabaseVersionChecker(database.Path).CheckAsync());
    }

    [Theory]
    [InlineData("INSERT INTO flyway_schema_history VALUES ('4', 1);", DatabaseStatus.OutdatedSchema)]
    [InlineData("INSERT INTO flyway_schema_history VALUES ('5', 0);", DatabaseStatus.OutdatedSchema)]
    [InlineData("INSERT INTO flyway_schema_history VALUES ('5', 1);", DatabaseStatus.Ready)]
    public async Task CheckAsync_FlywayHistory_RequiresSuccessfulVersionFive(string insert, DatabaseStatus expected)
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync(false);
        await database.ExecuteAsync("CREATE TABLE flyway_schema_history (version TEXT, success INTEGER);");
        await database.ExecuteAsync(insert);
        Assert.Equal(expected, await new DatabaseVersionChecker(database.Path).CheckAsync());
    }
}
