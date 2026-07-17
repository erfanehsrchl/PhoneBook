namespace PhoneBook.Api.Contracts.Contacts;

/// <summary>
/// Represents the input required to create a contact.
/// </summary>
public record CreateContactRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Tag);
