using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Contacts.Common;

public static class ContactMappings
{
    public static ContactResponse ToResponse(this Contact contact)
    {
        return new ContactResponse(
            contact.Id.Value,
            contact.FirstName.Value,
            contact.LastName.Value,
            contact.PhoneNumber.Value,
            contact.Tag.Value,
            contact.CreatedAtUtc,
            contact.UpdatedAtUtc);
    }
}
