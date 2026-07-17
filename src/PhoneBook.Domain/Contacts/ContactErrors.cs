using PhoneBook.Domain.Shared;

namespace PhoneBook.Domain.Contacts;

public static class ContactErrors
{
    public static Error IdEmpty { get; } = new(
        "Contact.Id.Empty",
        "Contact ID must not be empty.",
        ErrorType.Validation);

    public static Error IdAlreadyExists { get; } = new(
        "Contact.Id.AlreadyExists",
        "A contact with this ID already exists.",
        ErrorType.Conflict);

    public static Error FirstNameRequired { get; } = new(
        "Contact.FirstName.Required",
        "First name is required.",
        ErrorType.Validation);

    public static Error FirstNameTooLong { get; } = new(
        "Contact.FirstName.TooLong",
        "First name must not exceed 100 characters.",
        ErrorType.Validation);

    public static Error LastNameRequired { get; } = new(
        "Contact.LastName.Required",
        "Last name is required.",
        ErrorType.Validation);

    public static Error LastNameTooLong { get; } = new(
        "Contact.LastName.TooLong",
        "Last name must not exceed 100 characters.",
        ErrorType.Validation);

    public static Error PhoneNumberRequired { get; } = new(
        "Contact.PhoneNumber.Required",
        "Phone number is required.",
        ErrorType.Validation);

    public static Error PhoneNumberInvalid { get; } = new(
        "Contact.PhoneNumber.Invalid",
        "Phone number must be a valid Iranian mobile number.",
        ErrorType.Validation);

    public static Error PhoneNumberAlreadyExists { get; } = new(
        "Contact.PhoneNumber.AlreadyExists",
        "A contact with this phone number already exists.",
        ErrorType.Conflict);

    public static Error TagRequired { get; } = new(
        "Contact.Tag.Required",
        "Tag is required.",
        ErrorType.Validation);

    public static Error TagTooLong { get; } = new(
        "Contact.Tag.TooLong",
        "Tag must not exceed 100 characters.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "Contact.NotFound",
        "Contact was not found.",
        ErrorType.NotFound);

    public static Error TimestampMustBeUtc { get; } = new(
        "Contact.Timestamp.MustBeUtc",
        "Contact timestamps must be in UTC.",
        ErrorType.Validation);

    public static Error TimestampBeforeCreated { get; } = new(
        "Contact.Timestamp.BeforeCreated",
        "The update timestamp must not be earlier than the creation timestamp.",
        ErrorType.Validation);
}
