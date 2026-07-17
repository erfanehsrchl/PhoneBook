namespace PhoneBook.Domain.Contacts;

public readonly record struct ContactId
{
    public ContactId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Contact ID must not be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static ContactId New()
    {
        return new ContactId(Guid.NewGuid());
    }

}
