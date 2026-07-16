using FluentAssertions;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Shared;

public class ResultTests
{
    private static readonly Error TestError = new(
        "Result.Test",
        "A test error occurred.",
        ErrorType.Failure);

    [Fact]
    public void Success_should_set_IsSuccess_to_true()
    {
        Result result = Result.Success();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_should_set_IsFailure_to_false()
    {
        Result result = Result.Success();

        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Success_should_expose_Error_None()
    {
        Result result = Result.Success();

        result.Error.Equals(Error.None).Should().BeTrue();
    }

    [Fact]
    public void Failure_should_set_IsSuccess_to_false()
    {
        Result result = Result.Failure(TestError);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_should_set_IsFailure_to_true()
    {
        Result result = Result.Failure(TestError);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_should_expose_supplied_error()
    {
        Result result = Result.Failure(TestError);

        result.Error.Equals(TestError).Should().BeTrue();
    }

    [Fact]
    public void Failure_with_Error_None_should_be_rejected()
    {
        Action act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_with_null_error_should_be_rejected()
    {
        Action act = () => Result.Failure(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Failure_with_error_equal_to_Error_None_should_be_rejected()
    {
        Error equivalentNone = new(
            "Error.None",
            string.Empty,
            ErrorType.Failure);

        Action act = () => Result.Failure(equivalentNone);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Successful_result_with_error_should_be_rejected()
    {
        Action act = () => new TestResult(true, TestError);

        act.Should().Throw<InvalidOperationException>();
    }

    private class TestResult : Result
    {
        public TestResult(bool isSuccess, Error error)
            : base(isSuccess, error)
        {
        }
    }
}
