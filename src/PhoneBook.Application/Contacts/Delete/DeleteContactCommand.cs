using PhoneBook.Application.Abstractions.Messaging;

namespace PhoneBook.Application.Contacts.Delete;

public record DeleteContactCommand(Guid ContactId) : ICommand;
