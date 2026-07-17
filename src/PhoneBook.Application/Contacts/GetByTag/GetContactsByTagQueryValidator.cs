using FluentValidation;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetByTag;

public class GetContactsByTagQueryValidator : AbstractValidator<GetContactsByTagQuery>
{
    public GetContactsByTagQueryValidator()
    {
        RuleFor(query => query.Tag)
            .NotEmpty()
            .MaximumLength(ContactInputConstraints.MaximumTextLength);
    }
}
