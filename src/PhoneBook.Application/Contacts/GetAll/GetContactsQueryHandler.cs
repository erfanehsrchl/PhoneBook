using MediatR;
using Mapster;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetAll;

public class GetContactsQueryHandler
    : IRequestHandler<GetContactsQuery, PagedData<ContactResponse>>
{
    private readonly IContactRepository _contactRepository;

    public GetContactsQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<PagedData<ContactResponse>> Handle(
        GetContactsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _contactRepository.GetAllAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return page.Adapt<PagedData<ContactResponse>>();
    }
}
