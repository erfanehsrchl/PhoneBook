using MediatR;
using MapsterMapper;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.Update;

public class UpdateContactCommandHandler
    : IRequestHandler<UpdateContactCommand, ContactResponse>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public UpdateContactCommandHandler(
        IContactRepository contactRepository,
        IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
    }

    public async Task<ContactResponse> Handle(
        UpdateContactCommand request,
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

        var now = DateTime.UtcNow;
        contact.Update(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Tag,
            now);

        await _contactRepository.UpdateAsync(contact, cancellationToken);
        return _mapper.Map<ContactResponse>(contact);
    }
}
