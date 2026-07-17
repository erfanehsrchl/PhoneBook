using MediatR;
using MapsterMapper;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.Create;

public class CreateContactCommandHandler
    : IRequestHandler<CreateContactCommand, ContactResponse>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public CreateContactCommandHandler(
        IContactRepository contactRepository,
        IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
    }

    public async Task<ContactResponse> Handle(
        CreateContactCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var contact = Contact.Create(
            ContactId.New(),
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Tag,
            now);

        await _contactRepository.AddAsync(contact, cancellationToken);
        return _mapper.Map<ContactResponse>(contact);
    }
}
