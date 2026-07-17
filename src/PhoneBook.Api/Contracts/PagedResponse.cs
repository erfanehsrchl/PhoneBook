namespace PhoneBook.Api.Contracts;

/// <summary>
/// Represents one page of an ordered collection.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
