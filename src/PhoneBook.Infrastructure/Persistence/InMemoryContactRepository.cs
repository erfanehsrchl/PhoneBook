using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Infrastructure.Persistence;

public class InMemoryContactRepository : IContactRepository
{
    private readonly Dictionary<ContactId, ContactSnapshot> _contacts = new();
    private readonly Dictionary<PhoneNumber, ContactId> _phoneIndex = new();
    private readonly object _syncRoot = new();

    public Task AddAsync(
        Contact contact,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(contact);

        lock (_syncRoot)
        {
            if (_contacts.ContainsKey(contact.Id))
            {
                throw new BusinessRuleException(
                    "Contact.InvalidState",
                    "A contact with this ID already exists.");
            }

            if (_phoneIndex.ContainsKey(contact.PhoneNumber))
            {
                throw new ConflictException(
                    "Contact.PhoneNumberConflict",
                    "A contact with this phone number already exists.");
            }

            ContactSnapshot snapshot = ContactSnapshot.FromContact(contact);
            _contacts.Add(contact.Id, snapshot);
            _phoneIndex.Add(contact.PhoneNumber, contact.Id);
        }

        return Task.CompletedTask;
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

    public Task<PagedData<Contact>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePagination(pageNumber, pageSize);

        PagedData<Contact> result;

        lock (_syncRoot)
        {
            result = CreatePage(_contacts.Values, pageNumber, pageSize);
        }

        return Task.FromResult(result);
    }

    public Task<PagedData<Contact>> GetByTagAsync(
        Tag tag,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tag);
        ValidatePagination(pageNumber, pageSize);

        PagedData<Contact> result;

        lock (_syncRoot)
        {
            result = CreatePage(
                _contacts.Values.Where(snapshot => snapshot.Tag.Equals(tag)),
                pageNumber,
                pageSize);
        }

        return Task.FromResult(result);
    }

    private static PagedData<Contact> CreatePage(
        IEnumerable<ContactSnapshot> source,
        int pageNumber,
        int pageSize)
    {
        ContactSnapshot[] orderedSnapshots = source
            .OrderBy(snapshot => snapshot.CreatedAtUtc)
            .ThenBy(snapshot => snapshot.Id.Value)
            .ToArray();
        long offset = (long)(pageNumber - 1) * pageSize;
        Contact[] contacts = offset >= orderedSnapshots.Length
            ? []
            : orderedSnapshots
                .Skip((int)offset)
                .Take(pageSize)
                .Select(Rehydrate)
                .ToArray();

        return new PagedData<Contact>(
            contacts,
            pageNumber,
            pageSize,
            orderedSnapshots.Length);
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
    }

    public Task UpdateAsync(
        Contact contact,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(contact);

        lock (_syncRoot)
        {
            if (!_contacts.TryGetValue(contact.Id, out ContactSnapshot? storedSnapshot))
            {
                throw new NotFoundException(
                    "Contact.NotFound",
                    "Contact was not found.");
            }

            PhoneNumber oldPhoneNumber = storedSnapshot.PhoneNumber;
            PhoneNumber newPhoneNumber = contact.PhoneNumber;
            bool phoneNumberChanged = !oldPhoneNumber.Equals(newPhoneNumber);

            if (phoneNumberChanged
                && _phoneIndex.TryGetValue(newPhoneNumber, out ContactId existingContactId)
                && existingContactId != contact.Id)
            {
                throw new ConflictException(
                    "Contact.PhoneNumberConflict",
                    "A contact with this phone number already exists.");
            }

            ContactSnapshot updatedSnapshot = ContactSnapshot.FromContact(contact);
            _contacts[contact.Id] = updatedSnapshot;

            if (phoneNumberChanged)
            {
                _phoneIndex.Remove(oldPhoneNumber);
                _phoneIndex.Add(newPhoneNumber, contact.Id);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ContactId id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_contacts.TryGetValue(id, out ContactSnapshot? snapshot))
            {
                throw new NotFoundException(
                    "Contact.NotFound",
                    "Contact was not found.");
            }

            _contacts.Remove(id);
            _phoneIndex.Remove(snapshot.PhoneNumber);
        }

        return Task.CompletedTask;
    }

    private static Contact Rehydrate(ContactSnapshot snapshot)
    {
        return Contact.Rehydrate(
            snapshot.Id,
            snapshot.FirstName,
            snapshot.LastName,
            snapshot.PhoneNumber,
            snapshot.Tag,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc);
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
