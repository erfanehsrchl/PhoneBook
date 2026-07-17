using FluentAssertions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Domain.UnitTests.Contacts;

public class ContactTests
{
    private static readonly DateTime CreatedAtUtc = new(
        2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc = new(
        2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    [Fact]
    public void Valid_input_should_create_contact_with_normalized_values()
    {
        ContactId id = new(Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"));

        Contact contact = Contact.Create(
            id,
            "  Erfan  ",
            "  Ahmadi  ",
            "0912-123-4567",
            "  Coworker  ",
            CreatedAtUtc);

        contact.Id.Equals(id).Should().BeTrue();
        contact.FirstName.Value.Should().Be("Erfan");
        contact.LastName.Value.Should().Be("Ahmadi");
        contact.PhoneNumber.Value.Should().Be("+989121234567");
        contact.Tag.Value.Should().Be("Coworker");
        contact.CreatedAtUtc.Should().Be(CreatedAtUtc);
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Ahmadi", "09121234567", "Coworker")]
    [InlineData("Erfan", "", "09121234567", "Coworker")]
    [InlineData("Erfan", "Ahmadi", "invalid", "Coworker")]
    [InlineData("Erfan", "Ahmadi", "09121234567", "")]
    public void Invalid_input_should_throw(
        string firstName,
        string lastName,
        string phoneNumber,
        string tag)
    {
        Action act = () => CreateContact(firstName, lastName, phoneNumber, tag);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Non_utc_creation_timestamp_should_throw(DateTimeKind kind)
    {
        Action act = () => CreateContact(
            createdAtUtc: DateTime.SpecifyKind(CreatedAtUtc, kind));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Valid_update_should_change_editable_values_and_timestamp_only()
    {
        Contact contact = CreateContact();
        ContactId originalId = contact.Id;

        contact.Update("Sara", "Karimi", "0935 765 4321", "Friend", UpdatedAtUtc);

        contact.FirstName.Value.Should().Be("Sara");
        contact.LastName.Value.Should().Be("Karimi");
        contact.PhoneNumber.Value.Should().Be("+989357654321");
        contact.Tag.Value.Should().Be("Friend");
        contact.UpdatedAtUtc.Should().Be(UpdatedAtUtc);
        contact.Id.Equals(originalId).Should().BeTrue();
        contact.CreatedAtUtc.Should().Be(CreatedAtUtc);
    }

    [Fact]
    public void Invalid_update_should_throw_without_partial_mutation()
    {
        Contact contact = CreateContact();

        Action act = () => contact.Update(
            "Changed",
            "Changed",
            "invalid",
            "Changed",
            UpdatedAtUtc);

        act.Should().Throw<ArgumentException>();
        contact.FirstName.Value.Should().Be("Erfan");
        contact.PhoneNumber.Value.Should().Be("+989121234567");
        contact.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Invalid_update_timestamp_should_throw_without_mutation()
    {
        Contact contact = CreateContact();

        Action act = () => contact.Update(
            "Changed",
            "Changed",
            "09357654321",
            "Changed",
            DateTime.SpecifyKind(UpdatedAtUtc, DateTimeKind.Local));

        act.Should().Throw<ArgumentException>();
        contact.FirstName.Value.Should().Be("Erfan");
        contact.UpdatedAtUtc.Should().BeNull();
    }

    private static Contact CreateContact(
        string? firstName = "Erfan",
        string? lastName = "Ahmadi",
        string? phoneNumber = "09121234567",
        string? tag = "Coworker",
        DateTime? createdAtUtc = null)
    {
        return Contact.Create(
            new ContactId(Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00")),
            firstName,
            lastName,
            phoneNumber,
            tag,
            createdAtUtc ?? CreatedAtUtc);
    }
}
