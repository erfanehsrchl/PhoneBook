namespace PhoneBook.Application.Contacts.Common;

public record ContactResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Tag,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
