namespace ListingSearch.Api.Services;

/// <summary>
/// Thrown when a search request is invalid. Carries field-level messages that
/// the API layer turns into an RFC 7807 validation problem (HTTP 400).
/// </summary>
public sealed class SearchValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public SearchValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more search parameters are invalid: " +
               string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(" ", e.Value)}")))
    {
        Errors = errors;
    }

    public SearchValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }
}
