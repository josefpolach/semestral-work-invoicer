using Invoicing.Data;

namespace Invoicing.Tests.Data;

public class DatabaseVersionCheckerTests
{
    [Fact]
    public async Task CheckAsync_ComparesSchemaVersions()
    {
        using var database = new TemporaryDatabase();
        var checker = new DatabaseVersionChecker(database.Path);
        Assert.Equal(DatabaseStatus.FileMissing, await checker.CheckAsync());
        await database.CreateAsync(false);
        Assert.Equal(DatabaseStatus.NotMigrated, await checker.CheckAsync());
        await database.ExecuteAsync("""
            CREATE TABLE flyway_schema_history (version TEXT, success INTEGER);
            INSERT INTO flyway_schema_history VALUES ('4', 1), ('5', 0);
            """);
        Assert.Equal(DatabaseStatus.OutdatedSchema, await checker.CheckAsync());
        await database.ExecuteAsync("INSERT INTO flyway_schema_history VALUES ('5', 1);");
        Assert.Equal(DatabaseStatus.Ready, await checker.CheckAsync());
    }
}
