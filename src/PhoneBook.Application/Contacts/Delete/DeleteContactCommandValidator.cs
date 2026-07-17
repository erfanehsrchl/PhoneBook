using FluentValidation;

namespace PhoneBook.Application.Contacts.Delete;

public class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(command => command.ContactId)
            .NotEmpty()
            .WithMessage("Contact ID must not be empty.");
    }
}
