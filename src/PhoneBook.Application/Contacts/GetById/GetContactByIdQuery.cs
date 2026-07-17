using MediatR;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetById;

public sealed record GetContactByIdQuery(Guid ContactId) : IRequest<ContactResponse>;
