using System.Text.Json;
using ListingSearch.Api.Models;

namespace ListingSearch.Api.Data;

/// <summary>Holds listings in memory. Loaded once from a JSON file at startup.</summary>
public sealed class InMemoryListingRepository : IListingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Feeds are messy; tolerate numbers-as-strings (e.g. "zip": 78701 or "price": "450000").
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    private readonly IReadOnlyList<Listing> _listings;

    public InMemoryListingRepository(IEnumerable<Listing> listings)
    {
        _listings = listings.ToList();
    }

    public IReadOnlyList<Listing> GetAll() => _listings;

    public static InMemoryListingRepository FromJsonFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Listings data file not found: {path}", path);

        var json = File.ReadAllText(path);
        var listings = JsonSerializer.Deserialize<List<Listing>>(json, JsonOptions)
                       ?? throw new InvalidDataException($"'{path}' did not contain a JSON array of listings.");
        return new InMemoryListingRepository(listings);
    }
}
