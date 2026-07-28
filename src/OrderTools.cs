using System.ComponentModel;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Server;

namespace McpOrdersServer;

[McpServerToolType]
public sealed class MovieTools
{
    private readonly DatabaseService _db;

    public MovieTools(DatabaseService db) => _db = db;

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(SqliteDataReader reader)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>
    /// Look up a single movie by title using case-insensitive partial matching.
    /// Exact matches are ranked first.
    /// </summary>
    private async Task<Dictionary<string, object?>?> FindMovieAsync(string title)
    {
        using var conn = _db.OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT *
            FROM   movies
            WHERE  LOWER(title) LIKE LOWER('%' || $title || '%')
            ORDER  BY CASE WHEN LOWER(title) = LOWER($title) THEN 0 ELSE 1 END, title
            LIMIT  1
            """;
        cmd.Parameters.AddWithValue("$title", title);
        using var reader = await cmd.ExecuteReaderAsync();
        var rows = await ReadRowsAsync(reader);
        return rows.Count > 0 ? rows[0] : null;
    }

    // ─── Tools ────────────────────────────────────────────────────────────────

    [McpServerTool]
    [Description("Search the movie catalog using optional filters. All parameters are optional.")]
    public async Task<string> SearchMovies(
        [Description("Partial title to search for (case-insensitive).")] string? title = null,
        [Description("Genre to filter by (e.g. Animation, Family, Adventure).")] string? genre = null,
        [Description("Minimum release year (inclusive).")] int? minYear = null,
        [Description("Maximum release year (inclusive).")] int? maxYear = null,
        [Description("Minimum IMDb rating from 0 to 10.")] double? minRating = null,
        [Description("Maximum number of results to return (default 10).")] int limit = 10)
    {
        using var conn = _db.OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT movie_id, title, genre, release_year, imdb_rating,
                   production_budget_usd, gross_sales_usd, net_sales_estimate_usd,
                   currency, poster_url, wikipedia_url, imdb_url
            FROM   movies
            WHERE  ($title     IS NULL OR LOWER(title) LIKE LOWER('%' || $title || '%'))
              AND  ($genre     IS NULL OR LOWER(genre) LIKE LOWER('%' || $genre || '%'))
              AND  ($minYear   IS NULL OR release_year >= $minYear)
              AND  ($maxYear   IS NULL OR release_year <= $maxYear)
              AND  ($minRating IS NULL OR imdb_rating  >= $minRating)
            ORDER  BY imdb_rating DESC
            LIMIT  $limit
            """;
        cmd.Parameters.AddWithValue("$title",     (object?)title     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$genre",     (object?)genre     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$minYear",   (object?)minYear   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$maxYear",   (object?)maxYear   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$minRating", (object?)minRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$limit",     limit);

        using var reader = await cmd.ExecuteReaderAsync();
        var movies = await ReadRowsAsync(reader);
        return JsonSerializer.Serialize(new { count = movies.Count, movies }, _json);
    }

    [McpServerTool]
    [Description(
        "Return detailed information for a single movie. " +
        "Uses case-insensitive partial title matching; exact matches are preferred.")]
    public async Task<string> GetMovieByTitle(
        [Description("Movie title or partial title to look up.")] string title)
    {
        var movie = await FindMovieAsync(title);
        if (movie is null)
            return JsonSerializer.Serialize(
                new { found = false, message = $"No movie matched '{title}'." }, _json);

        return JsonSerializer.Serialize(new { found = true, movie }, _json);
    }

    [McpServerTool]
    [Description("Return the highest-rated movies by IMDb rating, optionally filtered by genre.")]
    public async Task<string> GetTopRatedMovies(
        [Description("Number of movies to return (default 5).")] int limit = 5,
        [Description("Optional genre filter (e.g. Animation, Family).")] string? genre = null)
    {
        using var conn = _db.OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT title, genre, release_year, imdb_rating,
                   gross_sales_usd, net_sales_estimate_usd, imdb_url
            FROM   movies
            WHERE  ($genre IS NULL OR LOWER(genre) LIKE LOWER('%' || $genre || '%'))
            ORDER  BY imdb_rating DESC
            LIMIT  $limit
            """;
        cmd.Parameters.AddWithValue("$genre", (object?)genre ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        var movies = await ReadRowsAsync(reader);
        return JsonSerializer.Serialize(new { count = movies.Count, movies }, _json);
    }

    [McpServerTool]
    [Description("Return the highest-grossing movies by worldwide box-office revenue, optionally filtered by genre.")]
    public async Task<string> GetHighestGrossingMovies(
        [Description("Number of movies to return (default 5).")] int limit = 5,
        [Description("Optional genre filter (e.g. Animation, Family).")] string? genre = null)
    {
        using var conn = _db.OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT title, genre, release_year, imdb_rating,
                   production_budget_usd, gross_sales_usd, net_sales_estimate_usd,
                   currency, imdb_url
            FROM   movies
            WHERE  ($genre IS NULL OR LOWER(genre) LIKE LOWER('%' || $genre || '%'))
            ORDER  BY gross_sales_usd DESC
            LIMIT  $limit
            """;
        cmd.Parameters.AddWithValue("$genre", (object?)genre ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        var movies = await ReadRowsAsync(reader);
        return JsonSerializer.Serialize(new { count = movies.Count, movies }, _json);
    }

    [McpServerTool]
    [Description(
        "Return movies ordered by estimated net sales (gross_sales_usd minus production_budget_usd). " +
        "Note: this is a simplified estimate and does not account for marketing costs, " +
        "distribution fees, or other expenses.")]
    public async Task<string> GetMoviesByEstimatedNetSales(
        [Description("Number of movies to return (default 5).")] int limit = 5)
    {
        using var conn = _db.OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT title, release_year, genre, imdb_rating,
                   production_budget_usd, gross_sales_usd, net_sales_estimate_usd, imdb_url
            FROM   movies
            ORDER  BY net_sales_estimate_usd DESC
            LIMIT  $limit
            """;
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        var movies = await ReadRowsAsync(reader);
        return JsonSerializer.Serialize(new { count = movies.Count, movies }, _json);
    }

    [McpServerTool]
    [Description(
        "Compare two movies side-by-side across IMDb rating, release year, " +
        "production budget, gross sales, and estimated net sales.")]
    public async Task<string> CompareMovies(
        [Description("Title of the first movie (case-insensitive, partial match allowed).")] string firstTitle,
        [Description("Title of the second movie (case-insensitive, partial match allowed).")] string secondTitle)
    {
        var first  = await FindMovieAsync(firstTitle);
        var second = await FindMovieAsync(secondTitle);

        if (first is null || second is null)
        {
            var missing = first is null ? firstTitle : secondTitle;
            return JsonSerializer.Serialize(
                new { found = false, message = $"No movie matched '{missing}'." }, _json);
        }

        static long   ToLong(object? v) => v is null ? 0L : Convert.ToInt64(v);
        static double ToDbl(object? v)  => v is null ? 0d : Convert.ToDouble(v);
        static int    ToInt(object? v)  => v is null ? 0  : Convert.ToInt32(v);

        var comparison = new
        {
            first_title  = first["title"],
            second_title = second["title"],
            imdb_rating = new
            {
                first      = ToDbl(first["imdb_rating"]),
                second     = ToDbl(second["imdb_rating"]),
                difference = Math.Round(ToDbl(first["imdb_rating"]) - ToDbl(second["imdb_rating"]), 2),
            },
            release_year = new
            {
                first      = ToInt(first["release_year"]),
                second     = ToInt(second["release_year"]),
                difference = ToInt(first["release_year"]) - ToInt(second["release_year"]),
            },
            production_budget_usd = new
            {
                first      = ToLong(first["production_budget_usd"]),
                second     = ToLong(second["production_budget_usd"]),
                difference = ToLong(first["production_budget_usd"]) - ToLong(second["production_budget_usd"]),
            },
            gross_sales_usd = new
            {
                first      = ToLong(first["gross_sales_usd"]),
                second     = ToLong(second["gross_sales_usd"]),
                difference = ToLong(first["gross_sales_usd"]) - ToLong(second["gross_sales_usd"]),
            },
            net_sales_estimate_usd = new
            {
                first      = ToLong(first["net_sales_estimate_usd"]),
                second     = ToLong(second["net_sales_estimate_usd"]),
                difference = ToLong(first["net_sales_estimate_usd"]) - ToLong(second["net_sales_estimate_usd"]),
            },
        };

        return JsonSerializer.Serialize(new { found = true, comparison }, _json);
    }

    [McpServerTool]
    [Description(
        "Return a summary of the entire movie dataset including totals, averages, " +
        "and the highest-rated and highest-grossing titles.")]
    public async Task<string> GetMovieStatistics()
    {
        using var conn = _db.OpenConnection();

        Dictionary<string, object?> agg;
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*)                         AS total_movies,
                       ROUND(AVG(imdb_rating), 2)       AS avg_rating,
                       MIN(release_year)                AS earliest_year,
                       MAX(release_year)                AS latest_year,
                       SUM(gross_sales_usd)             AS total_gross_sales_usd,
                       ROUND(AVG(gross_sales_usd))      AS avg_gross_sales_usd
                FROM   movies
                """;
            using var reader = await cmd.ExecuteReaderAsync();
            agg = (await ReadRowsAsync(reader))[0];
        }

        Dictionary<string, object?>? topRated;
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT title, imdb_rating FROM movies ORDER BY imdb_rating DESC LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            topRated = (await ReadRowsAsync(reader)).FirstOrDefault();
        }

        Dictionary<string, object?>? topGross;
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT title, gross_sales_usd FROM movies ORDER BY gross_sales_usd DESC LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            topGross = (await ReadRowsAsync(reader)).FirstOrDefault();
        }

        var result = new
        {
            total_movies           = agg["total_movies"],
            avg_rating             = agg["avg_rating"],
            earliest_year          = agg["earliest_year"],
            latest_year            = agg["latest_year"],
            total_gross_sales_usd  = agg["total_gross_sales_usd"],
            avg_gross_sales_usd    = agg["avg_gross_sales_usd"],
            highest_rated_movie    = topRated?["title"],
            highest_grossing_movie = topGross?["title"],
        };

        return JsonSerializer.Serialize(result, _json);
    }

    // ─── Shared options ───────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };
}
