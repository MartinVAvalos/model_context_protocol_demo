using Microsoft.Data.Sqlite;

namespace McpOrdersServer;

/// <summary>Opens SQLite connections to the family movies database.</summary>
public sealed class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "db/family_movies.sqlite";
    }

    public SqliteConnection OpenConnection()
    {
        if (!File.Exists(_dbPath))
            throw new FileNotFoundException($"Database not found at '{_dbPath}'");

        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }
}
