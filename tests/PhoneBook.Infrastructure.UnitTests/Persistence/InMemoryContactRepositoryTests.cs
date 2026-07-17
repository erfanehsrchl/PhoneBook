using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;
using PhoneBook.Infrastructure.Persistence;

namespace PhoneBook.Infrastructure.UnitTests.Persistence;

public class InMemoryContactRepositoryTests
{
    private const string FirstId = "00000000-0000-0000-0000-000000000001";
    private const string SecondId = "00000000-0000-0000-0000-000000000002";
    private const string ThirdId = "00000000-0000-0000-0000-000000000003";

    [Fact]
    public async Task AddAsync_should_store_snapshot_and_preserve_audit_state()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        contact.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);

        Result addResult = await repository.AddAsync(contact, CancellationToken.None);
        Contact? stored = await repository.GetByIdAsync(contact.Id, CancellationToken.None);

        addResult.IsSuccess.Should().BeTrue();
        (stored is not null).Should().BeTrue();
        stored!.Id.Equals(contact.Id).Should().BeTrue();
        stored.FirstName.Value.Should().Be("Sara");
        stored.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        stored.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
        ReferenceEquals(stored, contact).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_should_reject_equivalent_phone_number_and_preserve_state()
    {
        InMemoryContactRepository repository = new();
        Contact first = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact duplicate = ContactTestData.CreateContact(SecondId, "00989121234567");

        Result firstResult = await repository.AddAsync(first, CancellationToken.None);
        Result duplicateResult = await repository.AddAsync(duplicate, CancellationToken.None);
        Contact? storedFirst = await repository.GetByIdAsync(first.Id, CancellationToken.None);
        Contact? storedDuplicate = await repository.GetByIdAsync(duplicate.Id, CancellationToken.None);
        Contact usableContact = ContactTestData.CreateContact(ThirdId, "09357654321");
        Result usableResult = await repository.AddAsync(usableContact, CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        duplicateResult.IsFailure.Should().BeTrue();
        duplicateResult.Error.Code.Should().Be("Contact.PhoneNumber.AlreadyExists");
        (storedFirst is not null).Should().BeTrue();
        (storedDuplicate is null).Should().BeTrue();
        usableResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_should_reject_duplicate_contact_id_without_overwrite()
    {
        InMemoryContactRepository repository = new();
        Contact original = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact duplicateId = ContactTestData.CreateContact(FirstId, "09357654321");

        await repository.AddAsync(original, CancellationToken.None);
        Result result = await repository.AddAsync(duplicateId, CancellationToken.None);
        Contact? stored = await repository.GetByIdAsync(original.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Id.AlreadyExists");
        stored!.PhoneNumber.Value.Should().Be("+989121234567");
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_for_missing_contact()
    {
        InMemoryContactRepository repository = new();
        ContactId missingId = ContactId.Create(Guid.Parse(FirstId)).Value;

        Contact? result = await repository.GetByIdAsync(missingId, CancellationToken.None);

        (result is null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_should_return_equivalent_independent_instances()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        await repository.AddAsync(contact, CancellationToken.None);

        Contact? firstRead = await repository.GetByIdAsync(contact.Id, CancellationToken.None);
        Contact? secondRead = await repository.GetByIdAsync(contact.Id, CancellationToken.None);

        (firstRead is not null).Should().BeTrue();
        (secondRead is not null).Should().BeTrue();
        ReferenceEquals(firstRead, contact).Should().BeFalse();
        ReferenceEquals(firstRead, secondRead).Should().BeFalse();
        firstRead!.PhoneNumber.Equals(secondRead!.PhoneNumber).Should().BeTrue();
        firstRead.CreatedAtUtc.Should().Be(secondRead.CreatedAtUtc);
        firstRead.UpdatedAtUtc.Should().Be(secondRead.UpdatedAtUtc);
    }

    [Fact]
    public async Task Mutating_read_contact_without_UpdateAsync_should_not_change_persisted_snapshot()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        await repository.AddAsync(contact, CancellationToken.None);
        Contact retrieved = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;

        retrieved.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);

        Contact persisted = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;

        persisted.FirstName.Value.Should().Be("Erfan");
        persisted.LastName.Value.Should().Be("Ahmadi");
        persisted.PhoneNumber.Value.Should().Be("+989121234567");
        persisted.Tag.Value.Should().Be("Coworker");
        persisted.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetByTagAsync_should_filter_using_value_equality_and_order_deterministically()
    {
        InMemoryContactRepository repository = new();
        Contact later = ContactTestData.CreateContact(
            ThirdId,
            "09357654321",
            "coworker",
            ContactTestData.CreatedAtUtc.AddHours(1));
        Contact tieSecond = ContactTestData.CreateContact(
            SecondId,
            "09135554444",
            "COWORKER");
        Contact tieFirst = ContactTestData.CreateContact(
            FirstId,
            "09124445555",
            "Coworker");
        Contact different = ContactTestData.CreateContact(
            "00000000-0000-0000-0000-000000000004",
            "09123334444",
            "Family");

        await repository.AddAsync(later, CancellationToken.None);
        await repository.AddAsync(tieSecond, CancellationToken.None);
        await repository.AddAsync(tieFirst, CancellationToken.None);
        await repository.AddAsync(different, CancellationToken.None);

        Tag query = Tag.Create(" COWORKER ").Value;
        IReadOnlyCollection<Contact> results = await repository.GetByTagAsync(
            query,
            CancellationToken.None);

        results.Should().HaveCount(3);
        results.Select(contact => contact.Id.Value).Should().ContainInOrder(
            Guid.Parse(FirstId),
            Guid.Parse(SecondId),
            Guid.Parse(ThirdId));
        results.Should().OnlyContain(contact => contact.Tag.Equals(query));
        results.Should().NotContain(contact => contact.Id.Equals(different.Id));
    }

    [Fact]
    public async Task GetByTagAsync_should_return_empty_independent_snapshot_and_preserve_audit_state()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        contact.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);
        await repository.AddAsync(contact, CancellationToken.None);

        IReadOnlyCollection<Contact> missing = await repository.GetByTagAsync(
            Tag.Create("Missing").Value,
            CancellationToken.None);
        IReadOnlyCollection<Contact> matches = await repository.GetByTagAsync(
            Tag.Create("friend").Value,
            CancellationToken.None);
        Contact match = matches.Single();

        missing.Should().BeEmpty();
        ReferenceEquals(match, contact).Should().BeFalse();
        match.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        match.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);

        match.Update(
            "Changed",
            "Changed",
            "09124445555",
            "Changed",
            ContactTestData.UpdatedAtUtc.AddHours(1));
        Contact persisted = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;
        persisted.FirstName.Value.Should().Be("Sara");
    }

    [Fact]
    public async Task UpdateAsync_should_replace_snapshot_and_preserve_identity_and_creation_time()
    {
        InMemoryContactRepository repository = new();
        Contact original = ContactTestData.CreateContact(FirstId);
        await repository.AddAsync(original, CancellationToken.None);
        Contact updated = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;
        updated.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);

        Result result = await repository.UpdateAsync(updated, CancellationToken.None);
        Contact stored = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;

        result.IsSuccess.Should().BeTrue();
        stored.Id.Equals(original.Id).Should().BeTrue();
        stored.FirstName.Value.Should().Be("Sara");
        stored.LastName.Value.Should().Be("Karimi");
        stored.Tag.Value.Should().Be("Friend");
        stored.PhoneNumber.Value.Should().Be("+989357654321");
        stored.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        stored.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_with_unchanged_phone_number_should_succeed()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        await repository.AddAsync(contact, CancellationToken.None);
        Contact updated = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;
        updated.Update(
            "Sara",
            "Karimi",
            "+989121234567",
            "Friend",
            ContactTestData.UpdatedAtUtc);

        Result result = await repository.UpdateAsync(updated, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_should_change_phone_index_and_release_old_number()
    {
        InMemoryContactRepository repository = new();
        Contact original = ContactTestData.CreateContact(FirstId, "09121234567");
        await repository.AddAsync(original, CancellationToken.None);
        Contact updated = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;
        updated.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);
        await repository.UpdateAsync(updated, CancellationToken.None);

        Contact oldNumberReuse = ContactTestData.CreateContact(SecondId, "09121234567");
        Contact newNumberDuplicate = ContactTestData.CreateContact(ThirdId, "+989357654321");
        Result oldNumberResult = await repository.AddAsync(oldNumberReuse, CancellationToken.None);
        Result newNumberResult = await repository.AddAsync(newNumberDuplicate, CancellationToken.None);

        oldNumberResult.IsSuccess.Should().BeTrue();
        newNumberResult.IsFailure.Should().BeTrue();
        newNumberResult.Error.Code.Should().Be("Contact.PhoneNumber.AlreadyExists");
    }

    [Fact]
    public async Task Duplicate_phone_update_should_preserve_both_snapshots_indices_and_audit_state()
    {
        InMemoryContactRepository repository = new();
        Contact first = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact second = ContactTestData.CreateContact(SecondId, "09357654321");
        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);

        Contact baseline = (await repository.GetByIdAsync(first.Id, CancellationToken.None))!;
        baseline.Update(
            "Baseline",
            "Contact",
            "09121234567",
            "Baseline",
            ContactTestData.UpdatedAtUtc);
        await repository.UpdateAsync(baseline, CancellationToken.None);

        Contact conflicting = (await repository.GetByIdAsync(first.Id, CancellationToken.None))!;
        conflicting.Update(
            "Partial",
            "Mutation",
            "09357654321",
            "Changed",
            ContactTestData.UpdatedAtUtc.AddHours(1));
        Result conflictResult = await repository.UpdateAsync(conflicting, CancellationToken.None);

        Contact storedFirst = (await repository.GetByIdAsync(first.Id, CancellationToken.None))!;
        Contact storedSecond = (await repository.GetByIdAsync(second.Id, CancellationToken.None))!;
        Result firstIndexResult = await repository.AddAsync(
            ContactTestData.CreateContact(ThirdId, "09121234567"),
            CancellationToken.None);
        Result secondIndexResult = await repository.AddAsync(
            ContactTestData.CreateContact(
                "00000000-0000-0000-0000-000000000004",
                "09357654321"),
            CancellationToken.None);

        conflictResult.IsFailure.Should().BeTrue();
        conflictResult.Error.Code.Should().Be("Contact.PhoneNumber.AlreadyExists");
        storedFirst.FirstName.Value.Should().Be("Baseline");
        storedFirst.PhoneNumber.Value.Should().Be("+989121234567");
        storedFirst.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
        storedSecond.PhoneNumber.Value.Should().Be("+989357654321");
        storedSecond.UpdatedAtUtc.Should().BeNull();
        firstIndexResult.IsFailure.Should().BeTrue();
        secondIndexResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_should_return_not_found_for_missing_contact()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);

        Result result = await repository.UpdateAsync(contact, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.NotFound");
    }

    [Fact]
    public async Task DeleteAsync_should_remove_only_target_and_release_phone_number()
    {
        InMemoryContactRepository repository = new();
        Contact first = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact second = ContactTestData.CreateContact(SecondId, "09357654321");
        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);

        bool deleted = await repository.DeleteAsync(first.Id, CancellationToken.None);
        Contact? removed = await repository.GetByIdAsync(first.Id, CancellationToken.None);
        Contact? remaining = await repository.GetByIdAsync(second.Id, CancellationToken.None);
        Result reused = await repository.AddAsync(
            ContactTestData.CreateContact(ThirdId, "09121234567"),
            CancellationToken.None);

        deleted.Should().BeTrue();
        (removed is null).Should().BeTrue();
        (remaining is not null).Should().BeTrue();
        reused.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_should_return_false_for_missing_contact()
    {
        InMemoryContactRepository repository = new();
        ContactId id = ContactId.Create(Guid.Parse(FirstId)).Value;

        bool result = await repository.DeleteAsync(id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        using CancellationTokenSource source = new();
        source.Cancel();

        Func<Task> act = () => repository.AddAsync(
            ContactTestData.CreateContact(FirstId),
            source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddAsync_should_reject_null_contact()
    {
        InMemoryContactRepository repository = new();

        Func<Task> act = () => repository.AddAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        ContactId id = ContactId.Create(Guid.Parse(FirstId)).Value;

        Func<Task> act = () => repository.GetByIdAsync(id, source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByTagAsync_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        using CancellationTokenSource source = new();
        source.Cancel();

        Func<Task> act = () => repository.GetByTagAsync(
            Tag.Create("Coworker").Value,
            source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByTagAsync_should_reject_null_tag()
    {
        InMemoryContactRepository repository = new();

        Func<Task> act = () => repository.GetByTagAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        using CancellationTokenSource source = new();
        source.Cancel();

        Func<Task> act = () => repository.UpdateAsync(
            ContactTestData.CreateContact(FirstId),
            source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteAsync_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        ContactId id = ContactId.Create(Guid.Parse(FirstId)).Value;

        Func<Task> act = () => repository.DeleteAsync(id, source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
