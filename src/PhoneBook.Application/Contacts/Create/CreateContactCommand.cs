using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Create;

public record CreateContactCommand(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Tag) : ICommand<ContactResponse>;
