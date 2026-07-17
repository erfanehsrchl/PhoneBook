using System.Text;

namespace PhoneBook.Domain.Contacts;

public record PhoneNumber
{
    private const int CanonicalLength = 13;
    private const int CountryPrefixLength = 4;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PhoneNumber Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Phone number is required.", nameof(value));
        }

        string normalizedValue = Normalize(value.Trim());

        if (!IsCanonical(normalizedValue))
        {
            throw new ArgumentException(
                "Phone number must be a valid Iranian mobile number.",
                nameof(value));
        }

        return new PhoneNumber(normalizedValue);
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && IsCanonical(Normalize(value.Trim()));
    }

    private static string Normalize(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char character in value)
        {
            if (character is ' ' or '-' or '(' or ')')
            {
                continue;
            }

            if (character is >= '\u06F0' and <= '\u06F9')
            {
                builder.Append((char)('0' + character - '\u06F0'));
                continue;
            }

            if (character is >= '\u0660' and <= '\u0669')
            {
                builder.Append((char)('0' + character - '\u0660'));
                continue;
            }

            builder.Append(character);
        }

        string digits = builder.ToString();

        if (digits.StartsWith("0098", StringComparison.Ordinal))
        {
            return $"+98{digits[4..]}";
        }

        if (digits.StartsWith("98", StringComparison.Ordinal))
        {
            return $"+98{digits[2..]}";
        }

        if (digits.StartsWith("09", StringComparison.Ordinal))
        {
            return $"+989{digits[2..]}";
        }

        return digits;
    }

    private static bool IsCanonical(string value)
    {
        return value.Length == CanonicalLength
            && value.StartsWith("+989", StringComparison.Ordinal)
            && value[CountryPrefixLength..].All(char.IsAsciiDigit);
    }
}
