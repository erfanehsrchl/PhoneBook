using FluentValidation;
using FluentValidation.Results;
using MediatR;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReadOnlyCollection<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToArray();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);
        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        ValidationFailure? firstFailure = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .DistinctBy(failure => new { failure.PropertyName, failure.ErrorMessage })
            .FirstOrDefault();

        if (firstFailure is null)
        {
            return await next(cancellationToken);
        }

        Error error = new(
            $"Validation.{firstFailure.PropertyName}",
            firstFailure.ErrorMessage,
            ErrorType.Validation);

        return ValidationResultFactory.Create<TResponse>(error);
    }
}
