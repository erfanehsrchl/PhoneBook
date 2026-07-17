using Mapster;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Application.Common.Mappings;

public sealed class ContactMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Contact, ContactResponse>()
            .Map(destination => destination.Id, source => source.Id.Value)
            .Map(destination => destination.FirstName, source => source.FirstName.Value)
            .Map(destination => destination.LastName, source => source.LastName.Value)
            .Map(destination => destination.PhoneNumber, source => source.PhoneNumber.Value)
            .Map(destination => destination.Tag, source => source.Tag.Value)
            .Map(destination => destination.CreatedAtUtc, source => source.CreatedAtUtc)
            .Map(destination => destination.UpdatedAtUtc, source => source.UpdatedAtUtc);
    }
}
