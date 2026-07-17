using MediatR;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetByTag;

public sealed record GetContactsByTagQuery(string? Tag, int PageNumber, int PageSize)
    : IRequest<PagedData<ContactResponse>>;
