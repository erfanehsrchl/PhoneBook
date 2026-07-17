using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class FirstNameTests
{
    [Fact]
    public void Valid_value_should_succeed()
    {
        Result<FirstName> result = FirstName.Create("Erfan");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Erfan");
    }

    [Fact]
    public void Surrounding_whitespace_should_be_trimmed()
    {
        Result<FirstName> result = FirstName.Create("  Erfan  ");

        result.Value.Value.Should().Be("Erfan");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_fail(string? value)
    {
        Result<FirstName> result = FirstName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.FirstName.Required");
    }

    [Fact]
    public void Value_longer_than_100_characters_should_fail()
    {
        Result<FirstName> result = FirstName.Create(new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.FirstName.TooLong");
    }

    [Theory]
    [InlineData("عرفان")]
    [InlineData("Łukasz")]
    public void Unicode_name_should_succeed(string value)
    {
        Result<FirstName> result = FirstName.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }
}
