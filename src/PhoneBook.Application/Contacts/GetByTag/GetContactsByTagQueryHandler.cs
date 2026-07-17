using MediatR;
using MapsterMapper;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetByTag;

public class GetContactsByTagQueryHandler
    : IRequestHandler<GetContactsByTagQuery, PagedData<ContactResponse>>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public GetContactsByTagQueryHandler(
        IContactRepository contactRepository,
        IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
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

        return new PagedData<ContactResponse>(
            _mapper.Map<IReadOnlyCollection<ContactResponse>>(page.Items),
            page.PageNumber,
            page.PageSize,
            page.TotalCount);
    }
}
