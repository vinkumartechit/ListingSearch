namespace ListingSearch.Api.Services;

/// <summary>Tunable knobs for relevance scoring. See README for the rationale.</summary>
public sealed class ScoringOptions
{
    /// <summary>Weight of how close the price is to targetBudget (0..1).</summary>
    public double BudgetWeight { get; init; } = 0.7;

    /// <summary>Weight of how recently the listing went live (0..1).</summary>
    public double RecencyWeight { get; init; } = 0.3;

    /// <summary>Days after which the recency signal halves.</summary>
    public double RecencyHalfLifeDays { get; init; } = 30;
}
