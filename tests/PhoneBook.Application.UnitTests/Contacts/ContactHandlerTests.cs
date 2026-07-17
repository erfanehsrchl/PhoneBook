using FluentAssertions;
using Mapster;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Application.Common.Mappings;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Application.Contacts.Create;
using PhoneBook.Application.Contacts.Delete;
using PhoneBook.Application.Contacts.GetAll;
using PhoneBook.Application.Contacts.GetById;
using PhoneBook.Application.Contacts.GetByTag;
using PhoneBook.Application.Contacts.Update;
using PhoneBook.Application.UnitTests.TestDoubles;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.UnitTests.Contacts;

public class ContactHandlerTests
{
    static ContactHandlerTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(ContactMappingConfig).Assembly);
        TypeAdapterConfig.GlobalSettings.Compile();
    }

    [Fact]
    public async Task Create_handler_should_return_contact_use_utc_time_and_forward_token()
    {
        FakeContactRepository repository = new();
        CreateContactCommandHandler handler = new(repository);
        using CancellationTokenSource source = new();
        DateTime before = DateTime.UtcNow;

        ContactResponse response = await handler.Handle(
            new("Erfan", "Ahmadi", "0912-123-4567", "Coworker"),
            source.Token);
        DateTime after = DateTime.UtcNow;

        response.PhoneNumber.Should().Be("+989121234567");
        response.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        response.CreatedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        response.UpdatedAtUtc.Should().BeNull();
        repository.AddCallCount.Should().Be(1);
        repository.AddedContact!.Id.Value.Should().Be(response.Id);
        repository.CapturedCancellationToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task Create_handler_should_propagate_conflict_exception()
    {
        ConflictException expected = new(
            "Contact.PhoneNumberConflict",
            "A contact with this phone number already exists.");
        FakeContactRepository repository = new() { AddException = expected };
        CreateContactCommandHandler handler = new(repository);

        Func<Task> act = () => handler.Handle(
            new("Erfan", "Ahmadi", "09121234567", "Coworker"),
            CancellationToken.None);

        ConflictException exception =
            (await act.Should().ThrowAsync<ConflictException>()).Which;
        exception.Code.Should().Be("Contact.PhoneNumberConflict");
    }

    [Fact]
    public async Task Update_handler_should_return_updated_contact_and_use_utc_time()
    {
        Contact contact = ContactTestData.Create();
        FakeContactRepository repository = new() { ContactToReturn = contact };
        UpdateContactCommandHandler handler = new(repository);
        DateTime before = DateTime.UtcNow;

        ContactResponse response = await handler.Handle(
            new(ContactTestData.ContactGuid, "Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);
        DateTime after = DateTime.UtcNow;

        response.FirstName.Should().Be("Sara");
        response.PhoneNumber.Should().Be("+989357654321");
        response.UpdatedAtUtc.Should().NotBeNull();
        response.UpdatedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        response.UpdatedAtUtc.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        repository.UpdateCallCount.Should().Be(1);
        ReferenceEquals(repository.UpdatedContact, contact).Should().BeTrue();
    }

    [Fact]
    public async Task Update_handler_should_throw_not_found_exception()
    {
        FakeContactRepository repository = new();
        UpdateContactCommandHandler handler = new(repository);

        Func<Task> act = () => handler.Handle(
            new(ContactTestData.ContactGuid, "Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        NotFoundException exception =
            (await act.Should().ThrowAsync<NotFoundException>()).Which;
        exception.Code.Should().Be("Contact.NotFound");
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_handler_should_forward_request()
    {
        FakeContactRepository repository = new();
        DeleteContactCommandHandler handler = new(repository);
        using CancellationTokenSource source = new();

        await handler.Handle(new(ContactTestData.ContactGuid), source.Token);

        repository.DeleteCallCount.Should().Be(1);
        repository.CapturedCancellationToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_handler_should_return_mapped_contact()
    {
        Contact contact = ContactTestData.Create();
        FakeContactRepository repository = new() { ContactToReturn = contact };
        GetContactByIdQueryHandler handler = new(repository);

        ContactResponse response = await handler.Handle(
            new(ContactTestData.ContactGuid),
            CancellationToken.None);

        response.Id.Should().Be(contact.Id.Value);
        response.PhoneNumber.Should().Be(contact.PhoneNumber.Value);
    }

    [Fact]
    public async Task GetById_handler_should_throw_not_found_exception()
    {
        GetContactByIdQueryHandler handler = new(new FakeContactRepository());

        Func<Task> act = () => handler.Handle(
            new(ContactTestData.ContactGuid),
            CancellationToken.None);

        NotFoundException exception =
            (await act.Should().ThrowAsync<NotFoundException>()).Which;
        exception.Code.Should().Be("Contact.NotFound");
        exception.Message.Should().Be("Contact was not found.");
    }

    [Fact]
    public async Task GetAll_handler_should_map_page_and_forward_pagination()
    {
        Contact contact = ContactTestData.Create();
        FakeContactRepository repository = new() { AllContacts = [contact] };
        GetContactsQueryHandler handler = new(repository);

        PagedData<ContactResponse> response = await handler.Handle(
            new(2, 10),
            CancellationToken.None);

        response.Items.Should().ContainSingle();
        response.Items.Single().Id.Should().Be(contact.Id.Value);
        response.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().Be(1);
        repository.GetAllCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByTag_handler_should_map_page_and_normalize_tag()
    {
        Contact contact = ContactTestData.Create();
        FakeContactRepository repository = new() { ContactsByTag = [contact] };
        GetContactsByTagQueryHandler handler = new(repository);

        PagedData<ContactResponse> response = await handler.Handle(
            new(" COWORKER ", 1, 20),
            CancellationToken.None);

        response.Items.Should().ContainSingle();
        repository.RequestedTag!.Value.Should().Be("COWORKER");
        repository.RequestedPageNumber.Should().Be(1);
        repository.RequestedPageSize.Should().Be(20);
    }

}
