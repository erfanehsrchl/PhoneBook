using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class TagTests
{
    [Fact]
    public void Valid_tag_should_succeed()
    {
        Result<Tag> result = Tag.Create("Coworker");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Coworker");
    }

    [Fact]
    public void Surrounding_whitespace_should_be_trimmed()
    {
        Result<Tag> result = Tag.Create("  Coworker  ");

        result.Value.Value.Should().Be("Coworker");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_value_should_fail(string? value)
    {
        Result<Tag> result = Tag.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Tag.Required");
    }

    [Fact]
    public void Value_longer_than_100_characters_should_fail()
    {
        Result<Tag> result = Tag.Create(new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Tag.TooLong");
    }

    [Fact]
    public void Values_should_be_equal_ignoring_case_and_whitespace()
    {
        Tag first = Tag.Create("Coworker").Value;
        Tag second = Tag.Create(" COWORKER ").Value;

        first.Equals(second).Should().BeTrue();
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equal_values_should_produce_equal_hash_codes()
    {
        Tag first = Tag.Create("Coworker").Value;
        Tag second = Tag.Create("coworker").Value;

        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Display_value_should_preserve_casing()
    {
        Tag tag = Tag.Create("  Close Friend  ").Value;

        tag.Value.Should().Be("Close Friend");
    }
}
