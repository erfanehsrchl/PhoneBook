using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class LastNameTests
{
    [Fact]
    public void Valid_value_should_succeed()
    {
        Result<LastName> result = LastName.Create("Ahmadi");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Ahmadi");
    }

    [Fact]
    public void Surrounding_whitespace_should_be_trimmed()
    {
        Result<LastName> result = LastName.Create("  Ahmadi  ");

        result.Value.Value.Should().Be("Ahmadi");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_fail(string? value)
    {
        Result<LastName> result = LastName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.LastName.Required");
    }

    [Fact]
    public void Value_longer_than_100_characters_should_fail()
    {
        Result<LastName> result = LastName.Create(new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.LastName.TooLong");
    }

    [Theory]
    [InlineData("احمدی")]
    [InlineData("García")]
    public void Unicode_name_should_succeed(string value)
    {
        Result<LastName> result = LastName.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }
}
