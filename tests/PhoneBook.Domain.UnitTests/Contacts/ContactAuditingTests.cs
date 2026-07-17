using FluentAssertions;
using PhoneBook.Domain.Abstractions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class ContactAuditingTests
{
    private static readonly DateTime CreatedAtUtc = new(
        2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc = new(
        2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Contact_should_implement_IAuditableEntity()
    {
        Contact contact = CreateContact();

        (contact is IAuditableEntity).Should().BeTrue();
    }

    [Fact]
    public void Create_should_store_supplied_creation_timestamp_and_clear_update_timestamp()
    {
        Contact contact = CreateContact();

        contact.CreatedAtUtc.Should().Be(CreatedAtUtc);
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_should_reject_non_utc_creation_timestamp(DateTimeKind kind)
    {
        Result<Contact> result = CreateContactResult(
            DateTime.SpecifyKind(CreatedAtUtc, kind));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
    }

    [Fact]
    public void Update_should_store_supplied_update_timestamp()
    {
        Contact contact = CreateContact();

        Result result = contact.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            UpdatedAtUtc);

        result.IsSuccess.Should().BeTrue();
        contact.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Update_should_reject_non_utc_timestamp(DateTimeKind kind)
    {
        Contact contact = CreateContact();

        Result result = contact.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            DateTime.SpecifyKind(UpdatedAtUtc, kind));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Update_should_reject_timestamp_earlier_than_creation_and_preserve_state()
    {
        Contact contact = CreateContact();

        Result result = contact.Update(
            "Changed",
            "Changed",
            "09357654321",
            "Changed",
            CreatedAtUtc.AddTicks(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.BeforeCreated");
        contact.FirstName.Value.Should().Be("Erfan");
        contact.LastName.Value.Should().Be("Ahmadi");
        contact.PhoneNumber.Value.Should().Be("+989121234567");
        contact.Tag.Value.Should().Be("Coworker");
        contact.CreatedAtUtc.Should().Be(CreatedAtUtc);
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_should_preserve_identity_and_audit_timestamps()
    {
        Contact original = CreateContact();
        original.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            UpdatedAtUtc);

        Result<Contact> result = Contact.Rehydrate(
            original.Id,
            original.FirstName,
            original.LastName,
            original.PhoneNumber,
            original.Tag,
            original.CreatedAtUtc,
            original.UpdatedAtUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Equals(original.Id).Should().BeTrue();
        result.Value.CreatedAtUtc.Should().Be(CreatedAtUtc);
        result.Value.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
    }

    [Fact]
    public void Rehydrate_should_reject_non_utc_creation_timestamp()
    {
        Contact contact = CreateContact();

        Result<Contact> result = Contact.Rehydrate(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Tag,
            DateTime.SpecifyKind(CreatedAtUtc, DateTimeKind.Unspecified),
            null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
    }

    [Fact]
    public void Rehydrate_should_reject_non_utc_update_timestamp()
    {
        Contact contact = CreateContact();

        Result<Contact> result = Contact.Rehydrate(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Tag,
            CreatedAtUtc,
            DateTime.SpecifyKind(UpdatedAtUtc, DateTimeKind.Local));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.MustBeUtc");
    }

    [Fact]
    public void Rehydrate_should_reject_update_timestamp_earlier_than_creation()
    {
        Contact contact = CreateContact();

        Result<Contact> result = Contact.Rehydrate(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Tag,
            CreatedAtUtc,
            CreatedAtUtc.AddMinutes(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.BeforeCreated");
    }

    private static Contact CreateContact()
    {
        return CreateContactResult(CreatedAtUtc).Value;
    }

    private static Result<Contact> CreateContactResult(DateTime createdAtUtc)
    {
        ContactId id = ContactId.Create(
            Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00")).Value;

        return Contact.Create(
            id,
            "Erfan",
            "Ahmadi",
            "09121234567",
            "Coworker",
            createdAtUtc);
    }
}
