using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.UnitTests.Contacts;

internal static class ContactTestData
{
    public static readonly Guid ContactGuid = Guid.Parse(
        "11223344-5566-7788-99aa-bbccddeeff00");

    public static readonly DateTime CreatedAtUtc = new(
        2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime UpdatedAtUtc = new(
        2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);

    public static Contact Create(
        Guid? id = null,
        string firstName = "Erfan",
        string lastName = "Ahmadi",
        string phoneNumber = "09121234567",
        string tag = "Coworker",
        DateTime? createdAtUtc = null)
    {
        ContactId contactId = ContactId.Create(id ?? ContactGuid).Value;

        return Contact.Create(
            contactId,
            firstName,
            lastName,
            phoneNumber,
            tag,
            createdAtUtc ?? CreatedAtUtc).Value;
    }
}
