using ListingSearch.Api.Models;

namespace ListingSearch.Api.Services;

/// <summary>
/// Validates everything that can be checked without looking at the data.
/// (Checks that depend on the data - unknown city, page out of range - live in the search service.)
/// </summary>
/// 


public static class SearchQueryValidator
{
    private static readonly HashSet<string> AllowedStatuses =    new(StringComparer.OrdinalIgnoreCase) { "active", "pending", "sold" };

    public static void ValidateOrThrow(SearchQuery q)
    {
        var errors = new Dictionary<string, List<string>>();

        
        void Add(string field, string msg)
        {
            if (!errors.TryGetValue(field, out var list)) errors[field] = list = new List<string>();
            list.Add(msg);
        }

        if (q.Status is not null && !AllowedStatuses.Contains(q.Status))
            Add("status", "status must be one of: active, pending, sold.");

        if (q.MinPrice is < 0) Add("minPrice", "minPrice cannot be negative.");
        if (q.MaxPrice is < 0) Add("maxPrice", "maxPrice cannot be negative.");
        if (q.MinPrice is { } lo && q.MaxPrice is { } hi && lo > hi)
        {
            Add("minPrice", $"minPrice ({lo:0.##}) cannot be greater than maxPrice ({hi:0.##}).");
            Add("maxPrice", $"maxPrice ({hi:0.##}) cannot be less than minPrice ({lo:0.##}).");
        }

        if (q.MinBedrooms is < 0) Add("minBedrooms", "minBedrooms cannot be negative.");
        if (q.TargetBudget is <= 0) Add("targetBudget", "targetBudget must be greater than zero.");

        if (q.Keyword is { Length: > SearchQuery.MaxKeywordLength })
            Add("keyword", $"keyword cannot be longer than {SearchQuery.MaxKeywordLength} characters.");

        if (q.Page < 1) Add("page", "page must be 1 or greater.");
        if (q.PageSize < 1) Add("pageSize", "pageSize must be 1 or greater.");
        else if (q.PageSize > SearchQuery.MaxPageSize)
            Add("pageSize", $"pageSize cannot be greater than {SearchQuery.MaxPageSize}.");

        if (errors.Count > 0)
            throw new SearchValidationException(
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}
