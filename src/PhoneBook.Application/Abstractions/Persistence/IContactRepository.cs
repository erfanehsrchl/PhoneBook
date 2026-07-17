using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IContactRepository
{
    Task AddAsync(
        Contact contact,
        CancellationToken cancellationToken);

    Task<Contact?> GetByIdAsync(
        ContactId id,
        CancellationToken cancellationToken);

    Task<PagedData<Contact>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedData<Contact>> GetByTagAsync(
        Tag tag,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Contact contact,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ContactId id,
        CancellationToken cancellationToken);
}
