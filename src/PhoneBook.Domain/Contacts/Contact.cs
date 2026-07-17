using PhoneBook.Domain.Abstractions;

namespace PhoneBook.Domain.Contacts;

public class Contact : AggregateRoot<ContactId>, IAuditableEntity
{
    private const int MaximumNameLength = 100;

    private Contact(
        ContactId id,
        string firstName,
        string lastName,
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

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public PhoneNumber PhoneNumber { get; private set; }

    public Tag Tag { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Contact Create(
        ContactId id,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? tag,
        DateTime createdAtUtc)
    {
        ValidateCreationTimestamp(createdAtUtc);

        return new Contact(
            id,
            ValidateRequiredName(firstName, "First name", nameof(firstName)),
            ValidateRequiredName(lastName, "Last name", nameof(lastName)),
            Contacts.PhoneNumber.Create(phoneNumber),
            Contacts.Tag.Create(tag),
            createdAtUtc,
            null);
    }

    public void Update(
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? tag,
        DateTime updatedAtUtc)
    {
        string newFirstName = ValidateRequiredName(
            firstName,
            "First name",
            nameof(firstName));
        string newLastName = ValidateRequiredName(
            lastName,
            "Last name",
            nameof(lastName));
        PhoneNumber newPhoneNumber = Contacts.PhoneNumber.Create(phoneNumber);
        Tag newTag = Contacts.Tag.Create(tag);
        ValidateUpdateTimestamp(updatedAtUtc);

        FirstName = newFirstName;
        LastName = newLastName;
        PhoneNumber = newPhoneNumber;
        Tag = newTag;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Contact Rehydrate(
        ContactId id,
        string? firstName,
        string? lastName,
        PhoneNumber phoneNumber,
        Tag tag,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        string normalizedFirstName = ValidateRequiredName(
            firstName,
            "First name",
            nameof(firstName));
        string normalizedLastName = ValidateRequiredName(
            lastName,
            "Last name",
            nameof(lastName));
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(tag);
        ValidateCreationTimestamp(createdAtUtc);

        if (updatedAtUtc is not null)
        {
            if (updatedAtUtc.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Contact timestamps must be in UTC.",
                    nameof(updatedAtUtc));
            }

            if (updatedAtUtc.Value < createdAtUtc)
            {
                throw new ArgumentException(
                    "The update timestamp must not be earlier than the creation timestamp.",
                    nameof(updatedAtUtc));
            }
        }

        return new Contact(
            id,
            normalizedFirstName,
            normalizedLastName,
            phoneNumber,
            tag,
            createdAtUtc,
            updatedAtUtc);
    }

    private static string ValidateRequiredName(
        string? value,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", parameterName);
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumNameLength)
        {
            throw new ArgumentException(
                $"{displayName} must not exceed 100 characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static void ValidateCreationTimestamp(DateTime createdAtUtc)
    {
        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Contact timestamps must be in UTC.",
                nameof(createdAtUtc));
        }
    }

    private void ValidateUpdateTimestamp(DateTime updatedAtUtc)
    {
        if (updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Contact timestamps must be in UTC.",
                nameof(updatedAtUtc));
        }

        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The update timestamp must not be earlier than the creation timestamp.",
                nameof(updatedAtUtc));
        }
    }
}
