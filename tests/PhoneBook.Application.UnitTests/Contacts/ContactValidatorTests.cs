using FluentAssertions;
using FluentValidation.Results;
using PhoneBook.Application.Contacts.Create;
using PhoneBook.Application.Contacts.Delete;
using PhoneBook.Application.Contacts.GetAll;
using PhoneBook.Application.Contacts.GetById;
using PhoneBook.Application.Contacts.GetByTag;
using PhoneBook.Application.Contacts.Update;

namespace PhoneBook.Application.UnitTests.Contacts;

public class ContactValidatorTests
{
    [Fact]
    public void Create_validator_should_reject_missing_fields()
    {
        CreateContactCommandValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateContactCommand("", "", "", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain(
            nameof(CreateContactCommand.FirstName),
            nameof(CreateContactCommand.LastName),
            nameof(CreateContactCommand.PhoneNumber),
            nameof(CreateContactCommand.Tag));
    }

    [Fact]
    public void Create_validator_should_reject_overlong_names_and_tag()
    {
        CreateContactCommandValidator validator = new();
        string overlong = new('a', 101);

        ValidationResult result = validator.Validate(
            new CreateContactCommand(overlong, overlong, "09121234567", overlong));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateContactCommand.FirstName));
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateContactCommand.LastName));
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateContactCommand.Tag));
    }

    [Fact]
    public void Create_validator_should_accept_valid_input()
    {
        CreateContactCommandValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateContactCommand("Erfan", "Ahmadi", "09121234567", "Coworker"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_validator_should_reject_empty_id_and_required_fields()
    {
        UpdateContactCommandValidator validator = new();

        ValidationResult result = validator.Validate(
            new UpdateContactCommand(Guid.Empty, "", "", "", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain(
            nameof(UpdateContactCommand.ContactId),
            nameof(UpdateContactCommand.FirstName),
            nameof(UpdateContactCommand.LastName),
            nameof(UpdateContactCommand.PhoneNumber),
            nameof(UpdateContactCommand.Tag));
    }

    [Fact]
    public void Update_validator_should_reject_overlong_fields()
    {
        UpdateContactCommandValidator validator = new();
        string overlong = new('a', 101);

        ValidationResult result = validator.Validate(
            new UpdateContactCommand(
                ContactTestData.ContactGuid,
                overlong,
                overlong,
                "09121234567",
                overlong));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Update_validator_should_accept_valid_input()
    {
        UpdateContactCommandValidator validator = new();

        ValidationResult result = validator.Validate(
            new UpdateContactCommand(
                ContactTestData.ContactGuid,
                "Sara",
                "Karimi",
                "09357654321",
                "Friend"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Delete_validator_should_validate_id(bool useValidId)
    {
        DeleteContactCommandValidator validator = new();

        ValidationResult result = validator.Validate(
            new DeleteContactCommand(
                useValidId ? ContactTestData.ContactGuid : Guid.Empty));

        result.IsValid.Should().Be(useValidId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetById_validator_should_validate_id(bool useValidId)
    {
        GetContactByIdQueryValidator validator = new();

        ValidationResult result = validator.Validate(
            new GetContactByIdQuery(
                useValidId ? ContactTestData.ContactGuid : Guid.Empty));

        result.IsValid.Should().Be(useValidId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void GetByTag_validator_should_reject_invalid_tag(string tag)
    {
        GetContactsByTagQueryValidator validator = new();

        ValidationResult result = validator.Validate(new GetContactsByTagQuery(tag, 1, 20));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetByTag_validator_should_accept_valid_tag()
    {
        GetContactsByTagQueryValidator validator = new();

        ValidationResult result = validator.Validate(
            new GetContactsByTagQuery("Coworker", 1, 20));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetAll_validator_should_reject_invalid_pagination(
        int pageNumber,
        int pageSize)
    {
        GetContactsQueryValidator validator = new();

        ValidationResult result = validator.Validate(
            new GetContactsQuery(pageNumber, pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetAll_validator_should_accept_valid_pagination()
    {
        GetContactsQueryValidator validator = new();

        ValidationResult result = validator.Validate(new GetContactsQuery(1, 100));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetByTag_validator_should_reject_invalid_pagination(
        int pageNumber,
        int pageSize)
    {
        GetContactsByTagQueryValidator validator = new();

        ValidationResult result = validator.Validate(
            new GetContactsByTagQuery("Coworker", pageNumber, pageSize));

        result.IsValid.Should().BeFalse();
    }
}
