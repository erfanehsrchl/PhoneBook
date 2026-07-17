using FluentValidation;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.Update;

public class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.ContactId)
            .NotEmpty();

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
