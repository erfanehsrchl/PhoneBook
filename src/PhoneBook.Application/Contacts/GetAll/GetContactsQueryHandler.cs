using MediatR;
using MapsterMapper;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetAll;

public class GetContactsQueryHandler
    : IRequestHandler<GetContactsQuery, PagedData<ContactResponse>>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public GetContactsQueryHandler(
        IContactRepository contactRepository,
        IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
    }

    public async Task<PagedData<ContactResponse>> Handle(
        GetContactsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _contactRepository.GetAllAsync(
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
