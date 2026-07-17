using FluentAssertions;
using FluentValidation;
using MediatR;
using PhoneBook.Application.Behaviors;

namespace PhoneBook.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task No_validators_should_call_next()
    {
        ValidationBehavior<TestRequest, string> behavior = new([]);
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        };

        string result = await behavior.Handle(new("value"), next, CancellationToken.None);

        result.Should().Be("handled");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Valid_request_should_call_next()
    {
        InlineValidator<TestRequest> validator = new();
        validator.RuleFor(request => request.Value).NotEmpty();
        ValidationBehavior<TestRequest, string> behavior = new([validator]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

        string result = await behavior.Handle(new("value"), next, CancellationToken.None);

        result.Should().Be("handled");
    }

    [Fact]
    public async Task Invalid_request_should_throw_validation_exception_without_calling_next()
    {
        InlineValidator<TestRequest> validator = new();
        validator.RuleFor(request => request.Value)
            .NotEmpty()
            .WithMessage("Value is required.");
        ValidationBehavior<TestRequest, string> behavior = new([validator]);
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        };

        Func<Task> act = () => behavior.Handle(new(""), next, CancellationToken.None);

        FluentValidation.ValidationException exception =
            (await act.Should().ThrowAsync<FluentValidation.ValidationException>()).Which;
        exception.Errors.Should().ContainSingle();
        exception.Errors.Single().PropertyName.Should().Be(nameof(TestRequest.Value));
        exception.Errors.Single().ErrorMessage.Should().Be("Value is required.");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Behavior_should_pass_cancellation_token_to_validator()
    {
        CancellationToken capturedToken = default;
        InlineValidator<TestRequest> validator = new();
        validator.RuleFor(request => request.Value)
            .MustAsync((_, cancellationToken) =>
            {
                capturedToken = cancellationToken;
                return Task.FromResult(true);
            });
        ValidationBehavior<TestRequest, string> behavior = new([validator]);
        using CancellationTokenSource source = new();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

        await behavior.Handle(new("value"), next, source.Token);

        capturedToken.Equals(source.Token).Should().BeTrue();
    }

    private sealed record TestRequest(string Value) : IRequest<string>;
}
