using ListingSearch.Api.Models;
using ListingSearch.Api.Services;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class StatusFilterTests
{
    [Fact]
    public void Status_filter_keeps_only_listings_with_that_status()
    {
        var svc = CreateService(
            Make("a", status: "active"), Make("b", status: "sold"), Make("c", status: "active"));

        var ids = svc.Search(new SearchQuery { Status = "active" }).Items
            .Select(i => i.Listing.Id).OrderBy(x => x).ToArray();

        Assert.Equal(new[] { "a", "c" }, ids);
    }

    [Fact]
    public void Status_filter_ignores_case()
    {
        var svc = CreateService(Make("a", status: "active"), Make("b", status: "sold"));
        Assert.Equal(1, svc.Search(new SearchQuery { Status = "SOLD" }).TotalCount);
    }

    [Fact]
    public void Unknown_status_is_rejected_with_a_clear_message()
    {
        var ex = Assert.Throws<SearchValidationException>(() =>
            CreateService(Make("a")).Search(new SearchQuery { Status = "archived" }));

        Assert.Contains("status", ex.Errors.Keys);
    }
}