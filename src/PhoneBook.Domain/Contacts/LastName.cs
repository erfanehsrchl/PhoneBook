namespace PhoneBook.Domain.Contacts;

public record LastName
{
    private const int MaximumLength = 100;

    private LastName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LastName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Last name is required.", nameof(value));
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength)
        {
            throw new ArgumentException(
                "Last name must not exceed 100 characters.",
                nameof(value));
        }

        return new LastName(normalizedValue);
    }
}
