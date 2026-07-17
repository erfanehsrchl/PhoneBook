namespace PhoneBook.Domain.Contacts;

public record Tag
{
    private const int MaximumLength = 100;

    private Tag(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Tag Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tag is required.", nameof(value));
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength)
        {
            throw new ArgumentException(
                "Tag must not exceed 100 characters.",
                nameof(value));
        }

        return new Tag(normalizedValue);
    }

    public virtual bool Equals(Tag? other)
    {
        return other is not null
            && EqualityContract == other.EqualityContract
            && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            EqualityContract,
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value));
    }
}
