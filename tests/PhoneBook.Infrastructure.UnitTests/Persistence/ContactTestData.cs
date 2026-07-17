using PhoneBook.Domain.Contacts;

namespace PhoneBook.Infrastructure.UnitTests.Persistence;

internal static class ContactTestData
{
    public static readonly DateTime CreatedAtUtc = new(
        2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime UpdatedAtUtc = new(
        2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);

    public static Contact CreateContact(
        string id,
        string phoneNumber = "09121234567",
        string tag = "Coworker",
        DateTime? createdAtUtc = null,
        string firstName = "Erfan",
        string lastName = "Ahmadi")
    {
        ContactId contactId = new(Guid.Parse(id));

        return Contact.Create(
            contactId,
            firstName,
            lastName,
            phoneNumber,
            tag,
            createdAtUtc ?? CreatedAtUtc);
    }
}
