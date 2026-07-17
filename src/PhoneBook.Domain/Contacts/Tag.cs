using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public record Tag
{
    private const int MaximumLength = 100;

    private Tag(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Tag> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Tag>.Failure(ContactErrors.TagRequired);
        }

        string normalizedValue = value.Trim();

        return normalizedValue.Length > MaximumLength
            ? Result<Tag>.Failure(ContactErrors.TagTooLong)
            : Result<Tag>.Success(new Tag(normalizedValue));
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
