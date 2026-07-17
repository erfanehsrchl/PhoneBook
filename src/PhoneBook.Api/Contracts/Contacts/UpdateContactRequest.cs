namespace PhoneBook.Api.Contracts.Contacts;

/// <summary>
/// Represents the editable input for an existing contact.
/// </summary>
public record UpdateContactRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Tag);
