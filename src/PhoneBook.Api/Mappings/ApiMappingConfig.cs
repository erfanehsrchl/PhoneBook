using Mapster;
using PhoneBook.Api.Contracts;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Application.Contacts.Create;
using PhoneBook.Application.Contacts.Delete;
using PhoneBook.Application.Contacts.GetAll;
using PhoneBook.Application.Contacts.GetById;
using PhoneBook.Application.Contacts.GetByTag;
using PhoneBook.Application.Contacts.Update;

namespace PhoneBook.Api.Mappings;

/// <summary>
/// Configures mappings between HTTP contracts and Application messages.
/// </summary>
public sealed class ApiMappingConfig : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateContactRequest, CreateContactCommand>();

        config.NewConfig<(Guid Id, UpdateContactRequest Request), UpdateContactCommand>()
            .Map(destination => destination.ContactId, source => source.Id)
            .Map(destination => destination.FirstName, source => source.Request.FirstName)
            .Map(destination => destination.LastName, source => source.Request.LastName)
            .Map(destination => destination.PhoneNumber, source => source.Request.PhoneNumber)
            .Map(destination => destination.Tag, source => source.Request.Tag);

        config.NewConfig<Guid, DeleteContactCommand>()
            .Map(destination => destination.ContactId, source => source);

        config.NewConfig<Guid, GetContactByIdQuery>()
            .Map(destination => destination.ContactId, source => source);

        config.NewConfig<(int? PageNumber, int? PageSize), GetContactsQuery>()
            .Map(
                destination => destination.PageNumber,
                source => source.PageNumber ?? PaginationDefaults.PageNumber)
            .Map(
                destination => destination.PageSize,
                source => source.PageSize ?? PaginationDefaults.PageSize);

        config.NewConfig<
                (string Tag, int? PageNumber, int? PageSize),
                GetContactsByTagQuery>()
            .Map(destination => destination.Tag, source => source.Tag)
            .Map(
                destination => destination.PageNumber,
                source => source.PageNumber ?? PaginationDefaults.PageNumber)
            .Map(
                destination => destination.PageSize,
                source => source.PageSize ?? PaginationDefaults.PageSize);

        config.NewConfig(typeof(PagedData<>), typeof(PagedResponse<>));

        config.NewConfig<
                (ContactResponse Data, int StatusCode, string Message),
                ApiResponse<ContactResponse>>()
            .Map(destination => destination.Data, source => source.Data)
            .Map(destination => destination.StatusCode, source => source.StatusCode)
            .Map(destination => destination.Message, source => source.Message);

        config.NewConfig<
                (PagedResponse<ContactResponse> Data, int StatusCode, string Message),
                ApiResponse<PagedResponse<ContactResponse>>>()
            .Map(destination => destination.Data, source => source.Data)
            .Map(destination => destination.StatusCode, source => source.StatusCode)
            .Map(destination => destination.Message, source => source.Message);
    }
}
