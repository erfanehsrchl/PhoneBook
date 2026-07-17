using MediatR;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.Delete;

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand>
{
    private readonly IContactRepository _contactRepository;

    public DeleteContactCommandHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task Handle(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        await _contactRepository.DeleteAsync(
            new ContactId(request.ContactId),
            cancellationToken);
    }
}
