using FluentValidation;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetByTag;

public class GetContactsByTagQueryValidator : AbstractValidator<GetContactsByTagQuery>
{
    public GetContactsByTagQueryValidator()
    {
        RuleFor(query => query.Tag)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tag is required.")
            .MaximumLength(ContactInputConstraints.MaximumTextLength)
            .WithMessage("Tag must not exceed 100 characters.");

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(PaginationDefaults.PageNumber)
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(
                PaginationDefaults.PageNumber,
                PaginationDefaults.MaximumPageSize)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
