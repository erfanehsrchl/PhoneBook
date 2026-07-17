using MediatR;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Create;

public sealed record CreateContactCommand(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Tag) : IRequest<ContactResponse>;
