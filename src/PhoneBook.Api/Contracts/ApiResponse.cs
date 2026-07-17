namespace PhoneBook.Api.Contracts;

/// <summary>
/// Represents a consistent API response without a data payload.
/// </summary>
public record ApiResponse(
    int StatusCode,
    string Message,
    string? ErrorCode = null);

/// <summary>
/// Represents a consistent API response with a data payload.
/// </summary>
/// <typeparam name="T">The response data type.</typeparam>
public record ApiResponse<T>(
    T? Data,
    int StatusCode,
    string Message,
    string? ErrorCode = null)
    : ApiResponse(StatusCode, Message, ErrorCode);
