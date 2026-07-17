using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetByTag;

public record GetContactsByTagQuery(string? Tag)
    : IQuery<IReadOnlyCollection<ContactResponse>>;
