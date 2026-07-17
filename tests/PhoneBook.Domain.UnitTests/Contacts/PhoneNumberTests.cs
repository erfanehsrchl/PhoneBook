using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("09121234567")]
    [InlineData("+989121234567")]
    [InlineData("989121234567")]
    [InlineData("00989121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧")]
    [InlineData("0912 123 4567")]
    [InlineData("0912-123-4567")]
    [InlineData("(0912) 123 4567")]
    public void Supported_format_should_normalize_to_canonical_value(string value)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+989121234567");
    }

    [Theory]
    [InlineData("02112345678")]
    [InlineData("0812345678")]
    [InlineData("+981212345678")]
    [InlineData("+98912345")]
    [InlineData("+9891212345678")]
    [InlineData("abc09121234567")]
    [InlineData("0912A234567")]
    public void Invalid_format_should_fail(string value)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.PhoneNumber.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_fail(string? value)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.PhoneNumber.Required");
    }

    [Fact]
    public void Equivalent_formats_should_produce_equal_values()
    {
        PhoneNumber local = PhoneNumber.Create("0912-123-4567").Value;
        PhoneNumber international = PhoneNumber.Create("+989121234567").Value;

        local.Equals(international).Should().BeTrue();
    }
}
