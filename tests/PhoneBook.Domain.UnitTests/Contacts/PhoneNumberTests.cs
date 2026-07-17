using FluentAssertions;
using PhoneBook.Domain.Contacts;

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
        PhoneNumber.Create(value).Value.Should().Be("+989121234567");
        PhoneNumber.IsValid(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("02112345678")]
    [InlineData("0812345678")]
    [InlineData("+981212345678")]
    [InlineData("+98912345")]
    [InlineData("+9891212345678")]
    [InlineData("abc09121234567")]
    [InlineData("0912A234567")]
    public void Invalid_format_should_throw(string value)
    {
        Action act = () => PhoneNumber.Create(value);

        act.Should().Throw<ArgumentException>();
        PhoneNumber.IsValid(value).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_throw(string? value)
    {
        Action act = () => PhoneNumber.Create(value);

        act.Should().Throw<ArgumentException>();
        PhoneNumber.IsValid(value).Should().BeFalse();
    }

    [Fact]
    public void Equivalent_formats_should_produce_equal_values()
    {
        PhoneNumber.Create("0912-123-4567")
            .Equals(PhoneNumber.Create("+989121234567"))
            .Should()
            .BeTrue();
    }
}
