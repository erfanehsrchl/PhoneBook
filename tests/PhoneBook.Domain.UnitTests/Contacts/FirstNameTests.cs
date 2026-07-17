using FluentAssertions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class FirstNameTests
{
    [Fact]
    public void Valid_value_should_be_trimmed()
    {
        FirstName.Create("  Erfan  ").Value.Should().Be("Erfan");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_throw(string? value)
    {
        Action act = () => FirstName.Create(value);

        act.Should().Throw<ArgumentException>().WithMessage("First name is required.*");
    }

    [Fact]
    public void Value_longer_than_100_characters_should_throw()
    {
        Action act = () => FirstName.Create(new string('a', 101));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("عرفان")]
    [InlineData("Łukasz")]
    public void Unicode_name_should_be_preserved(string value)
    {
        FirstName.Create(value).Value.Should().Be(value);
    }
}
