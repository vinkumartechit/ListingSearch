using ListingSearch.Api.Models;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class RankingTests
{
    [Fact]
    public void Closer_to_budget_ranks_higher_when_listed_on_the_same_day()
    {
        var svc = CreateService(
            Make("far", price: 800_000m), Make("near", price: 510_000m), Make("exact", price: 500_000m));

        var ids = svc.Search(new SearchQuery { TargetBudget = 500_000m }).Items.Select(i => i.Listing.Id).ToArray();

        Assert.Equal(new[] { "exact", "near", "far" }, ids);
    }

    [Fact]
    public void Fresher_listing_ranks_higher_when_price_is_equally_close()
    {
        var svc = CreateService(
            Make("old", price: 500_000m, listed: Today.AddDays(-60)),
            Make("new", price: 500_000m, listed: Today.AddDays(-1)));

        var ids = svc.Search(new SearchQuery { TargetBudget = 500_000m }).Items.Select(i => i.Listing.Id).ToArray();

        Assert.Equal(new[] { "new", "old" }, ids);
    }

    [Fact]
    public void Without_a_target_budget_results_are_ordered_newest_first()
    {
        var svc = CreateService(
            Make("b", listed: Today.AddDays(-20)), Make("a", listed: Today.AddDays(-2)), Make("c", listed: Today.AddDays(-90)));

        var ids = svc.Search(new SearchQuery()).Items.Select(i => i.Listing.Id).ToArray();

        Assert.Equal(new[] { "a", "b", "c" }, ids);
    }

    [Fact]
    public void Tied_scores_are_ordered_deterministically_by_source_then_id()
    {
        // Identical price and date => identical score, price and listedDate: only (source, id) can break the tie.
        var svc = CreateService(
            Make("2", source: "MLS_B"), Make("1", source: "MLS_B"), Make("2", source: "MLS_A"), Make("1", source: "MLS_A"));

        var keys = svc.Search(new SearchQuery { TargetBudget = 500_000m }).Items
            .Select(i => $"{i.Listing.Source}/{i.Listing.Id}").ToArray();

        Assert.Equal(new[] { "MLS_A/1", "MLS_A/2", "MLS_B/1", "MLS_B/2" }, keys);
    }

    [Fact]
    public void Tied_scores_prefer_the_newer_listing_then_the_cheaper_one()
    {
        // Budget fit is symmetric, so 450k and 550k score identically against a 500k target.
        var svc = CreateService(
            Make("dear", price: 550_000m), Make("cheap", price: 450_000m));

        var ids = svc.Search(new SearchQuery { TargetBudget = 500_000m }).Items.Select(i => i.Listing.Id).ToArray();

        Assert.Equal(new[] { "cheap", "dear" }, ids);
    }

    [Fact]
    public void Tied_scores_paginate_without_duplicates_or_gaps()
    {
        var listings = Enumerable.Range(1, 7).Select(i => Make($"T{i}", price: 500_000m)).ToArray();
        var svc = CreateService(listings);

        var seen = new List<string>();
        for (var page = 1; page <= 4; page++)
            seen.AddRange(svc.Search(new SearchQuery { TargetBudget = 500_000m, Page = page, PageSize = 2 })
                .Items.Select(i => i.Listing.Id));

        Assert.Equal(7, seen.Count);
        Assert.Equal(7, seen.Distinct().Count());
    }

    [Fact]
    public void Repeating_the_same_search_gives_the_same_order()
    {
        var svc = CreateService(Many(15));
        var q = new SearchQuery { TargetBudget = 310_000m, PageSize = 15 };

        var first = svc.Search(q).Items.Select(i => i.Listing.Id).ToArray();
        var second = svc.Search(q).Items.Select(i => i.Listing.Id).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Result_items_expose_the_score_and_its_breakdown()
    {
        var svc = CreateService(Make("a", price: 500_000m));
        var item = svc.Search(new SearchQuery { TargetBudget = 500_000m }).Items.Single();

        Assert.Equal(1.0, item.Score, 6);
        Assert.Equal(1.0, item.Breakdown.BudgetFit!.Value, 6);
        Assert.Equal(1.0, item.Breakdown.Recency, 6);
    }
}
