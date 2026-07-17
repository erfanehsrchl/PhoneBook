using PhoneBook.Domain.Abstractions;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public class Contact : AggregateRoot<ContactId>, IAuditableEntity
{
    private Contact(
        ContactId id,
        FirstName firstName,
        LastName lastName,
        PhoneNumber phoneNumber,
        Tag tag,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Tag = tag;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public FirstName FirstName { get; private set; }

    public LastName LastName { get; private set; }

    public PhoneNumber PhoneNumber { get; private set; }

    public Tag Tag { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Result<Contact> Create(
        ContactId id,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? tag,
        DateTime createdAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            return Result<Contact>.Failure(ContactErrors.IdEmpty);
        }

        Result<FirstName> firstNameResult = Contacts.FirstName.Create(firstName);

        if (firstNameResult.IsFailure)
        {
            return Result<Contact>.Failure(firstNameResult.Error);
        }

        Result<LastName> lastNameResult = Contacts.LastName.Create(lastName);

        if (lastNameResult.IsFailure)
        {
            return Result<Contact>.Failure(lastNameResult.Error);
        }

        Result<PhoneNumber> phoneNumberResult = Contacts.PhoneNumber.Create(phoneNumber);

        if (phoneNumberResult.IsFailure)
        {
            return Result<Contact>.Failure(phoneNumberResult.Error);
        }

        Result<Tag> tagResult = Contacts.Tag.Create(tag);

        if (tagResult.IsFailure)
        {
            return Result<Contact>.Failure(tagResult.Error);
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<Contact>.Failure(ContactErrors.TimestampMustBeUtc);
        }

        Contact contact = new(
            id,
            firstNameResult.Value,
            lastNameResult.Value,
            phoneNumberResult.Value,
            tagResult.Value,
            createdAtUtc,
            null);

        return Result<Contact>.Success(contact);
    }

    public Result Update(
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? tag,
        DateTime updatedAtUtc)
    {
        Result<FirstName> firstNameResult = Contacts.FirstName.Create(firstName);

        if (firstNameResult.IsFailure)
        {
            return Result.Failure(firstNameResult.Error);
        }

        Result<LastName> lastNameResult = Contacts.LastName.Create(lastName);

        if (lastNameResult.IsFailure)
        {
            return Result.Failure(lastNameResult.Error);
        }

        Result<PhoneNumber> phoneNumberResult = Contacts.PhoneNumber.Create(phoneNumber);

        if (phoneNumberResult.IsFailure)
        {
            return Result.Failure(phoneNumberResult.Error);
        }

        Result<Tag> tagResult = Contacts.Tag.Create(tag);

        if (tagResult.IsFailure)
        {
            return Result.Failure(tagResult.Error);
        }

        if (updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(ContactErrors.TimestampMustBeUtc);
        }

        if (updatedAtUtc < CreatedAtUtc)
        {
            return Result.Failure(ContactErrors.TimestampBeforeCreated);
        }

        FirstName = firstNameResult.Value;
        LastName = lastNameResult.Value;
        PhoneNumber = phoneNumberResult.Value;
        Tag = tagResult.Value;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }

    public static Result<Contact> Rehydrate(
        ContactId id,
        FirstName firstName,
        LastName lastName,
        PhoneNumber phoneNumber,
        Tag tag,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(tag);

        if (id.Value == Guid.Empty)
        {
            return Result<Contact>.Failure(ContactErrors.IdEmpty);
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc is { Kind: not DateTimeKind.Utc })
        {
            return Result<Contact>.Failure(ContactErrors.TimestampMustBeUtc);
        }

        if (updatedAtUtc < createdAtUtc)
        {
            return Result<Contact>.Failure(ContactErrors.TimestampBeforeCreated);
        }

        Contact contact = new(
            id,
            firstName,
            lastName,
            phoneNumber,
            tag,
            createdAtUtc,
            updatedAtUtc);

        return Result<Contact>.Success(contact);
    }
}
