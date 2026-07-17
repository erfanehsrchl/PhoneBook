using MediatR;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Update;

public sealed record UpdateContactCommand(
    Guid ContactId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Tag) : IRequest<ContactResponse>;
