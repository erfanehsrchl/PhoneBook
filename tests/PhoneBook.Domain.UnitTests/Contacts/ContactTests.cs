using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class ContactTests
{
    private static readonly DateTime CreatedAtUtc = new(
        2026,
        1,
        2,
        3,
        4,
        5,
        DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc = new(
        2026,
        2,
        3,
        4,
        5,
        6,
        DateTimeKind.Utc);

    [Fact]
    public void Valid_input_should_create_contact_with_normalized_values()
    {
        ContactId id = ContactId.Create(Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00")).Value;

        Result<Contact> result = Contact.Create(
            id,
            "  Erfan  ",
            "  Ahmadi  ",
            "0912-123-4567",
            "  Coworker  ",
            CreatedAtUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Equals(id).Should().BeTrue();
        result.Value.FirstName.Value.Should().Be("Erfan");
        result.Value.LastName.Value.Should().Be("Ahmadi");
        result.Value.PhoneNumber.Value.Should().Be("+989121234567");
        result.Value.Tag.Value.Should().Be("Coworker");
        result.Value.CreatedAtUtc.Should().Be(CreatedAtUtc);
        result.Value.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Invalid_first_name_should_return_failure()
    {
        Result<Contact> result = CreateContact(firstName: " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.FirstName.Required");
    }

    [Fact]
    public void Invalid_last_name_should_return_failure()
    {
        Result<Contact> result = CreateContact(lastName: " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.LastName.Required");
    }

    [Fact]
    public void Invalid_phone_number_should_return_failure()
    {
        Result<Contact> result = CreateContact(phoneNumber: "02112345678");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.PhoneNumber.Invalid");
    }

    [Fact]
    public void Invalid_tag_should_return_failure()
    {
        Result<Contact> result = CreateContact(tag: " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Tag.Required");
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Non_utc_creation_timestamp_should_return_failure(DateTimeKind kind)
    {
        DateTime timestamp = DateTime.SpecifyKind(CreatedAtUtc, kind);

        Result<Contact> result = CreateContact(createdAtUtc: timestamp);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
    }

    [Fact]
    public void Empty_contact_id_should_return_failure()
    {
        Result<Contact> result = Contact.Create(
            default,
            "Erfan",
            "Ahmadi",
            "09121234567",
            "Coworker",
            CreatedAtUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Id.Empty");
    }

    [Fact]
    public void Valid_update_should_change_editable_values_and_timestamp_only()
    {
        Contact contact = CreateValidContact();
        ContactId originalId = contact.Id;
        DateTime originalCreatedAtUtc = contact.CreatedAtUtc;

        Result result = contact.Update(
            "Sara",
            "Karimi",
            "0935 765 4321",
            "Friend",
            UpdatedAtUtc);

        result.IsSuccess.Should().BeTrue();
        contact.FirstName.Value.Should().Be("Sara");
        contact.LastName.Value.Should().Be("Karimi");
        contact.PhoneNumber.Value.Should().Be("+989357654321");
        contact.Tag.Value.Should().Be("Friend");
        contact.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
        contact.Id.Equals(originalId).Should().BeTrue();
        contact.CreatedAtUtc.Should().Be(originalCreatedAtUtc);
    }

    [Fact]
    public void Invalid_update_should_not_partially_mutate_contact()
    {
        Contact contact = CreateValidContact();
        ContactId originalId = contact.Id;
        FirstName originalFirstName = contact.FirstName;
        LastName originalLastName = contact.LastName;
        PhoneNumber originalPhoneNumber = contact.PhoneNumber;
        Tag originalTag = contact.Tag;
        DateTime originalCreatedAtUtc = contact.CreatedAtUtc;
        DateTime? originalUpdatedAtUtc = contact.UpdatedAtUtc;

        Result result = contact.Update(
            "Changed",
            "Changed",
            "invalid",
            "Changed",
            UpdatedAtUtc);

        result.IsFailure.Should().BeTrue();
        contact.Id.Equals(originalId).Should().BeTrue();
        contact.FirstName.Equals(originalFirstName).Should().BeTrue();
        contact.LastName.Equals(originalLastName).Should().BeTrue();
        contact.PhoneNumber.Equals(originalPhoneNumber).Should().BeTrue();
        contact.Tag.Equals(originalTag).Should().BeTrue();
        contact.CreatedAtUtc.Should().Be(originalCreatedAtUtc);
        contact.UpdatedAtUtc.Should().Be(originalUpdatedAtUtc);
    }

    [Fact]
    public void Non_utc_update_timestamp_should_return_failure_without_mutation()
    {
        Contact contact = CreateValidContact();

        Result result = contact.Update(
            "Changed",
            "Changed",
            "09357654321",
            "Changed",
            DateTime.SpecifyKind(UpdatedAtUtc, DateTimeKind.Local));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
        contact.FirstName.Value.Should().Be("Erfan");
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Update_should_normalize_equivalent_phone_number_format()
    {
        Contact contact = CreateValidContact();

        Result result = contact.Update(
            "Erfan",
            "Ahmadi",
            "00989121234567",
            "Coworker",
            UpdatedAtUtc);

        result.IsSuccess.Should().BeTrue();
        contact.PhoneNumber.Value.Should().Be("+989121234567");
    }

    private static Contact CreateValidContact()
    {
        return CreateContact().Value;
    }

    private static Result<Contact> CreateContact(
        string? firstName = "Erfan",
        string? lastName = "Ahmadi",
        string? phoneNumber = "09121234567",
        string? tag = "Coworker",
        DateTime? createdAtUtc = null)
    {
        ContactId id = ContactId.Create(Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00")).Value;

        return Contact.Create(
            id,
            firstName,
            lastName,
            phoneNumber,
            tag,
            createdAtUtc ?? CreatedAtUtc);
    }
}
