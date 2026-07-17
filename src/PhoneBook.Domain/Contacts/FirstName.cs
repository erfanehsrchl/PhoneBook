using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public record FirstName
{
    private const int MaximumLength = 100;

    private FirstName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<FirstName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<FirstName>.Failure(ContactErrors.FirstNameRequired);
        }

        string normalizedValue = value.Trim();

        return normalizedValue.Length > MaximumLength
            ? Result<FirstName>.Failure(ContactErrors.FirstNameTooLong)
            : Result<FirstName>.Success(new FirstName(normalizedValue));
    }
}
