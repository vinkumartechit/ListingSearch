namespace ListingSearch.Api.Models;

/// <summary>
/// A property listing as ingested from a data feed. <c>Id</c> is only unique
/// within a <c>Source</c>, so the real identity is the pair (Source, Id).
/// </summary>
public sealed record Listing(
    string Id,
    string Source,
    string Address,
    string City,
    string State,
    string Zip,
    decimal Price,
    int Bedrooms,
    double Bathrooms,
    int Sqft,
    double Latitude,
    double Longitude,
    DateOnly ListedDate,
    string Status,
    string Description);
