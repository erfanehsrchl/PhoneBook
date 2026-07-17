using MediatR;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetAll;

public sealed record GetContactsQuery(int PageNumber, int PageSize)
    : IRequest<PagedData<ContactResponse>>;
