namespace PhoneBook.Domain.Shared;

public record Error(
    string Code,
    string Description,
    ErrorType Type)
{
    public static Error None { get; } = new(
        "Error.None",
        string.Empty,
        ErrorType.Failure);
}
