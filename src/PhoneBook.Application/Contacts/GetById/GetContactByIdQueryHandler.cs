using MediatR;
using MapsterMapper;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetById;

public class GetContactByIdQueryHandler
    : IRequestHandler<GetContactByIdQuery, ContactResponse>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public GetContactByIdQueryHandler(
        IContactRepository contactRepository,
        IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
    }

    public async Task<ContactResponse> Handle(
        GetContactByIdQuery request,
        CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(
            new ContactId(request.ContactId),
            cancellationToken);

        if (contact is null)
        {
            throw new NotFoundException(
                "Contact.NotFound",
                "Contact was not found.");
        }

        return _mapper.Map<ContactResponse>(contact);
    }
}
