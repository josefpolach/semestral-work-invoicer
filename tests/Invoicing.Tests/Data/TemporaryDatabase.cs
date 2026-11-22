using Microsoft.Data.Sqlite;

namespace Invoicing.Tests.Data;

internal class TemporaryDatabase : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".db");

    public async Task CreateAsync(bool migrate = true)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path }.ToString());
        await connection.OpenAsync();
        if (!migrate)
        {
            return;
        }

        var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "Migrations");
        foreach (var script in Directory.GetFiles(directory, "V*.sql").Order(StringComparer.Ordinal))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = await File.ReadAllTextAsync(script);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(Path);
    }
}
