using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Contacts.GetById;

public class GetContactByIdQueryHandler
    : IQueryHandler<GetContactByIdQuery, ContactResponse>
{
    private readonly IContactRepository _contactRepository;

    public GetContactByIdQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Result<ContactResponse>> Handle(
        GetContactByIdQuery request,
        CancellationToken cancellationToken)
    {
        Result<ContactId> contactIdResult = ContactId.Create(request.ContactId);

        if (contactIdResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(contactIdResult.Error);
        }

        Contact? contact = await _contactRepository.GetByIdAsync(
            contactIdResult.Value,
            cancellationToken);

        return contact is null
            ? Result<ContactResponse>.Failure(ContactErrors.NotFound)
            : Result<ContactResponse>.Success(contact.ToResponse());
    }
}
