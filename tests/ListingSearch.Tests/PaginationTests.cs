using ListingSearch.Api.Services;
using ListingSearch.Api.Models;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class PaginationTests
{
    [Fact]
    public void First_page_of_many_has_next_but_not_previous()
    {
        var r = CreateService(Many(25)).Search(new SearchQuery { Page = 1, PageSize = 10 });

        Assert.Equal(10, r.Items.Count);
        Assert.Equal(25, r.TotalCount);
        Assert.Equal(3, r.TotalPages);
        Assert.True(r.HasNext);
        Assert.False(r.HasPrevious);
    }

    [Fact]
    public void Last_page_holds_the_remainder_and_has_no_next()
    {
        var r = CreateService(Many(25)).Search(new SearchQuery { Page = 3, PageSize = 10 });

        Assert.Equal(5, r.Items.Count);
        Assert.False(r.HasNext);
        Assert.True(r.HasPrevious);
    }

    [Fact]
    public void Exact_multiple_of_page_size_has_no_trailing_empty_page()
    {
        var svc = CreateService(Many(20));

        var last = svc.Search(new SearchQuery { Page = 2, PageSize = 10 });

        Assert.Equal(2, last.TotalPages);
        Assert.Equal(10, last.Items.Count);
        Assert.Throws<SearchValidationException>(() => svc.Search(new SearchQuery { Page = 3, PageSize = 10 }));
    }

    [Fact]
    public void Page_size_larger_than_result_set_returns_a_single_page()
    {
        var r = CreateService(Many(3)).Search(new SearchQuery { PageSize = 50 });

        Assert.Equal(3, r.Items.Count);
        Assert.Equal(1, r.TotalPages);
        Assert.False(r.HasNext);
    }

    [Fact]
    public void Page_size_of_one_pages_through_every_listing()
    {
        var svc = CreateService(Many(4));
        var ids = Enumerable.Range(1, 4)
            .Select(p => svc.Search(new SearchQuery { Page = p, PageSize = 1 }).Items.Single().Listing.Id)
            .ToArray();

        Assert.Equal(4, ids.Distinct().Count());
    }

    [Fact]
    public void Pages_do_not_overlap_and_together_cover_every_match()
    {
        var svc = CreateService(Many(23));
        var all = new List<string>();
        for (var p = 1; p <= 3; p++)
            all.AddRange(svc.Search(new SearchQuery { Page = p, PageSize = 10 }).Items.Select(i => i.Listing.Id));

        Assert.Equal(23, all.Count);
        Assert.Equal(23, all.Distinct().Count());
    }

    [Fact]
    public void Page_beyond_the_last_page_is_an_error_not_a_silent_empty_page()
    {
        var ex = Assert.Throws<SearchValidationException>(() =>
            CreateService(Many(5)).Search(new SearchQuery { Page = 2, PageSize = 10 }));

        Assert.Contains("page", ex.Errors.Keys);
        Assert.Contains("out of range", ex.Errors["page"][0]);
    }

    [Fact]
    public void Page_one_of_an_empty_result_is_fine()
    {
        var r = CreateService(Many(3)).Search(new SearchQuery { MinPrice = 99_000_000m, Page = 1 });

        Assert.Empty(r.Items);
        Assert.Equal(0, r.TotalPages);
    }
}
