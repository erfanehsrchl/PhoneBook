using FluentValidation;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Application.Contacts.GetAll;

public class GetContactsQueryValidator : AbstractValidator<GetContactsQuery>
{
    public GetContactsQueryValidator()
    {
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
