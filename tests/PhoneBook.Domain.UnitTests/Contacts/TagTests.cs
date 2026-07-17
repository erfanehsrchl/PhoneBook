using FluentAssertions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class TagTests
{
    [Fact]
    public void Valid_tag_should_be_trimmed_and_preserve_casing()
    {
        Tag.Create("  Close Friend  ").Value.Should().Be("Close Friend");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_throw(string? value)
    {
        Action act = () => Tag.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Value_longer_than_100_characters_should_throw()
    {
        Action act = () => Tag.Create(new string('a', 101));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Values_should_be_equal_ignoring_case_and_whitespace()
    {
        Tag first = Tag.Create("Coworker");
        Tag second = Tag.Create(" COWORKER ");

        first.Equals(second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
