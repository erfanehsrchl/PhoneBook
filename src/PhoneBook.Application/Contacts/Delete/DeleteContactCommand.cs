using MediatR;

namespace PhoneBook.Application.Contacts.Delete;

public sealed record DeleteContactCommand(Guid ContactId) : IRequest;
