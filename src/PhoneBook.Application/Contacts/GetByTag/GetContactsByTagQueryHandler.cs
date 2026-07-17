using MediatR;
using Mapster;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetByTag;

public class GetContactsByTagQueryHandler
    : IRequestHandler<GetContactsByTagQuery, PagedData<ContactResponse>>
{
    private readonly IContactRepository _contactRepository;

    public GetContactsByTagQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<PagedData<ContactResponse>> Handle(
        GetContactsByTagQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _contactRepository.GetByTagAsync(
            Tag.Create(request.Tag),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return page.Adapt<PagedData<ContactResponse>>();
    }
}
