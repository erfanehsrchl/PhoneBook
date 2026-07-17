using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Update;

public record UpdateContactCommand(
    Guid ContactId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Tag) : ICommand<ContactResponse>;
