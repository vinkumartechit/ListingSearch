using ListingSearch.Api.Data;
using ListingSearch.Api.Models;
using ListingSearch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- services --------------------------------------------------------------------------------------
var dataPath = builder.Configuration["Listings:DataPath"] ?? "Data/sample_listings.json";
if (!Path.IsPathRooted(dataPath)) dataPath = Path.Combine(AppContext.BaseDirectory, dataPath);

builder.Services.AddSingleton<IListingRepository>(_ => InMemoryListingRepository.FromJsonFile(dataPath));
builder.Services.AddSingleton(builder.Configuration.GetSection("Scoring").Get<ScoringOptions>() ?? new ScoringOptions());
builder.Services.AddSingleton<ListingScorer>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ListingSearchService>();
builder.Services.AddProblemDetails();

// The Vite dev server proxies /api, so CORS is only a fallback for running the UI on another origin.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();   // unexpected errors -> 500 ProblemDetails, never a stack trace
app.UseCors();

// ---- endpoints -------------------------------------------------------------------------------------
app.MapGet("/api/listings/search", (HttpRequest request, ListingSearchService search) =>
{
    try
    {
        var raw = request.Query.ToDictionary(kv => kv.Key, kv => (string?)kv.Value.FirstOrDefault(),
            StringComparer.OrdinalIgnoreCase);
        var query = SearchRequestParser.Parse(raw);
        var result = search.Search(query);
        return Results.Ok(SearchResponse.From(result));
    }
    catch (SearchValidationException ex)
    {
        return Results.ValidationProblem(
            ex.Errors.ToDictionary(e => e.Key, e => e.Value),
            title: "One or more search parameters are invalid.",
            statusCode: StatusCodes.Status400BadRequest);
    }
});

app.MapGet("/api/cities", (ListingSearchService search) => Results.Ok(search.Cities()));

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// ---- response shape --------------------------------------------------------------------------------
public sealed record ListingDto(
    string Id, string Source, string Address, string City, string State, string Zip,
    decimal Price, int Bedrooms, double Bathrooms, int Sqft, DateOnly ListedDate, string Status,
    string Description, double Score, ScoreBreakdown ScoreBreakdown);

public sealed record SearchResponse(
    IReadOnlyList<ListingDto> Items, int Page, int PageSize, int TotalCount, int TotalPages,
    bool HasPrevious, bool HasNext)
{
    public static SearchResponse From(SearchResult r) => new(
        r.Items.Select(s => new ListingDto(
            s.Listing.Id, s.Listing.Source, s.Listing.Address, s.Listing.City, s.Listing.State, s.Listing.Zip,
            s.Listing.Price, s.Listing.Bedrooms, s.Listing.Bathrooms, s.Listing.Sqft, s.Listing.ListedDate,
            s.Listing.Status, s.Listing.Description, Math.Round(s.Score, 4),
            new ScoreBreakdown(s.Breakdown.BudgetFit is { } b ? Math.Round(b, 4) : null, Math.Round(s.Breakdown.Recency, 4)))).ToList(),
        r.Page, r.PageSize, r.TotalCount, r.TotalPages, r.HasPrevious, r.HasNext);
}

// Lets a test project reference Program if integration tests are added later.
public partial class Program { }
