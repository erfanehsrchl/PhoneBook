using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Contacts.Delete;

public class DeleteContactCommandHandler : ICommandHandler<DeleteContactCommand>
{
    private readonly IContactRepository _contactRepository;

    public DeleteContactCommandHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Result> Handle(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        Result<ContactId> contactIdResult = ContactId.Create(request.ContactId);

        if (contactIdResult.IsFailure)
        {
            return Result.Failure(contactIdResult.Error);
        }

        bool deleted = await _contactRepository.DeleteAsync(
            contactIdResult.Value,
            cancellationToken);

        return deleted
            ? Result.Success()
            : Result.Failure(ContactErrors.NotFound);
    }
}
