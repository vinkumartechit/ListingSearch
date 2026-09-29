using ListingSearch.Api.Services;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class ScoringTests
{
    private readonly ListingScorer _scorer = new();

    [Fact]
    public void BudgetFit_is_one_when_price_equals_target() =>
        Assert.Equal(1.0, _scorer.BudgetFit(500_000m, 500_000m), 6);

    [Theory]
    [InlineData(450_000, 0.9)]   // 10% under
    [InlineData(550_000, 0.9)]   // 10% over  -> symmetric
    [InlineData(250_000, 0.5)]   // 50% under
    [InlineData(750_000, 0.5)]   // 50% over
    public void BudgetFit_falls_off_linearly_and_symmetrically(int price, double expected) =>
        Assert.Equal(expected, _scorer.BudgetFit(price, 500_000m), 6);

    [Theory]
    [InlineData(1_000_000)]      // exactly 100% away
    [InlineData(5_000_000)]      // far beyond: clamped, never negative
    [InlineData(0)]
    public void BudgetFit_is_clamped_at_zero(int price) =>
        Assert.Equal(0.0, _scorer.BudgetFit(price, 500_000m), 6);

    [Fact]
    public void BudgetFit_rejects_non_positive_target() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _scorer.BudgetFit(100m, 0m));

    [Fact]
    public void Recency_is_one_for_a_listing_dated_today() =>
        Assert.Equal(1.0, _scorer.Recency(Today, Today), 6);

    [Fact]
    public void Recency_halves_every_half_life() =>
        Assert.Equal(0.5, _scorer.Recency(Today.AddDays(-30), Today), 6);

    [Fact]
    public void Recency_decays_monotonically() =>
        Assert.True(_scorer.Recency(Today.AddDays(-10), Today) > _scorer.Recency(Today.AddDays(-11), Today));

    [Fact]
    public void Recency_treats_future_dates_as_brand_new() =>
        Assert.Equal(1.0, _scorer.Recency(Today.AddDays(5), Today), 6);

    [Fact]
    public void Score_combines_budget_and_recency_with_configured_weights()
    {
        // price 10% off target (0.9), listed 30 days ago (0.5) -> 0.7*0.9 + 0.3*0.5 = 0.78
        var scored = _scorer.Score(Make(price: 550_000m, listed: Today.AddDays(-30)), 500_000m, Today);
        Assert.Equal(0.78, scored.Score, 6);
        Assert.Equal(0.9, scored.Breakdown.BudgetFit!.Value, 6);
        Assert.Equal(0.5, scored.Breakdown.Recency, 6);
    }

    [Fact]
    public void Score_without_a_target_budget_is_recency_only()
    {
        var scored = _scorer.Score(Make(price: 1m, listed: Today.AddDays(-30)), null, Today);
        Assert.Equal(0.5, scored.Score, 6);
        Assert.Null(scored.Breakdown.BudgetFit);
    }

    [Fact]
    public void Score_always_lies_between_zero_and_one()
    {
        foreach (var price in new[] { 1m, 499_999m, 500_000m, 9_000_000m })
        foreach (var age in new[] { -3, 0, 1, 90, 4000 })
        {
            var s = _scorer.Score(Make(price: price, listed: Today.AddDays(-age)), 500_000m, Today).Score;
            Assert.InRange(s, 0.0, 1.0);
        }
    }
}
