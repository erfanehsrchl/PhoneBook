using FluentValidation;

namespace PhoneBook.Application.Contacts.GetById;

public class GetContactByIdQueryValidator : AbstractValidator<GetContactByIdQuery>
{
    public GetContactByIdQueryValidator()
    {
        RuleFor(query => query.ContactId)
            .NotEmpty();
    }
}
