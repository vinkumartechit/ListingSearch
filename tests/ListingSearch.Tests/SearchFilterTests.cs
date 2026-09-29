using ListingSearch.Api.Models;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class SearchFilterTests
{
    private static string[] Ids(SearchResult r) => r.Items.Select(i => i.Listing.Id).OrderBy(x => x).ToArray();

    [Fact]
    public void No_filters_returns_everything()
    {
        var svc = CreateService(Many(4));
        Assert.Equal(4, svc.Search(new SearchQuery()).TotalCount);
    }

    [Fact]
    public void MinPrice_and_MaxPrice_are_inclusive()
    {
        var svc = CreateService(
            Make("a", price: 100_000m), Make("b", price: 200_000m), Make("c", price: 300_000m), Make("d", price: 400_000m));

        var r = svc.Search(new SearchQuery { MinPrice = 200_000m, MaxPrice = 300_000m });

        Assert.Equal(new[] { "b", "c" }, Ids(r));
    }

    [Fact]
    public void MinBedrooms_keeps_listings_with_at_least_that_many()
    {
        var svc = CreateService(Make("a", bedrooms: 1), Make("b", bedrooms: 3), Make("c", bedrooms: 5));
        Assert.Equal(new[] { "b", "c" }, Ids(svc.Search(new SearchQuery { MinBedrooms = 3 })));
    }

    [Theory]
    [InlineData("Austin")]
    [InlineData("austin")]
    [InlineData("AUSTIN")]
    [InlineData("  Austin  ")]
    public void City_matching_ignores_case_and_surrounding_whitespace(string input)
    {
        var svc = CreateService(Make("a", city: "Austin"), Make("b", city: "AUSTIN"), Make("c", city: "Dallas"));
        Assert.Equal(new[] { "a", "b" }, Ids(svc.Search(new SearchQuery { City = input })));
    }

    [Fact]
    public void City_matching_ignores_periods_and_repeated_spaces()
    {
        var svc = CreateService(Make("a", city: "St. Louis"), Make("b", city: "Austin"));
        Assert.Equal(new[] { "a" }, Ids(svc.Search(new SearchQuery { City = "st   louis" })));
    }

    [Fact]
    public void Keyword_is_case_insensitive_and_matches_the_description()
    {
        var svc = CreateService(Make("a", description: "Heated POOL and patio"), Make("b", description: "Big garage"));
        Assert.Equal(new[] { "a" }, Ids(svc.Search(new SearchQuery { Keyword = "pool" })));
    }

    [Fact]
    public void Multiple_keyword_terms_must_all_match()
    {
        var svc = CreateService(
            Make("a", description: "pool and garage"), Make("b", description: "pool only"), Make("c", description: "garage only"));
        Assert.Equal(new[] { "a" }, Ids(svc.Search(new SearchQuery { Keyword = "pool garage" })));
    }

    [Fact]
    public void Blank_keyword_is_treated_as_no_keyword()
    {
        var svc = CreateService(Many(3));
        Assert.Equal(3, svc.Search(new SearchQuery { Keyword = "   " }).TotalCount);
    }

    [Fact]
    public void All_filters_combine_with_AND()
    {
        var svc = CreateService(
            Make("match", city: "Austin", price: 450_000m, bedrooms: 3, description: "has a pool"),
            Make("wrong-city", city: "Dallas", price: 450_000m, bedrooms: 3, description: "has a pool"),
            Make("too-pricey", city: "Austin", price: 900_000m, bedrooms: 3, description: "has a pool"),
            Make("too-small", city: "Austin", price: 450_000m, bedrooms: 1, description: "has a pool"),
            Make("no-pool", city: "Austin", price: 450_000m, bedrooms: 3, description: "has a yard"));

        var r = svc.Search(new SearchQuery
        {
            City = "austin", MinPrice = 400_000m, MaxPrice = 500_000m, MinBedrooms = 3, Keyword = "pool"
        });

        Assert.Equal(new[] { "match" }, Ids(r));
    }

    [Fact]
    public void Known_city_with_no_remaining_matches_is_an_empty_result_not_an_error()
    {
        var svc = CreateService(Make("a", city: "Austin", price: 100_000m));

        var r = svc.Search(new SearchQuery { City = "Austin", MinPrice = 900_000m });

        Assert.Empty(r.Items);
        Assert.Equal(0, r.TotalCount);
        Assert.Equal(0, r.TotalPages);
        Assert.False(r.HasNext);
        Assert.False(r.HasPrevious);
    }

    [Fact]
    public void Same_id_from_different_sources_are_distinct_listings()
    {
        var svc = CreateService(Make("1001", source: "MLS_A", address: "A St"), Make("1001", source: "MLS_B", address: "B St"));

        var r = svc.Search(new SearchQuery());

        Assert.Equal(2, r.TotalCount);
        Assert.Equal(new[] { "A St", "B St" }, r.Items.Select(i => i.Listing.Address).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void Empty_dataset_returns_an_empty_page()
    {
        var r = CreateService().Search(new SearchQuery());
        Assert.Equal(0, r.TotalCount);
        Assert.Empty(r.Items);
    }
}
