using ListingSearch.Api.Data;
using ListingSearch.Api.Models;

namespace ListingSearch.Api.Services;

/// <summary>
/// Filter -> score -> sort -> paginate. No I/O and no HTTP concerns, so it is easy to unit test and to extend.
/// </summary>
public sealed class ListingSearchService
{
    private readonly IListingRepository _repository;
    private readonly ListingScorer _scorer;
    private readonly TimeProvider _time;

    public ListingSearchService(IListingRepository repository, ListingScorer scorer, TimeProvider time)
    {
        _repository = repository;
        _scorer = scorer;
        _time = time;
    }

    public SearchResult Search(SearchQuery query)
    {
        SearchQueryValidator.ValidateOrThrow(query);

        var all = _repository.GetAll();
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

        // A city we have never heard of is almost certainly a typo. Say so, instead of returning a
        // silent empty page. (A known city whose listings are all filtered out is a normal empty result.)
        var cityKey = NormalizeCity(query.City);
        if (cityKey is not null && !all.Any(l => NormalizeCity(l.City) == cityKey))
        {
            var known = string.Join(", ", KnownCities(all));
            throw new SearchValidationException("city",
                $"No listings found for city '{query.City}'. Known cities: {known}.");
        }

        var terms = SplitKeyword(query.Keyword);

        var ranked = all
            .Where(l => query.MinPrice is null || l.Price >= query.MinPrice)
            .Where(l => query.MaxPrice is null || l.Price <= query.MaxPrice)
            .Where(l => query.MinBedrooms is null || l.Bedrooms >= query.MinBedrooms)
            .Where(l => cityKey is null || NormalizeCity(l.City) == cityKey)
            .Where(l => terms.Length == 0 || MatchesAllTerms(l.Description, terms))
            .Where(l => query.Status is null || string.Equals(l.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Select(l => _scorer.Score(l, query.TargetBudget, today))
            // Total order => deterministic pagination even when scores tie:
            // score desc, newer first, cheaper first, then (source, id) as the final tiebreaker.
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Listing.ListedDate)
            .ThenBy(s => s.Listing.Price)
            .ThenBy(s => s.Listing.Source, StringComparer.Ordinal)
            .ThenBy(s => s.Listing.Id, StringComparer.Ordinal)
            .ToList();

        var total = ranked.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);

        if (total > 0 && query.Page > totalPages)
        {
            throw new SearchValidationException("page",
                $"page {query.Page} is out of range; there {(totalPages == 1 ? "is 1 page" : $"are {totalPages} pages")} of results.");
        }

        var items = ranked.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
        return new SearchResult(items, query.Page, query.PageSize, total, totalPages);
    }

    public IReadOnlyList<string> Cities() => KnownCities(_repository.GetAll());

    // ---- helpers -------------------------------------------------------------------------------------

    private static IReadOnlyList<string> KnownCities(IEnumerable<Listing> listings) =>
        listings
            .GroupBy(l => NormalizeCity(l.City)!)
            .Select(g => g.Select(l => l.City.Trim()).GroupBy(c => c).OrderByDescending(x => x.Count()).First().Key)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Case/whitespace/period-insensitive key, so "austin", " Austin " and "AUSTIN" all match.</summary>
    internal static string? NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var parts = city.Replace(".", "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    private static string[] SplitKeyword(string? keyword) =>
        string.IsNullOrWhiteSpace(keyword)
            ? Array.Empty<string>()
            : keyword.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Every term must appear (case-insensitive substring) - "pool garage" means pool AND garage.</summary>
    private static bool MatchesAllTerms(string description, string[] terms) =>
        terms.All(t => description.Contains(t, StringComparison.OrdinalIgnoreCase));
}
