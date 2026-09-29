# Listing Search Service

Full-stack search over property listings: **ASP.NET Core 8 minimal API** (C#) + **React / TypeScript** (Vite) UI.
Data is held in memory, loaded from a JSON file at startup.

```
src/ListingSearch.Api/      API: models, scoring, validation, search service, endpoints, sample data
tests/ListingSearch.Tests/  xUnit tests for the core logic
frontend/                   React + TypeScript UI
```

## Run it

Prerequisites: .NET 8 SDK, Node 18+.

```bash
# 1. API  (http://localhost:5080)
dotnet run --project src/ListingSearch.Api

# 2. UI   (http://localhost:5173, proxies /api -> :5080)
cd frontend
npm install
npm run dev

# 3. Tests
dotnet test
```

Open http://localhost:5173. To use a different dataset, set `Listings:DataPath` in `appsettings.json`
(a relative path resolves against the build output folder) or pass `--Listings:DataPath=/path/to/sample_listings.json`.
Property names are matched case-insensitively and numbers-as-strings are tolerated.

## API

| Endpoint | Description |
|---|---|
| `GET /api/listings/search` | Filtered, ranked, paginated results |
| `GET /api/cities` | Distinct city names (feeds the UI's autocomplete) |
| `GET /api/health` | Liveness check |

Query parameters for search (all optional): `minPrice`, `maxPrice`, `minBedrooms`, `city`, `keyword`,
`targetBudget`, `page` (default 1), `pageSize` (default 10, max 100).

Response:

```json
{
  "items": [{ "source": "MLS_A", "id": "1001", "address": "...", "price": 525000, "bedrooms": 3,
              "score": 0.9202, "scoreBreakdown": { "budgetFit": 0.95, "recency": 0.8507 }, "...": "..." }],
  "page": 1, "pageSize": 10, "totalCount": 12, "totalPages": 2, "hasPrevious": false, "hasNext": true
}
```

## Filtering rules

- `minPrice` / `maxPrice`: inclusive bounds. `minBedrooms`: at least that many.
- `city`: case-, whitespace- and period-insensitive (`" austin "` matches `AUSTIN`; `St. Louis` matches `st louis`),
  because the feeds are inconsistent.
- `keyword`: split on whitespace; **every term must appear** (case-insensitive substring) in the description.
  `pool garage` means pool AND garage.
- Filters combine with AND.

## Scoring approach

Score is in **[0, 1]**, higher is better.

```
budgetFit = 1 - min(|price - targetBudget| / targetBudget, 1)   1.0 at target, 0.0 when >= 100% away
recency   = 0.5 ^ (ageInDays / 30)                              1.0 today, 0.5 after 30 days
score     = 0.7 * budgetFit + 0.3 * recency                     (targetBudget supplied)
score     = recency                                             (no targetBudget)
```

Why this shape:

- **Budget fit is relative, symmetric and bounded.** Being $50k off matters far more on a $200k target than on a $2M one,
  so distance is measured as a fraction of the target. Over and under budget are treated equally (a buyer's "target" is
  a target, not a cap; use `maxPrice` for a hard cap). Clamping at 0 keeps outliers from going negative.
- **Recency decays exponentially** with a 30-day half-life: new listings get a clear boost, but a 6-month-old listing
  is not punished infinitely harder than a 5-month-old one. Future-dated listings (feed clock skew) count as "today".
- **Budget outweighs recency (70/30)** because the user told us their budget explicitly; freshness is an implicit signal.
- **No target budget => recency only**, rather than inventing a budget. The UI says so.
- Weights and half-life are configurable under `Scoring` in `appsettings.json`.
- `scoreBreakdown` is returned so any ranking is explainable (hover the score in the UI).

**Tie-breaking / stable paging.** Results are sorted by score, then newer `listedDate`, then lower price, then
`source`, then `id`. That is a total order, so tied scores never reorder between requests and no listing is duplicated
or skipped across pages. Because `id` is only unique *per source*, identity throughout is `(source, id)`.

## Invalid input

Every problem returns **HTTP 400** as an RFC 7807 validation problem with a message per field. Nothing is silently
clamped or ignored.

| Input | Result |
|---|---|
| `minPrice > maxPrice` | 400, error on both `minPrice` and `maxPrice` |
| negative `minPrice` / `maxPrice` / `minBedrooms`, `targetBudget <= 0` | 400 on that field |
| `pageSize <= 0` or `> 100`, `page < 1` | 400 on that field |
| non-numeric (`minPrice=abc`, `minBedrooms=2.5`) | 400 on that field |
| `keyword` over 200 characters | 400 |
| `city` that matches **no listing at all** (likely a typo) | 400, message lists the known cities |
| `page` beyond the last page | 400 with the real page count |
| valid filters that match nothing (known city, price too high) | **200** with empty `items` and `totalCount: 0` |

Several errors are reported together. Unexpected exceptions become a 500 ProblemDetails, never a stack trace.

## Trade-offs and assumptions

- **Unknown city = error, empty result = 200.** The brief asks for a clear error over a silent empty result for a
  city with no matches. I read "no matches" as "we've never heard of that city", which is almost always a typo, and
  I distinguish it from a legitimate empty search (real city, filters too tight). The UI autocompletes from `/api/cities`.
- **Page out of range is a 400**, not an empty page. The UI resets to page 1 whenever filters or page size change,
  so users don't hit it in normal use.
- **Status is not filtered.** The brief doesn't ask for it, so sold and pending listings appear (with a status badge).
  It is a one-line `Where` if wanted.
- **No cross-feed deduplication.** The sample data contains the same property in two feeds (412 Oak Hollow) and both
  are returned, as separate `(source, id)` listings. Deduplicating needs a matching rule (normalised address + geo
  distance) and a policy for choosing the winner, which I judged out of scope.
- **Keyword search is substring, AND-of-terms.** No stemming, ranking by term frequency, or fuzziness.
- **Scoring is computed per request** over the whole in-memory set (O(n log n)). Fine for thousands of listings; a
  real store would filter in the database and score a bounded candidate set.
- **"Today" comes from `TimeProvider`**, so tests pin the clock and scores are exact. The sample data has fixed dates,
  so recency scores drift as the calendar moves; that does not affect ordering logic or tests.
- The sample data (`Data/sample_listings.json`, 12 listings) was written for this project to cover multiple sources,
  duplicate ids across sources, inconsistent city/state casing, all three statuses and tied prices. Swap in the
  official file via `Listings:DataPath`.

## Design

```
HTTP query string
   -> SearchRequestParser   (raw strings -> SearchQuery; bad numbers -> field errors)
   -> ListingSearchService  (validate -> city check -> filter -> score -> sort -> paginate)
        uses ListingScorer, IListingRepository, TimeProvider
   -> SearchResponse DTO -> JSON
```

The service and scorer are pure (no HTTP, no I/O, injectable clock), which is what makes them straightforward to test
and to extend. `Program.cs` only wires things together and maps `SearchValidationException` to a 400.

Where common follow-ups would go:

| Change | Where |
|---|---|
| New filter (status, sqft, bathrooms, radius) | `SearchQuery` + parser + validator + one `.Where(...)` in `ListingSearchService` |
| Different ranking / new signal | `ListingScorer.Score` (+ `ScoreBreakdown`, `ScoringOptions`) |
| Sort options | replace the `OrderBy` chain, keeping the `(source, id)` tiebreaker |
| Deduplicate across feeds | a step between `GetAll()` and filtering, or in the repository |
| Real database | implement `IListingRepository` |

## Tests

`dotnet test` runs the xUnit suite in `tests/ListingSearch.Tests` (about 75 cases):

- **ScoringTests**: budget-fit symmetry, clamping, recency half-life, future dates, weights, score always in [0, 1]
- **SearchFilterTests**: each filter, inclusive bounds, city normalisation, multi-term keyword, combined filters,
  no matches, same id across sources, empty dataset
- **RankingTests**: budget and recency ordering, **tied scores** (deterministic order, paging with ties), repeatability
- **PaginationTests**: first / last / partial / exact-multiple pages, page size 1 and oversized, no overlap or gaps,
  out-of-range page, empty result
- **ValidationTests**: every invalid-input row in the table above, multiple simultaneous errors, request parsing
