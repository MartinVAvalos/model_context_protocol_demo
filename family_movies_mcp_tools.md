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

