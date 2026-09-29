using System.Globalization;
using ListingSearch.Api.Models;

namespace ListingSearch.Api.Services;

/// <summary>
/// Turns raw query-string values into a <see cref="SearchQuery"/>. We parse by hand (instead of relying on
/// framework model binding) so that "minPrice=abc" produces the same field-level 400 as every other
/// invalid input, rather than a framework-specific error body.
/// </summary>
public static class SearchRequestParser
{
    public static SearchQuery Parse(IReadOnlyDictionary<string, string?> raw)
    {
        var errors = new Dictionary<string, string[]>();

        string? Get(string key) =>
            raw.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        decimal? Dec(string key)
        {
            var s = Get(key);
            if (s is null) return null;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
            errors[key] = new[] { $"'{s}' is not a valid number." };
            return null;
        }

        int? Int(string key)
        {
            var s = Get(key);
            if (s is null) return null;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
            errors[key] = new[] { $"'{s}' is not a valid whole number." };
            return null;
        }

        var query = new SearchQuery
        {
            MinPrice = Dec("minPrice"),
            MaxPrice = Dec("maxPrice"),
            MinBedrooms = Int("minBedrooms"),
            City = Get("city"),
            Status = Get("status"),
            Keyword = Get("keyword"),
            TargetBudget = Dec("targetBudget"),
            Page = Int("page") ?? 1,
            PageSize = Int("pageSize") ?? SearchQuery.DefaultPageSize,
        };

        if (errors.Count > 0) throw new SearchValidationException(errors);
        return query;
    }
}
