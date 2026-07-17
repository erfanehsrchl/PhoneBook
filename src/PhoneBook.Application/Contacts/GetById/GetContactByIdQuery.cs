using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetById;

public record GetContactByIdQuery(Guid ContactId) : IQuery<ContactResponse>;
