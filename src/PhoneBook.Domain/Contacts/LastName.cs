using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public record LastName
{
    private const int MaximumLength = 100;

    private LastName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<LastName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<LastName>.Failure(ContactErrors.LastNameRequired);
        }

        string normalizedValue = value.Trim();

        return normalizedValue.Length > MaximumLength
            ? Result<LastName>.Failure(ContactErrors.LastNameTooLong)
            : Result<LastName>.Success(new LastName(normalizedValue));
    }
}
