using ListingSearch.Api.Models;

namespace ListingSearch.Api.Services;

/// <summary>
/// Computes a relevance score in [0, 1]. Pure and deterministic given "today".
///
/// score = wB * budgetFit + wR * recency          (when targetBudget is supplied)
/// score = recency                                 (when it is not)
///
/// budgetFit = 1 - min(|price - target| / target, 1)   -> 1.0 at the target, 0.0 at >=100% away
/// recency   = 0.5 ^ (ageInDays / halfLifeDays)        -> 1.0 today, 0.5 after one half-life
/// </summary>
public sealed class ListingScorer
{
    private readonly ScoringOptions _options;

    public ListingScorer(ScoringOptions? options = null)
    {
        _options = options ?? new ScoringOptions();
    }

    public ScoredListing Score(Listing listing, decimal? targetBudget, DateOnly today)
    {
        var recency = Recency(listing.ListedDate, today);

        if (targetBudget is null)
        {
            // Without a budget there is nothing to be "close" to; ranking is purely by freshness.
            return new ScoredListing(listing, recency, new ScoreBreakdown(null, recency));
        }

        var budgetFit = BudgetFit(listing.Price, targetBudget.Value);
        var total = _options.BudgetWeight * budgetFit + _options.RecencyWeight * recency;
        return new ScoredListing(listing, total, new ScoreBreakdown(budgetFit, recency));
    }

    public double BudgetFit(decimal price, decimal target)
    {
        if (target <= 0) throw new ArgumentOutOfRangeException(nameof(target), "Target budget must be positive.");
        var relativeDistance = (double)(Math.Abs(price - target) / target);
        return 1.0 - Math.Min(relativeDistance, 1.0);
    }

    public double Recency(DateOnly listedDate, DateOnly today)
    {
        // A listing dated in the future (feed clock skew) is treated as brand new, not "more than new".
        var ageDays = Math.Max(0, today.DayNumber - listedDate.DayNumber);
        return Math.Pow(0.5, ageDays / _options.RecencyHalfLifeDays);
    }
}
