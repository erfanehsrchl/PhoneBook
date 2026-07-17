using PhoneBook.Application.Abstractions.Messaging;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Contacts.GetByTag;

public class GetContactsByTagQueryHandler
    : IQueryHandler<GetContactsByTagQuery, IReadOnlyCollection<ContactResponse>>
{
    private readonly IContactRepository _contactRepository;

    public GetContactsByTagQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Result<IReadOnlyCollection<ContactResponse>>> Handle(
        GetContactsByTagQuery request,
        CancellationToken cancellationToken)
    {
        Result<Tag> tagResult = Tag.Create(request.Tag);

        if (tagResult.IsFailure)
        {
            return Result<IReadOnlyCollection<ContactResponse>>.Failure(tagResult.Error);
        }

        IReadOnlyCollection<Contact> contacts = await _contactRepository.GetByTagAsync(
            tagResult.Value,
            cancellationToken);
        ContactResponse[] responses = contacts
            .Select(contact => contact.ToResponse())
            .ToArray();

        return Result<IReadOnlyCollection<ContactResponse>>.Success(responses);
    }
}
