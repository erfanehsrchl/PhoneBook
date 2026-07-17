using FluentAssertions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Infrastructure.Persistence;

namespace PhoneBook.Infrastructure.UnitTests.Persistence;

public class InMemoryContactRepositoryTests
{
    private const string FirstId = "00000000-0000-0000-0000-000000000001";
    private const string SecondId = "00000000-0000-0000-0000-000000000002";
    private const string ThirdId = "00000000-0000-0000-0000-000000000003";

    [Fact]
    public async Task AddAsync_should_store_independent_snapshot_and_audit_state()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        contact.Update("Sara", "Karimi", "09357654321", "Friend", ContactTestData.UpdatedAtUtc);

        await repository.AddAsync(contact, CancellationToken.None);
        Contact stored = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;

        ReferenceEquals(stored, contact).Should().BeFalse();
        stored.FirstName.Value.Should().Be("Sara");
        stored.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        stored.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
    }

    [Fact]
    public async Task AddAsync_should_throw_conflict_for_equivalent_phone_and_preserve_state()
    {
        InMemoryContactRepository repository = new();
        Contact first = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact duplicate = ContactTestData.CreateContact(SecondId, "00989121234567");
        await repository.AddAsync(first, CancellationToken.None);

        Func<Task> act = () => repository.AddAsync(duplicate, CancellationToken.None);

        ConflictException exception = (await act.Should().ThrowAsync<ConflictException>()).Which;
        exception.Code.Should().Be("Contact.PhoneNumberConflict");
        ((await repository.GetByIdAsync(first.Id, CancellationToken.None)) is not null)
            .Should()
            .BeTrue();
        ((await repository.GetByIdAsync(duplicate.Id, CancellationToken.None)) is null)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task AddAsync_should_throw_business_failure_for_duplicate_id()
    {
        InMemoryContactRepository repository = new();
        await repository.AddAsync(
            ContactTestData.CreateContact(FirstId, "09121234567"),
            CancellationToken.None);

        Func<Task> act = () => repository.AddAsync(
            ContactTestData.CreateContact(FirstId, "09357654321"),
            CancellationToken.None);

        BusinessRuleException exception =
            (await act.Should().ThrowAsync<BusinessRuleException>()).Which;
        exception.Code.Should().Be("Contact.InvalidState");
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_for_missing_contact()
    {
        InMemoryContactRepository repository = new();

        Contact? result = await repository.GetByIdAsync(
            new ContactId(Guid.Parse(FirstId)),
            CancellationToken.None);

        (result is null).Should().BeTrue();
    }

    [Fact]
    public async Task Mutating_read_contact_without_update_should_not_change_persisted_snapshot()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        await repository.AddAsync(contact, CancellationToken.None);
        Contact firstRead = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;
        Contact secondRead = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;

        firstRead.Update("Changed", "Changed", "09357654321", "Changed", ContactTestData.UpdatedAtUtc);
        Contact persisted = (await repository.GetByIdAsync(contact.Id, CancellationToken.None))!;

        ReferenceEquals(firstRead, secondRead).Should().BeFalse();
        persisted.FirstName.Value.Should().Be("Erfan");
        persisted.PhoneNumber.Value.Should().Be("+989121234567");
        persisted.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_should_order_before_paging_and_return_metadata()
    {
        InMemoryContactRepository repository = new();
        await repository.AddAsync(
            ContactTestData.CreateContact(ThirdId, "09357654321", createdAtUtc: ContactTestData.CreatedAtUtc.AddHours(1)),
            CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(SecondId, "09135554444"),
            CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(FirstId, "09124445555"),
            CancellationToken.None);

        PagedData<Contact> page = await repository.GetAllAsync(1, 2, CancellationToken.None);

        page.Items.Select(contact => contact.Id.Value).Should().ContainInOrder(
            Guid.Parse(FirstId),
            Guid.Parse(SecondId));
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.HasPreviousPage.Should().BeFalse();
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTagAsync_should_filter_then_page_case_insensitively()
    {
        InMemoryContactRepository repository = new();
        await repository.AddAsync(
            ContactTestData.CreateContact(FirstId, "09121234567", "Friend"),
            CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(SecondId, "09357654321", "Family"),
            CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(ThirdId, "09135554444", "friend"),
            CancellationToken.None);

        PagedData<Contact> page = await repository.GetByTagAsync(
            Tag.Create("FRIEND"),
            2,
            1,
            CancellationToken.None);

        page.Items.Should().ContainSingle();
        page.Items.Single().Id.Value.Should().Be(Guid.Parse(ThirdId));
        page.TotalCount.Should().Be(2);
        page.TotalPages.Should().Be(2);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_should_replace_snapshot_and_release_old_phone()
    {
        InMemoryContactRepository repository = new();
        Contact original = ContactTestData.CreateContact(FirstId, "09121234567");
        await repository.AddAsync(original, CancellationToken.None);
        Contact updated = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;
        updated.Update("Sara", "Karimi", "09357654321", "Friend", ContactTestData.UpdatedAtUtc);

        await repository.UpdateAsync(updated, CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(SecondId, "09121234567"),
            CancellationToken.None);
        Contact stored = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;

        stored.PhoneNumber.Value.Should().Be("+989357654321");
        stored.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
    }

    [Fact]
    public async Task Conflicting_update_should_throw_and_preserve_snapshots_and_indices()
    {
        InMemoryContactRepository repository = new();
        Contact first = ContactTestData.CreateContact(FirstId, "09121234567");
        Contact second = ContactTestData.CreateContact(SecondId, "09357654321");
        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);
        Contact conflicting = (await repository.GetByIdAsync(first.Id, CancellationToken.None))!;
        conflicting.Update("Changed", "Contact", "09357654321", "Changed", ContactTestData.UpdatedAtUtc);

        Func<Task> act = () => repository.UpdateAsync(conflicting, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        Contact stored = (await repository.GetByIdAsync(first.Id, CancellationToken.None))!;
        stored.PhoneNumber.Value.Should().Be("+989121234567");
        stored.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_should_throw_not_found_for_missing_contact()
    {
        InMemoryContactRepository repository = new();

        Func<Task> act = () => repository.UpdateAsync(
            ContactTestData.CreateContact(FirstId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_should_remove_contact_release_phone_and_throw_when_missing()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId, "09121234567");
        await repository.AddAsync(contact, CancellationToken.None);

        await repository.DeleteAsync(contact.Id, CancellationToken.None);
        await repository.AddAsync(
            ContactTestData.CreateContact(SecondId, "09121234567"),
            CancellationToken.None);
        Func<Task> missingDelete = () => repository.DeleteAsync(contact.Id, CancellationToken.None);

        ((await repository.GetByIdAsync(contact.Id, CancellationToken.None)) is null)
            .Should()
            .BeTrue();
        await missingDelete.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Repository_operations_should_honor_already_cancelled_token()
    {
        InMemoryContactRepository repository = new();
        Contact contact = ContactTestData.CreateContact(FirstId);
        using CancellationTokenSource source = new();
        source.Cancel();
        Func<Task>[] operations =
        [
            () => repository.AddAsync(contact, source.Token),
            () => repository.GetByIdAsync(contact.Id, source.Token),
            () => repository.GetAllAsync(1, 20, source.Token),
            () => repository.GetByTagAsync(Tag.Create("Friend"), 1, 20, source.Token),
            () => repository.UpdateAsync(contact, source.Token),
            () => repository.DeleteAsync(contact.Id, source.Token)
        ];

        foreach (Func<Task> operation in operations)
        {
            await operation.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
