using FluentAssertions;
using PhoneBook.Domain.Abstractions;
using PhoneBook.Domain.Contacts;

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
        (CreateContact() is IAuditableEntity).Should().BeTrue();
    }

    [Fact]
    public void Create_should_store_creation_timestamp_and_clear_update_timestamp()
    {
        Contact contact = CreateContact();

        contact.CreatedAtUtc.Should().Be(CreatedAtUtc);
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Update_should_store_supplied_update_timestamp()
    {
        Contact contact = CreateContact();

        contact.Update("Sara", "Karimi", "09357654321", "Friend", UpdatedAtUtc);

        contact.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
    }

    [Fact]
    public void Update_should_reject_timestamp_earlier_than_creation()
    {
        Contact contact = CreateContact();

        Action act = () => contact.Update(
            "Changed",
            "Changed",
            "09357654321",
            "Changed",
            CreatedAtUtc.AddTicks(-1));

        act.Should().Throw<ArgumentException>();
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_should_preserve_identity_and_audit_timestamps()
    {
        Contact original = CreateContact();
        original.Update("Sara", "Karimi", "09357654321", "Friend", UpdatedAtUtc);

        Contact rehydrated = Contact.Rehydrate(
            original.Id,
            original.FirstName,
            original.LastName,
            original.PhoneNumber,
            original.Tag,
            original.CreatedAtUtc,
            original.UpdatedAtUtc);

        rehydrated.Id.Equals(original.Id).Should().BeTrue();
        rehydrated.CreatedAtUtc.Should().Be(CreatedAtUtc);
        rehydrated.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Rehydrate_should_reject_non_utc_creation_timestamp(DateTimeKind kind)
    {
        Contact contact = CreateContact();

        Action act = () => Contact.Rehydrate(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Tag,
            DateTime.SpecifyKind(CreatedAtUtc, kind),
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rehydrate_should_reject_update_timestamp_earlier_than_creation()
    {
        Contact contact = CreateContact();

        Action act = () => Contact.Rehydrate(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Tag,
            CreatedAtUtc,
            CreatedAtUtc.AddMinutes(-1));

        act.Should().Throw<ArgumentException>();
    }

    private static Contact CreateContact()
    {
        return Contact.Create(
            new ContactId(Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00")),
            "Erfan",
            "Ahmadi",
            "09121234567",
            "Coworker",
            CreatedAtUtc);
    }
}
