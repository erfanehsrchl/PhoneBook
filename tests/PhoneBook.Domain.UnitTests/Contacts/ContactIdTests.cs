using FluentAssertions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class ContactIdTests
{
    [Fact]
    public void New_should_create_non_empty_id()
    {
        ContactId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_should_accept_non_empty_guid()
    {
        Guid value = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");

        ContactId id = new(value);

        id.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_should_reject_empty_guid()
    {
        Action act = () => _ = new ContactId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ids_with_same_guid_should_be_equal()
    {
        Guid value = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");

        new ContactId(value).Equals(new ContactId(value)).Should().BeTrue();
    }
}
