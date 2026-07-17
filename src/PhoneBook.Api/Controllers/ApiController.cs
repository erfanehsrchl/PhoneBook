using Microsoft.AspNetCore.Mvc;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Api.Controllers;

/// <summary>
/// Provides centralized HTTP mapping for failed application results.
/// </summary>
[ApiController]
public abstract class ApiController : ControllerBase
{
    /// <summary>
    /// Converts a failed result into an RFC 7807 response.
    /// </summary>
    /// <param name="result">The failed result.</param>
    /// <returns>A ProblemDetails response.</returns>
    protected IActionResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be mapped to a problem response.");
        }

        int statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        ProblemDetails problemDetails = new()
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = result.Error.Description,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = result.Error.Code;

        ObjectResult response = new(problemDetails)
        {
            StatusCode = statusCode
        };
        response.ContentTypes.Add("application/problem+json");

        return response;
    }

    /// <summary>
    /// Converts a failed generic result into an RFC 7807 response.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The failed result.</param>
    /// <returns>A ProblemDetails response.</returns>
    protected IActionResult Problem<TValue>(Result<TValue> result)
    {
        return Problem((Result)result);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
            _ => "Internal Server Error"
        };
    }
}
