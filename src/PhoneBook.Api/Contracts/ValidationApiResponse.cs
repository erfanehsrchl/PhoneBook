namespace PhoneBook.Api.Contracts;

/// <summary>
/// Represents application validation failures grouped by input field.
/// </summary>
public sealed record ValidationApiResponse(
    int StatusCode,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors,
    string? ErrorCode = "Validation.Failed")
    : ApiResponse(StatusCode, Message, ErrorCode);
