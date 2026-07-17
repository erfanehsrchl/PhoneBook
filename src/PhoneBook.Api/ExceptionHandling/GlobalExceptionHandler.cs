using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using PhoneBook.Api.Contracts;
using PhoneBook.Application.Common.Exceptions;

namespace PhoneBook.Api.ExceptionHandling;

/// <summary>
/// Converts application and unexpected exceptions into safe API error responses.
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
        if (exception is ValidationException validationException)
        {
            await WriteValidationResponseAsync(
                httpContext,
                validationException,
                cancellationToken);
            return true;
        }

        int statusCode;
        string message;
        string errorCode;

        if (exception is ApplicationExceptionBase applicationException)
        {
            statusCode = applicationException switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError
            };
            message = applicationException.Message;
            errorCode = applicationException.Code;
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            message = "An unexpected error occurred.";
            errorCode = "Server.UnexpectedError";
            _logger.LogError(
                exception,
                "An unexpected exception occurred while processing {RequestPath}.",
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse(statusCode, message, errorCode),
            cancellationToken);
        return true;
    }

    private static async Task WriteValidationResponseAsync(
        HttpContext httpContext,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = exception.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Distinct()
                    .ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ValidationApiResponse(
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                errors),
            cancellationToken);
    }
}
