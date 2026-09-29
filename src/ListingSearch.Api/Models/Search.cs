namespace ListingSearch.Api.Models;

/// <summary>A validated, normalised search request.</summary>
public sealed record SearchQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    public const int MaxKeywordLength = 200;

    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public string? City { get; init; }
    public string? Keyword { get; init; }
    public decimal? TargetBudget { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Status { get; init; }
}

public sealed record ScoreBreakdown(double? BudgetFit, double Recency);

public sealed record ScoredListing(Listing Listing, double Score, ScoreBreakdown Breakdown);

public sealed record SearchResult(
    IReadOnlyList<ScoredListing> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
