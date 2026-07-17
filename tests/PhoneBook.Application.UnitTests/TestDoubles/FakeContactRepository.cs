using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.UnitTests.TestDoubles;

internal class FakeContactRepository : IContactRepository
{
    public Exception? AddException { get; set; }

    public Exception? UpdateException { get; set; }

    public Exception? DeleteException { get; set; }

    public Contact? ContactToReturn { get; set; }

    public IReadOnlyCollection<Contact> ContactsByTag { get; set; } = [];

    public IReadOnlyCollection<Contact> AllContacts { get; set; } = [];

    public int AddCallCount { get; private set; }

    public int GetByIdCallCount { get; private set; }

    public int GetByTagCallCount { get; private set; }

    public int GetAllCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public Contact? AddedContact { get; private set; }

    public Contact? UpdatedContact { get; private set; }

    public Tag? RequestedTag { get; private set; }

    public int RequestedPageNumber { get; private set; }

    public int RequestedPageSize { get; private set; }

    public CancellationToken CapturedCancellationToken { get; private set; }

    public Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        AddCallCount++;
        AddedContact = contact;
        CapturedCancellationToken = cancellationToken;
        return AddException is null
            ? Task.CompletedTask
            : Task.FromException(AddException);
    }

    public Task<Contact?> GetByIdAsync(
        ContactId id,
        CancellationToken cancellationToken)
    {
        GetByIdCallCount++;
        CapturedCancellationToken = cancellationToken;
        return Task.FromResult(ContactToReturn);
    }

    public Task<PagedData<Contact>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        GetAllCallCount++;
        CapturePaging(pageNumber, pageSize, cancellationToken);
        return Task.FromResult(
            new PagedData<Contact>(AllContacts, pageNumber, pageSize, AllContacts.Count));
    }

    public Task<PagedData<Contact>> GetByTagAsync(
        Tag tag,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        GetByTagCallCount++;
        RequestedTag = tag;
        CapturePaging(pageNumber, pageSize, cancellationToken);
        return Task.FromResult(
            new PagedData<Contact>(ContactsByTag, pageNumber, pageSize, ContactsByTag.Count));
    }

    public Task UpdateAsync(Contact contact, CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        UpdatedContact = contact;
        CapturedCancellationToken = cancellationToken;
        return UpdateException is null
            ? Task.CompletedTask
            : Task.FromException(UpdateException);
    }

    public Task DeleteAsync(ContactId id, CancellationToken cancellationToken)
    {
        DeleteCallCount++;
        CapturedCancellationToken = cancellationToken;
        return DeleteException is null
            ? Task.CompletedTask
            : Task.FromException(DeleteException);
    }

    private void CapturePaging(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        RequestedPageNumber = pageNumber;
        RequestedPageSize = pageSize;
        CapturedCancellationToken = cancellationToken;
    }
}
