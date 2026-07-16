using FluentAssertions;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Shared;

public class ResultOfTTests
{
    private static readonly Error TestError = new(
        "ResultOfT.Test",
        "A test error occurred.",
        ErrorType.Validation);

    [Fact]
    public void Success_should_expose_supplied_value()
    {
        Result<string> result = Result<string>.Success("value");

        result.Value.Should().Be("value");
    }

    [Fact]
    public void Success_should_expose_Error_None()
    {
        Result<string> result = Result<string>.Success("value");

        result.Error.Equals(Error.None).Should().BeTrue();
    }

    [Fact]
    public void Failure_should_expose_supplied_error()
    {
        Result<string> result = Result<string>.Failure(TestError);

        result.Error.Equals(TestError).Should().BeTrue();
    }

    [Fact]
    public void Accessing_Value_on_failure_should_throw_InvalidOperationException()
    {
        Result<string> result = Result<string>.Failure(TestError);

        Func<string> act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The value of a failed result cannot be accessed.");
    }

    [Fact]
    public void Success_with_null_reference_value_should_be_rejected()
    {
        Action act = () => Result<string>.Success(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Implicit_conversion_from_value_should_create_success()
    {
        Result<string> result = "value";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
        result.Error.Equals(Error.None).Should().BeTrue();
    }

    [Fact]
    public void Implicit_conversion_from_Error_should_create_failure()
    {
        Result<string> result = TestError;

        result.IsFailure.Should().BeTrue();
        result.Error.Equals(TestError).Should().BeTrue();
    }

    [Fact]
    public void IsFailure_should_always_be_inverse_of_IsSuccess()
    {
        Result<string> success = Result<string>.Success("value");
        Result<string> failure = Result<string>.Failure(TestError);

        success.IsFailure.Should().Be(!success.IsSuccess);
        failure.IsFailure.Should().Be(!failure.IsSuccess);
    }
}
