using ListingSearch.Api.Models;
using ListingSearch.Api.Services;
using static ListingSearch.Tests.TestSupport;

namespace ListingSearch.Tests;

public class ValidationTests
{
    private static SearchValidationException Invalid(SearchQuery q) =>
        Assert.Throws<SearchValidationException>(() => CreateService(Many(3)).Search(q));

    [Fact]
    public void MinPrice_greater_than_MaxPrice_is_rejected_on_both_fields()
    {
        var ex = Invalid(new SearchQuery { MinPrice = 500_000m, MaxPrice = 100_000m });

        Assert.Contains("minPrice", ex.Errors.Keys);
        Assert.Contains("maxPrice", ex.Errors.Keys);
    }

    [Fact]
    public void MinPrice_equal_to_MaxPrice_is_allowed()
    {
        var svc = CreateService(Make("a", price: 300_000m));
        Assert.Equal(1, svc.Search(new SearchQuery { MinPrice = 300_000m, MaxPrice = 300_000m }).TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Non_positive_page_size_is_rejected(int pageSize) =>
        Assert.Contains("pageSize", Invalid(new SearchQuery { PageSize = pageSize }).Errors.Keys);

    [Fact]
    public void Page_size_above_the_maximum_is_rejected() =>
        Assert.Contains("pageSize", Invalid(new SearchQuery { PageSize = SearchQuery.MaxPageSize + 1 }).Errors.Keys);

    [Fact]
    public void Page_size_at_the_maximum_is_accepted() =>
        Assert.Equal(3, CreateService(Many(3)).Search(new SearchQuery { PageSize = SearchQuery.MaxPageSize }).TotalCount);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Non_positive_page_is_rejected(int page) =>
        Assert.Contains("page", Invalid(new SearchQuery { Page = page }).Errors.Keys);

    [Fact]
    public void Negative_prices_are_rejected()
    {
        var ex = Invalid(new SearchQuery { MinPrice = -1m, MaxPrice = -2m });
        Assert.Contains("minPrice", ex.Errors.Keys);
        Assert.Contains("maxPrice", ex.Errors.Keys);
    }

    [Fact]
    public void Negative_MinBedrooms_is_rejected() =>
        Assert.Contains("minBedrooms", Invalid(new SearchQuery { MinBedrooms = -1 }).Errors.Keys);

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Non_positive_TargetBudget_is_rejected(int target) =>
        Assert.Contains("targetBudget", Invalid(new SearchQuery { TargetBudget = target }).Errors.Keys);

    [Fact]
    public void Overlong_keyword_is_rejected() =>
        Assert.Contains("keyword", Invalid(new SearchQuery { Keyword = new string('x', SearchQuery.MaxKeywordLength + 1) }).Errors.Keys);

    [Fact]
    public void Unknown_city_is_an_error_that_lists_the_known_cities()
    {
        var svc = CreateService(Make("a", city: "Austin"), Make("b", city: "Dallas"));

        var ex = Assert.Throws<SearchValidationException>(() => svc.Search(new SearchQuery { City = "Atlantis" }));

        Assert.Contains("city", ex.Errors.Keys);
        Assert.Contains("Atlantis", ex.Errors["city"][0]);
        Assert.Contains("Austin", ex.Errors["city"][0]);
        Assert.Contains("Dallas", ex.Errors["city"][0]);
    }

    [Fact]
    public void Several_problems_are_reported_together()
    {
        var ex = Invalid(new SearchQuery { MinPrice = 5m, MaxPrice = 1m, PageSize = 0, MinBedrooms = -2 });

        Assert.True(ex.Errors.Count >= 3);
    }

    [Fact]
    public void Cities_lists_each_city_once_using_its_most_common_spelling()
    {
        var svc = CreateService(
            Make("a", city: "Austin"), Make("b", city: "Austin"), Make("c", city: "austin"), Make("d", city: "Dallas"));

        Assert.Equal(new[] { "Austin", "Dallas" }, svc.Cities().ToArray());
    }
}

public class RequestParserTests
{
    private static IReadOnlyDictionary<string, string?> Raw(params (string, string?)[] kv) =>
        kv.ToDictionary(x => x.Item1, x => x.Item2);

    [Fact]
    public void Empty_request_gets_defaults()
    {
        var q = SearchRequestParser.Parse(Raw());

        Assert.Equal(1, q.Page);
        Assert.Equal(SearchQuery.DefaultPageSize, q.PageSize);
        Assert.Null(q.MinPrice);
        Assert.Null(q.City);
    }

    [Fact]
    public void Valid_values_are_parsed()
    {
        var q = SearchRequestParser.Parse(Raw(
            ("minPrice", "100000"), ("maxPrice", "450000.50"), ("minBedrooms", "3"), ("city", " Austin "),
            ("keyword", "pool"), ("targetBudget", "300000"), ("page", "2"), ("pageSize", "25")));

        Assert.Equal(100_000m, q.MinPrice);
        Assert.Equal(450_000.50m, q.MaxPrice);
        Assert.Equal(3, q.MinBedrooms);
        Assert.Equal("Austin", q.City);
        Assert.Equal("pool", q.Keyword);
        Assert.Equal(300_000m, q.TargetBudget);
        Assert.Equal(2, q.Page);
        Assert.Equal(25, q.PageSize);
    }

    [Fact]
    public void Blank_values_are_treated_as_absent()
    {
        var q = SearchRequestParser.Parse(Raw(("minPrice", ""), ("city", "  "), ("page", "")));

        Assert.Null(q.MinPrice);
        Assert.Null(q.City);
        Assert.Equal(1, q.Page);
    }

    [Theory]
    [InlineData("minPrice", "abc")]
    [InlineData("maxPrice", "12k")]
    [InlineData("targetBudget", "lots")]
    [InlineData("minBedrooms", "2.5")]
    [InlineData("page", "one")]
    [InlineData("pageSize", "ten")]
    public void Non_numeric_values_are_reported_against_their_field(string field, string value)
    {
        var ex = Assert.Throws<SearchValidationException>(() => SearchRequestParser.Parse(Raw((field, value))));

        Assert.Contains(field, ex.Errors.Keys);
    }

    [Fact]
    public void Decimal_point_is_always_a_dot_regardless_of_server_culture()
    {
        var q = SearchRequestParser.Parse(Raw(("minPrice", "1234.5")));
        Assert.Equal(1234.5m, q.MinPrice);
    }
}
