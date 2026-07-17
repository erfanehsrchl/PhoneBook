namespace PhoneBook.Domain.Contacts;

public record FirstName
{
    private const int MaximumLength = 100;

    private FirstName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static FirstName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("First name is required.", nameof(value));
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength)
        {
            throw new ArgumentException(
                "First name must not exceed 100 characters.",
                nameof(value));
        }

        return new FirstName(normalizedValue);
    }
}
