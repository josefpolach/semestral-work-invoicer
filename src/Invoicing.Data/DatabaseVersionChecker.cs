using Dapper;

namespace Invoicing.Data;

/// <summary>Checks the schema installed by Flyway.</summary>
public class DatabaseVersionChecker(string databaseFilePath)
{
    public const string RequiredSchemaVersion = "5";
    private const string History = """
        SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'flyway_schema_history'
        """;
    private const string Version = "SELECT COUNT(*) FROM flyway_schema_history WHERE version = @Version AND success = 1";

    public async Task<DatabaseStatus> CheckAsync()
    {
        if (!File.Exists(databaseFilePath))
        {
            return DatabaseStatus.FileMissing;
        }

        await using var connection = await new SqliteConnectionFactory(databaseFilePath).OpenConnectionAsync();
        if (await connection.ExecuteScalarAsync<int>(History) == 0)
        {
            return DatabaseStatus.NotMigrated;
        }

        return await connection.ExecuteScalarAsync<int>(Version, new { Version = RequiredSchemaVersion }) > 0
            ? DatabaseStatus.Ready : DatabaseStatus.OutdatedSchema;
    }
}
