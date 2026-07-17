using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Infrastructure.Persistence;

public class InMemoryContactRepository : IContactRepository
{
    private readonly Dictionary<ContactId, ContactSnapshot> _contacts = new();
    private readonly Dictionary<PhoneNumber, ContactId> _phoneIndex = new();
    private readonly object _syncRoot = new();

    public Task<Result> AddAsync(
        Contact contact,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(contact);

        lock (_syncRoot)
        {
            if (_contacts.ContainsKey(contact.Id))
            {
                return Task.FromResult(Result.Failure(ContactErrors.IdAlreadyExists));
            }

            if (_phoneIndex.ContainsKey(contact.PhoneNumber))
            {
                return Task.FromResult(Result.Failure(ContactErrors.PhoneNumberAlreadyExists));
            }

            ContactSnapshot snapshot = ContactSnapshot.FromContact(contact);
            _contacts.Add(contact.Id, snapshot);
            _phoneIndex.Add(contact.PhoneNumber, contact.Id);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Contact?> GetByIdAsync(
        ContactId id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ContactSnapshot? snapshot;

        lock (_syncRoot)
        {
            _contacts.TryGetValue(id, out snapshot);
        }

        Contact? contact = snapshot is null ? null : Rehydrate(snapshot);
        return Task.FromResult(contact);
    }

    public Task<IReadOnlyCollection<Contact>> GetByTagAsync(
        Tag tag,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tag);

        ContactSnapshot[] snapshots;

        lock (_syncRoot)
        {
            snapshots = _contacts.Values
                .Where(snapshot => snapshot.Tag.Equals(tag))
                .ToArray();
        }

        Contact[] contacts = snapshots
            .OrderBy(snapshot => snapshot.CreatedAtUtc)
            .ThenBy(snapshot => snapshot.Id.Value)
            .Select(Rehydrate)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Contact>>(contacts);
    }

    public Task<Result> UpdateAsync(
        Contact contact,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(contact);

        lock (_syncRoot)
        {
            if (!_contacts.TryGetValue(contact.Id, out ContactSnapshot? storedSnapshot))
            {
                return Task.FromResult(Result.Failure(ContactErrors.NotFound));
            }

            PhoneNumber oldPhoneNumber = storedSnapshot.PhoneNumber;
            PhoneNumber newPhoneNumber = contact.PhoneNumber;
            bool phoneNumberChanged = !oldPhoneNumber.Equals(newPhoneNumber);

            if (phoneNumberChanged
                && _phoneIndex.TryGetValue(newPhoneNumber, out ContactId existingContactId)
                && existingContactId != contact.Id)
            {
                return Task.FromResult(Result.Failure(ContactErrors.PhoneNumberAlreadyExists));
            }

            ContactSnapshot updatedSnapshot = ContactSnapshot.FromContact(contact);
            _contacts[contact.Id] = updatedSnapshot;

            if (phoneNumberChanged)
            {
                _phoneIndex.Remove(oldPhoneNumber);
                _phoneIndex.Add(newPhoneNumber, contact.Id);
            }
        }

        return Task.FromResult(Result.Success());
    }

    public Task<bool> DeleteAsync(
        ContactId id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_contacts.TryGetValue(id, out ContactSnapshot? snapshot))
            {
                return Task.FromResult(false);
            }

            _contacts.Remove(id);
            _phoneIndex.Remove(snapshot.PhoneNumber);
        }

        return Task.FromResult(true);
    }

    private static Contact Rehydrate(ContactSnapshot snapshot)
    {
        Result<Contact> result = Contact.Rehydrate(
            snapshot.Id,
            snapshot.FirstName,
            snapshot.LastName,
            snapshot.PhoneNumber,
            snapshot.Tag,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc);

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(
                $"Stored contact '{snapshot.Id.Value}' violates domain invariants: {result.Error.Code}.");
    }

    private record ContactSnapshot(
        ContactId Id,
        FirstName FirstName,
        LastName LastName,
        PhoneNumber PhoneNumber,
        Tag Tag,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public static ContactSnapshot FromContact(Contact contact)
        {
            return new ContactSnapshot(
                contact.Id,
                contact.FirstName,
                contact.LastName,
                contact.PhoneNumber,
                contact.Tag,
                contact.CreatedAtUtc,
                contact.UpdatedAtUtc);
        }
    }
}
