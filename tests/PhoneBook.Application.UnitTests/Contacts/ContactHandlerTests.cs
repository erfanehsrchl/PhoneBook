using FluentAssertions;
using PhoneBook.Application.Contacts.Create;
using PhoneBook.Application.Contacts.Delete;
using PhoneBook.Application.Contacts.GetById;
using PhoneBook.Application.Contacts.GetByTag;
using PhoneBook.Application.Contacts.Update;
using PhoneBook.Application.UnitTests.TestDoubles;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.UnitTests.Contacts;

public class ContactHandlerTests
{
    [Fact]
    public async Task Create_handler_should_return_mapped_contact_and_forward_clock_and_token()
    {
        FakeContactRepository repository = new();
        FakeDateTimeProvider clock = new(ContactTestData.CreatedAtUtc);
        CreateContactCommandHandler handler = new(repository, clock);
        using CancellationTokenSource source = new();
        CreateContactCommand command = new(
            " Erfan ",
            " Ahmadi ",
            "0912-123-4567",
            " Coworker ");

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            command,
            source.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.FirstName.Should().Be("Erfan");
        result.Value.LastName.Should().Be("Ahmadi");
        result.Value.PhoneNumber.Should().Be("+989121234567");
        result.Value.Tag.Should().Be("Coworker");
        result.Value.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        result.Value.UpdatedAtUtc.Should().BeNull();
        repository.AddCallCount.Should().Be(1);
        repository.GetByIdCallCount.Should().Be(0);
        repository.AddCancellationToken.Equals(source.Token).Should().BeTrue();
        (repository.AddedContact is not null).Should().BeTrue();
        repository.AddedContact!.CreatedAtUtc.Should().Be(clock.UtcNow);
    }

    [Theory]
    [InlineData("", "Ahmadi", "09121234567", "Coworker", "Contact.FirstName.Required")]
    [InlineData("Erfan", "", "09121234567", "Coworker", "Contact.LastName.Required")]
    [InlineData("Erfan", "Ahmadi", "invalid", "Coworker", "Contact.PhoneNumber.Invalid")]
    [InlineData("Erfan", "Ahmadi", "09121234567", "", "Contact.Tag.Required")]
    public async Task Create_handler_should_propagate_domain_input_failure(
        string firstName,
        string lastName,
        string phoneNumber,
        string tag,
        string expectedCode)
    {
        FakeContactRepository repository = new();
        CreateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.CreatedAtUtc));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(firstName, lastName, phoneNumber, tag),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
        repository.AddCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_handler_should_propagate_repository_conflict()
    {
        FakeContactRepository repository = new()
        {
            AddResult = Result.Failure(ContactErrors.PhoneNumberAlreadyExists)
        };
        CreateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.CreatedAtUtc));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new("Erfan", "Ahmadi", "09121234567", "Coworker"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.PhoneNumber.AlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Update_handler_should_persist_detached_contact_and_return_mapped_response()
    {
        Contact existing = ContactTestData.Create();
        FakeContactRepository repository = new() { ContactToReturn = existing };
        FakeDateTimeProvider clock = new(ContactTestData.UpdatedAtUtc);
        UpdateContactCommandHandler handler = new(repository, clock);
        using CancellationTokenSource source = new();

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(
                ContactTestData.ContactGuid,
                "Sara",
                "Karimi",
                "0935 765 4321",
                "Friend"),
            source.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Sara");
        result.Value.LastName.Should().Be("Karimi");
        result.Value.PhoneNumber.Should().Be("+989357654321");
        result.Value.Tag.Should().Be("Friend");
        result.Value.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        result.Value.UpdatedAtUtc.Should().Be(clock.UtcNow);
        repository.GetByIdCallCount.Should().Be(1);
        repository.UpdateCallCount.Should().Be(1);
        repository.GetByIdCancellationToken.Equals(source.Token).Should().BeTrue();
        repository.UpdateCancellationToken.Equals(source.Token).Should().BeTrue();
        ReferenceEquals(repository.UpdatedContact, existing).Should().BeTrue();
    }

    [Fact]
    public async Task Update_handler_should_return_not_found_without_persisting()
    {
        FakeContactRepository repository = new();
        UpdateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.UpdatedAtUtc));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid, "Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.NotFound");
        repository.UpdateCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", "Karimi", "09357654321", "Friend", "Contact.FirstName.Required")]
    [InlineData("Sara", "", "09357654321", "Friend", "Contact.LastName.Required")]
    [InlineData("Sara", "Karimi", "invalid", "Friend", "Contact.PhoneNumber.Invalid")]
    [InlineData("Sara", "Karimi", "09357654321", "", "Contact.Tag.Required")]
    public async Task Update_handler_should_propagate_domain_input_failure_without_persisting(
        string firstName,
        string lastName,
        string phoneNumber,
        string tag,
        string expectedCode)
    {
        FakeContactRepository repository = new() { ContactToReturn = ContactTestData.Create() };
        UpdateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.UpdatedAtUtc));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid, firstName, lastName, phoneNumber, tag),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_handler_should_not_persist_when_domain_rejects_clock()
    {
        FakeContactRepository repository = new() { ContactToReturn = ContactTestData.Create() };
        UpdateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.CreatedAtUtc.AddTicks(-1)));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid, "Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Timestamp.BeforeCreated");
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_handler_should_propagate_repository_conflict()
    {
        FakeContactRepository repository = new()
        {
            ContactToReturn = ContactTestData.Create(),
            UpdateResult = Result.Failure(ContactErrors.PhoneNumberAlreadyExists)
        };
        UpdateContactCommandHandler handler = new(
            repository,
            new FakeDateTimeProvider(ContactTestData.UpdatedAtUtc));

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid, "Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.PhoneNumber.AlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Theory]
    [InlineData(true, true, null)]
    [InlineData(false, false, "Contact.NotFound")]
    public async Task Delete_handler_should_use_single_atomic_delete_call(
        bool repositoryDeleteResult,
        bool expectedSuccess,
        string? expectedErrorCode)
    {
        FakeContactRepository repository = new() { DeleteResult = repositoryDeleteResult };
        DeleteContactCommandHandler handler = new(repository);
        using CancellationTokenSource source = new();

        Result result = await handler.Handle(
            new(ContactTestData.ContactGuid),
            source.Token);

        result.IsSuccess.Should().Be(expectedSuccess);
        result.Error.Code.Should().Be(expectedErrorCode ?? "Error.None");
        repository.DeleteCallCount.Should().Be(1);
        repository.GetByIdCallCount.Should().Be(0);
        repository.DeleteCancellationToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_handler_should_map_primitive_and_audit_values_and_forward_token()
    {
        Contact contact = ContactTestData.Create();
        contact.Update(
            "Sara",
            "Karimi",
            "09357654321",
            "Friend",
            ContactTestData.UpdatedAtUtc);
        FakeContactRepository repository = new() { ContactToReturn = contact };
        GetContactByIdQueryHandler handler = new(repository);
        using CancellationTokenSource source = new();

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid),
            source.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(ContactTestData.ContactGuid);
        result.Value.FirstName.Should().Be("Sara");
        result.Value.PhoneNumber.Should().Be("+989357654321");
        result.Value.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        result.Value.UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
        repository.GetByIdCancellationToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_handler_should_return_not_found()
    {
        FakeContactRepository repository = new();
        GetContactByIdQueryHandler handler = new(repository);

        Result<Application.Contacts.Common.ContactResponse> result = await handler.Handle(
            new(ContactTestData.ContactGuid),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.NotFound");
    }

    [Fact]
    public async Task GetByTag_handler_should_preserve_order_map_all_contacts_and_forward_token()
    {
        Contact first = ContactTestData.Create(
            firstName: "First",
            phoneNumber: "09121234567",
            tag: "Coworker");
        Contact second = ContactTestData.Create(
            id: Guid.Parse("22334455-6677-8899-aabb-ccddeeff0011"),
            firstName: "Second",
            phoneNumber: "09357654321",
            tag: "coworker");
        second.Update(
            "Second",
            "Updated",
            "09357654321",
            "coworker",
            ContactTestData.UpdatedAtUtc);
        FakeContactRepository repository = new() { ContactsByTag = [first, second] };
        GetContactsByTagQueryHandler handler = new(repository);
        using CancellationTokenSource source = new();

        Result<IReadOnlyCollection<Application.Contacts.Common.ContactResponse>> result =
            await handler.Handle(new(" COWORKER "), source.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(response => response.FirstName)
            .Should()
            .ContainInOrder("First", "Second");
        result.Value.Last().PhoneNumber.Should().Be("+989357654321");
        result.Value.Last().CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        result.Value.Last().UpdatedAtUtc.Should().Be(ContactTestData.UpdatedAtUtc);
        repository.GetByTagCallCount.Should().Be(1);
        repository.RequestedTag!.Value.Should().Be("COWORKER");
        repository.GetByTagCancellationToken.Equals(source.Token).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTag_handler_should_return_success_with_empty_collection()
    {
        FakeContactRepository repository = new();
        GetContactsByTagQueryHandler handler = new(repository);

        Result<IReadOnlyCollection<Application.Contacts.Common.ContactResponse>> result =
            await handler.Handle(new("Missing"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTag_handler_should_return_domain_failure_for_invalid_tag()
    {
        FakeContactRepository repository = new();
        GetContactsByTagQueryHandler handler = new(repository);

        Result<IReadOnlyCollection<Application.Contacts.Common.ContactResponse>> result =
            await handler.Handle(new(" "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contact.Tag.Required");
        repository.GetByTagCallCount.Should().Be(0);
    }
}
