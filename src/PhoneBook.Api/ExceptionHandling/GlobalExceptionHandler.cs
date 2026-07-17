using Microsoft.AspNetCore.Diagnostics;

namespace PhoneBook.Api.ExceptionHandling;

/// <summary>
/// Converts unexpected exceptions into safe RFC 7807 responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unexpected exception occurred while processing {RequestPath}.",
            httpContext.Request.Path);

        IResult response = Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            detail: "An unexpected error occurred.",
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "Server.Unexpected"
            });

        await response.ExecuteAsync(httpContext);
        return true;
    }
}
