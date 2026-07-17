using FluentValidation;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Create;

public class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(ContactInputConstraints.MaximumTextLength);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(ContactInputConstraints.MaximumTextLength);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty();

        RuleFor(command => command.Tag)
            .NotEmpty()
            .MaximumLength(ContactInputConstraints.MaximumTextLength);
    }
}
