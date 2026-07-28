# Family Movies MCP Server Tools

This document summarizes the recommended MCP tools for a portfolio-ready server backed by `family_movies.sqlite`.

> In MCP, these are called **tools**, not HTTP endpoints. Clients invoke them through MCP's `tools/call` method.

## Recommended Core Tools

### `search_movies`

Search the movie catalog using optional filters.

**Suggested parameters**

- `title`
- `genre`
- `minYear`
- `maxYear`
- `minRating`
- `limit`

**Example requests**

- Show me animated movies rated above 8.
- Find family movies released after 2010.
- Find movies with “Spider-Man” in the title.

**Demonstrates**

- Optional parameters
- Dynamic SQL filters
- Parameter validation
- Parameterized queries
- Structured results

---

### `get_movie_by_title`

Return detailed information for one movie.

**Suggested parameter**

- `title`

**Possible response fields**

- Title
- Description
- Genre
- Release year
- IMDb rating
- Production budget
- Gross sales
- Estimated net sales
- Poster URL
- Wikipedia URL
- IMDb URL

**Example requests**

- Tell me about *Coco*.
- Show me the poster and financial data for *Frozen*.

Use case-insensitive title matching and optionally allow partial matches.

---

### `get_top_rated_movies`

Return the highest-rated movies, optionally filtered by genre.

**Suggested parameters**

- `limit`
- `genre`

**Example requests**

- What are the five highest-rated family movies?
- What is the highest-rated animated movie?

Sort by IMDb rating in descending order.

---

### `get_highest_grossing_movies`

Return movies with the highest worldwide box-office gross.

**Suggested parameters**

- `limit`
- `genre`

**Example requests**

- Which movies made the most worldwide?
- Show me the three highest-grossing animated movies.

This tool should sort by `gross_sales_usd`.

---

### `get_movies_by_estimated_net_sales`

Return movies ordered by estimated net sales.

**Suggested parameter**

- `limit`

**Example requests**

- Which movies have the highest estimated net sales?
- Show me the five strongest financial performers.

The database calculates estimated net sales as:

```text
gross_sales_usd - production_budget_usd
```

This value is only a simplified demonstration estimate. It is not audited studio profit and does not include theater revenue shares, marketing costs, distribution fees, taxes, residuals, or non-theatrical revenue.

---

### `compare_movies`

Compare two movies across ratings, release year, and financial fields.

**Suggested parameters**

- `firstTitle`
- `secondTitle`

**Example requests**

- Compare *Frozen* and *Coco*.
- Which had a better rating: *Up* or *Finding Nemo*?
- Compare *Toy Story* and *The Lion King* financially.

**Possible comparison fields**

- IMDb rating difference
- Production budget difference
- Gross sales difference
- Estimated net sales difference
- Release-year difference

This tool is especially useful because it combines database retrieval with calculated comparisons.

---

### `get_movie_statistics`

Return a summary of the entire dataset.

**Suggested parameters**

None.

**Possible response fields**

- Number of movies
- Average IMDb rating
- Earliest release year
- Latest release year
- Total gross sales
- Average gross sales
- Highest-rated movie
- Highest-grossing movie

**Example requests**

- Give me a summary of the movie database.
- What is the average rating?
- How much did all ten movies gross combined?

## Optional Tools

### `get_movies_by_genre`

Return movies matching a genre.

**Suggested parameters**

- `genre`
- `limit`

This may be unnecessary if `search_movies` already supports genre filtering.

---

### `get_movies_by_release_period`

Return movies released within a year range.

**Suggested parameters**

- `startYear`
- `endYear`

**Example request**

- Show me family movies released between 2000 and 2010.

---

### `get_genre_summary`

Summarize the number of movies and average rating by genre.

This works best if genres are normalized into separate `genres` and `movie_genres` tables instead of being stored as comma-separated text.

## Recommended Minimal Tool Set

For a focused portfolio project, implement these six tools:

1. `search_movies`
2. `get_movie_by_title`
3. `get_top_rated_movies`
4. `get_highest_grossing_movies`
5. `get_movies_by_estimated_net_sales`
6. `compare_movies`

This set demonstrates:

- MCP tool registration
- Tool descriptions
- Argument validation
- Optional parameters
- SQLite querying
- Filtering and sorting
- Financial calculations
- Structured responses
- Error handling

## Validation Guidelines

Validate all incoming parameters before running a query.

Examples:

- `limit` should be between 1 and 50.
- Ratings should be between 0 and 10.
- `minYear` should not be greater than `maxYear`.
- Movie titles should not be empty.
- Genre values should be trimmed and checked before use.

Always use parameterized SQL instead of inserting user input directly into SQL strings.

## Avoid Generic SQL Tools

Do not expose tools such as:

```text
execute_sql
run_query
```

Generic SQL tools make it possible to run unsafe or destructive commands. Focused, read-only tools are safer and better demonstrate thoughtful MCP design.

## Optional MCP Resource

Consider exposing a resource such as:

```text
movies://catalog-info
```

It could explain:

- Available database fields
- Rating scale
- Data sources
- Poster URL behavior
- Estimated net sales calculation
- Financial-data limitations

Adding a resource helps demonstrate that the server supports more than tool calls.

## Suggested Architecture

```text
MCP Client
    |
    | JSON-RPC over stdio
    v
.NET MCP Server
    |
    | Focused read-only tools
    v
family_movies.sqlite
```
