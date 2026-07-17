using MediatR;
using Mapster;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.GetById;

public class GetContactByIdQueryHandler
    : IRequestHandler<GetContactByIdQuery, ContactResponse>
{
    private readonly IContactRepository _contactRepository;

    public GetContactByIdQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
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

        return contact.Adapt<ContactResponse>();
    }
}
