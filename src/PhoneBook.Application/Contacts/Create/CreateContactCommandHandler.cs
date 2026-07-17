using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Abstractions.Time;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Contacts.Create;

public class CreateContactCommandHandler
    : ICommandHandler<CreateContactCommand, ContactResponse>
{
    private readonly IContactRepository _contactRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateContactCommandHandler(
        IContactRepository contactRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _contactRepository = contactRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ContactResponse>> Handle(
        CreateContactCommand request,
        CancellationToken cancellationToken)
    {
        Result<FirstName> firstNameResult = FirstName.Create(request.FirstName);

        if (firstNameResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(firstNameResult.Error);
        }

        Result<LastName> lastNameResult = LastName.Create(request.LastName);

        if (lastNameResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(lastNameResult.Error);
        }

        Result<PhoneNumber> phoneNumberResult = PhoneNumber.Create(request.PhoneNumber);

        if (phoneNumberResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(phoneNumberResult.Error);
        }

        Result<Tag> tagResult = Tag.Create(request.Tag);

        if (tagResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(tagResult.Error);
        }

        ContactId contactId = ContactId.New();
        Result<Contact> contactResult = Contact.Create(
            contactId,
            firstNameResult.Value.Value,
            lastNameResult.Value.Value,
            phoneNumberResult.Value.Value,
            tagResult.Value.Value,
            _dateTimeProvider.UtcNow);

        if (contactResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(contactResult.Error);
        }

        Result addResult = await _contactRepository.AddAsync(
            contactResult.Value,
            cancellationToken);

        return addResult.IsFailure
            ? Result<ContactResponse>.Failure(addResult.Error)
            : Result<ContactResponse>.Success(contactResult.Value.ToResponse());
    }
}
