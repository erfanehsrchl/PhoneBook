using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Abstractions.Time;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Contacts.Update;

public class UpdateContactCommandHandler
    : ICommandHandler<UpdateContactCommand, ContactResponse>
{
    private readonly IContactRepository _contactRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateContactCommandHandler(
        IContactRepository contactRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _contactRepository = contactRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ContactResponse>> Handle(
        UpdateContactCommand request,
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

        if (contact is null)
        {
            return Result<ContactResponse>.Failure(ContactErrors.NotFound);
        }

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

        Result updateResult = contact.Update(
            firstNameResult.Value.Value,
            lastNameResult.Value.Value,
            phoneNumberResult.Value.Value,
            tagResult.Value.Value,
            _dateTimeProvider.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result<ContactResponse>.Failure(updateResult.Error);
        }

        Result repositoryResult = await _contactRepository.UpdateAsync(
            contact,
            cancellationToken);

        return repositoryResult.IsFailure
            ? Result<ContactResponse>.Failure(repositoryResult.Error)
            : Result<ContactResponse>.Success(contact.ToResponse());
    }
}
