using ListingSearch.Api.Data;
using ListingSearch.Api.Models;
using ListingSearch.Api.Services;

namespace ListingSearch.Tests;

/// <summary>A clock frozen at a known instant so recency scores are exact and repeatable.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;
    public FixedTimeProvider(DateTimeOffset now) => _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
}

public static class TestSupport
{
    public static readonly DateOnly Today = new(2026, 9, 29);

    public static ListingSearchService CreateService(params Listing[] listings) =>
        new(new InMemoryListingRepository(listings),
            new ListingScorer(),
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero)));

    public static Listing Make(
        string id = "1", string source = "MLS_A", string city = "Austin", string status = "active" ,decimal price = 500_000m,
        int bedrooms = 3, string description = "Nice home", DateOnly? listed = null, string address = "1 Main St") =>
        new(id, source, address, city, "TX", "78701", price, bedrooms, 2.0, 1800, 30.0, -97.0,
            listed ?? Today, status, description);

            

    /// <summary>n listings with distinct prices, so ordering is unambiguous. Ids are "L01".."Lnn".</summary>
    public static Listing[] Many(int n) =>
        Enumerable.Range(1, n)
            .Select(i => Make(id: $"L{i:00}", price: 300_000m + i * 1_000m, listed: Today.AddDays(-i)))
            .ToArray();
}
