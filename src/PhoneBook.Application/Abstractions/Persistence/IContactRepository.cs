using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IContactRepository
{
    Task<Result> AddAsync(
        Contact contact,
        CancellationToken cancellationToken);

    Task<Contact?> GetByIdAsync(
        ContactId id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Contact>> GetByTagAsync(
        Tag tag,
        CancellationToken cancellationToken);

    Task<Result> UpdateAsync(
        Contact contact,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        ContactId id,
        CancellationToken cancellationToken);
}
