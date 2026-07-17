using FluentValidation;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.Create;

public class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(ContactInputConstraints.MaximumTextLength)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(command => command.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(ContactInputConstraints.MaximumTextLength)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(command => command.PhoneNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Must(PhoneNumber.IsValid)
            .WithMessage("Phone number must be a valid Iranian mobile number.");

        RuleFor(command => command.Tag)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tag is required.")
            .MaximumLength(ContactInputConstraints.MaximumTextLength)
            .WithMessage("Tag must not exceed 100 characters.");
    }
}
