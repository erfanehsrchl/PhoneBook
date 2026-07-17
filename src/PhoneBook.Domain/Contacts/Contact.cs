using PhoneBook.Domain.Abstractions;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public class Contact : AggregateRoot<ContactId>
{
    private Contact(
        ContactId id,
        FirstName firstName,
        LastName lastName,
        PhoneNumber phoneNumber,
        Tag tag,
        DateTime createdAtUtc)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Tag = tag;
        CreatedAtUtc = createdAtUtc;
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
            createdAtUtc);

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

        FirstName = firstNameResult.Value;
        LastName = lastNameResult.Value;
        PhoneNumber = phoneNumberResult.Value;
        Tag = tagResult.Value;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }
}
