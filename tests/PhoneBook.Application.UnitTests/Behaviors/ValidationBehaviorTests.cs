using FluentAssertions;
using FluentValidation;
using MediatR;
using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Behaviors;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task No_validators_should_call_next()
    {
        ValidationBehavior<TestCommand, Result> behavior = new([]);
        bool nextCalled = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        Result result = await behavior.Handle(
            new("value"),
            next,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Valid_request_should_call_next()
    {
        InlineValidator<TestCommand> validator = new();
        validator.RuleFor(request => request.Value).NotEmpty();
        ValidationBehavior<TestCommand, Result> behavior = new([validator]);
        bool nextCalled = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        Result result = await behavior.Handle(
            new("value"),
            next,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_request_should_return_failed_Result_without_calling_next()
    {
        InlineValidator<TestCommand> validator = new();
        validator.RuleFor(request => request.Value)
            .NotEmpty()
            .WithMessage("Value is required.");
        ValidationBehavior<TestCommand, Result> behavior = new([validator]);
        bool nextCalled = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        Result result = await behavior.Handle(
            new(""),
            next,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Value");
        result.Error.Description.Should().Be("Value is required.");
        result.Error.Type.Should().Be(ErrorType.Validation);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_generic_request_should_return_failed_Result_of_T()
    {
        InlineValidator<TestQuery> validator = new();
        validator.RuleFor(request => request.Value).NotEmpty();
        ValidationBehavior<TestQuery, Result<string>> behavior = new([validator]);
        bool nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("handled"));
        };

        Result<string> result = await behavior.Handle(
            new(""),
            next,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Value");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Behavior_should_pass_cancellation_token_to_validator()
    {
        CancellationToken capturedToken = default;
        InlineValidator<TestCommand> validator = new();
        validator.RuleFor(request => request.Value)
            .MustAsync((_, cancellationToken) =>
            {
                capturedToken = cancellationToken;
                return Task.FromResult(false);
            });
        ValidationBehavior<TestCommand, Result> behavior = new([validator]);
        using CancellationTokenSource source = new();
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        await behavior.Handle(new("value"), next, source.Token);

        capturedToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task Multiple_failures_should_deterministically_return_first_deduplicated_error()
    {
        InlineValidator<TestCommand> firstValidator = new();
        firstValidator.RuleFor(request => request.Value)
            .NotEmpty()
            .WithMessage("Value is required.");
        InlineValidator<TestCommand> duplicateValidator = new();
        duplicateValidator.RuleFor(request => request.Value)
            .NotEmpty()
            .WithMessage("Value is required.");
        ValidationBehavior<TestCommand, Result> behavior = new(
            [firstValidator, duplicateValidator]);
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        Result result = await behavior.Handle(
            new(""),
            next,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Value");
        result.Error.Description.Should().Be("Value is required.");
    }

    private record TestCommand(string Value) : ICommand;

    private record TestQuery(string Value) : IQuery<string>;
}
