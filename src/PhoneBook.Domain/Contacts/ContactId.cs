using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public readonly record struct ContactId
{
    private ContactId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ContactId New()
    {
        return new ContactId(Guid.NewGuid());
    }

    public static Result<ContactId> Create(Guid value)
    {
        return value == Guid.Empty
            ? Result<ContactId>.Failure(ContactErrors.IdEmpty)
            : Result<ContactId>.Success(new ContactId(value));
    }
}
