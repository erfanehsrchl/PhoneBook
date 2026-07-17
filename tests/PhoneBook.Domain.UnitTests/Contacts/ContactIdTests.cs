using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class ContactIdTests
{
    [Fact]
    public void New_should_create_non_empty_id()
    {
        ContactId id = ContactId.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_should_accept_non_empty_guid()
    {
        Guid value = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");

        Result<ContactId> result = ContactId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_should_reject_empty_guid()
    {
        Result<ContactId> result = ContactId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Id.Empty");
    }

    [Fact]
    public void Ids_with_same_guid_should_be_equal()
    {
        Guid value = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");
        ContactId first = ContactId.Create(value).Value;
        ContactId second = ContactId.Create(value).Value;

        first.Equals(second).Should().BeTrue();
    }
}
