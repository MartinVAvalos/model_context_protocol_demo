using Microsoft.Data.Sqlite;

namespace ChatServer;

public sealed record MovieListItem(
    int Id,
    string Title,
    string PosterUrl,
    string Description,
    string Genre,
    int ReleaseYear,
    double ImdbRating,
    string ImdbUrl);

public sealed class MovieCatalogService
{
    private readonly string _dbPath;

    public MovieCatalogService()
    {
        _dbPath = ResolveDbPath();
    }

    public async Task<IReadOnlyList<MovieListItem>> GetMoviesAsync()
    {
        if (!File.Exists(_dbPath))
            throw new FileNotFoundException($"Database not found at '{_dbPath}'");

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT movie_id, title, poster_url, description, genre, release_year, imdb_rating, imdb_url
            FROM movies
            ORDER BY title
            """;

        var movies = new List<MovieListItem>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            movies.Add(new MovieListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetDouble(6),
                reader.GetString(7)));
        }

        return movies;
    }

    private static string ResolveDbPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("DB_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(root));

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "db", "family_movies.sqlite");
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "db", "family_movies.sqlite"));
    }
}