using ListingSearch.Api.Models;

namespace ListingSearch.Api.Data;

public interface IListingRepository
{
    IReadOnlyList<Listing> GetAll();
}
