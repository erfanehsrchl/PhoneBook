using FluentAssertions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class LastNameTests
{
    [Fact]
    public void Valid_value_should_be_trimmed()
    {
        LastName.Create("  Ahmadi  ").Value.Should().Be("Ahmadi");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_throw(string? value)
    {
        Action act = () => LastName.Create(value);

        act.Should().Throw<ArgumentException>().WithMessage("Last name is required.*");
    }

    [Fact]
    public void Value_longer_than_100_characters_should_throw()
    {
        Action act = () => LastName.Create(new string('a', 101));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("احمدی")]
    [InlineData("García")]
    public void Unicode_name_should_be_preserved(string value)
    {
        LastName.Create(value).Value.Should().Be(value);
    }
}
