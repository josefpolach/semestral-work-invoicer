using Dapper;
using Microsoft.Data.Sqlite;

namespace Invoicing.Data;

/// <summary>Opens an existing SQLite database and enables foreign keys.</summary>
public class SqliteConnectionFactory(string databaseFilePath)
{
    static SqliteConnectionFactory() => DefaultTypeMap.MatchNamesWithUnderscores = true;

    public async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databaseFilePath,
            Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true
        }.ToString());
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (SqliteException)
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
