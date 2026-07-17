using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.UnitTests.TestDoubles;

internal class FakeContactRepository : IContactRepository
{
    public Result AddResult { get; set; } = Result.Success();

    public Result UpdateResult { get; set; } = Result.Success();

    public bool DeleteResult { get; set; } = true;

    public Contact? ContactToReturn { get; set; }

    public IReadOnlyCollection<Contact> ContactsByTag { get; set; } = [];

    public int AddCallCount { get; private set; }

    public int GetByIdCallCount { get; private set; }

    public int GetByTagCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public Contact? AddedContact { get; private set; }

    public Contact? UpdatedContact { get; private set; }

    public Tag? RequestedTag { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public CancellationToken GetByTagCancellationToken { get; private set; }

    public CancellationToken UpdateCancellationToken { get; private set; }

    public CancellationToken DeleteCancellationToken { get; private set; }

    public Task<Result> AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        AddCallCount++;
        AddedContact = contact;
        AddCancellationToken = cancellationToken;
        return Task.FromResult(AddResult);
    }

    public Task<Contact?> GetByIdAsync(
        ContactId id,
        CancellationToken cancellationToken)
    {
        GetByIdCallCount++;
        GetByIdCancellationToken = cancellationToken;
        return Task.FromResult(ContactToReturn);
    }

    public Task<IReadOnlyCollection<Contact>> GetByTagAsync(
        Tag tag,
        CancellationToken cancellationToken)
    {
        GetByTagCallCount++;
        RequestedTag = tag;
        GetByTagCancellationToken = cancellationToken;
        return Task.FromResult(ContactsByTag);
    }

    public Task<Result> UpdateAsync(Contact contact, CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        UpdatedContact = contact;
        UpdateCancellationToken = cancellationToken;
        return Task.FromResult(UpdateResult);
    }

    public Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken)
    {
        DeleteCallCount++;
        DeleteCancellationToken = cancellationToken;
        return Task.FromResult(DeleteResult);
    }
}
