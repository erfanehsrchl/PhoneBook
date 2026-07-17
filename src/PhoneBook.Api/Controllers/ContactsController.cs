using MediatR;
using Microsoft.AspNetCore.Mvc;
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

namespace PhoneBook.Api.Controllers;

/// <summary>
/// Exposes HTTP operations for contacts.
/// </summary>
[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactsController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR request sender.</param>
    public ContactsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a contact.
    /// </summary>
    /// <param name="request">The contact input.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created contact or an API error response.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        CreateContactCommand command = new(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Tag);
        ContactResponse response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            new ApiResponse<ContactResponse>(
                response,
                StatusCodes.Status201Created,
                "Contact created successfully."));
    }

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="request">The updated contact input.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated contact or an API error response.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateContactRequest request,
        CancellationToken cancellationToken)
    {
        UpdateContactCommand command = new(
            id,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Tag);
        ContactResponse response = await _sender.Send(command, cancellationToken);

        return Ok(new ApiResponse<ContactResponse>(
            response,
            StatusCodes.Status200OK,
            "Contact updated successfully."));
    }

    /// <summary>
    /// Deletes a contact.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content or an API error response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new DeleteContactCommand(id),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Gets a contact by identifier.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The contact or an API error response.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        ContactResponse response = await _sender.Send(
            new GetContactByIdQuery(id),
            cancellationToken);

        return Ok(new ApiResponse<ContactResponse>(
            response,
            StatusCodes.Status200OK,
            "Contact retrieved successfully."));
    }

    /// <summary>
    /// Gets all contacts using deterministic pagination.
    /// </summary>
    /// <param name="pageNumber">The one-based page number. Defaults to 1.</param>
    /// <param name="pageSize">The number of contacts per page. Defaults to 20.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>An ordered page of contacts or an API error response.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ContactResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        GetContactsQuery query = new(
            pageNumber ?? PaginationDefaults.PageNumber,
            pageSize ?? PaginationDefaults.PageSize);
        PagedData<ContactResponse> response = await _sender.Send(
            query,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<ContactResponse>>(
            ToPagedResponse(response),
            StatusCodes.Status200OK,
            "Contacts retrieved successfully."));
    }

    /// <summary>
    /// Gets contacts matching a tag using deterministic pagination.
    /// </summary>
    /// <param name="tag">The tag to match.</param>
    /// <param name="pageNumber">The one-based page number. Defaults to 1.</param>
    /// <param name="pageSize">The number of contacts per page. Defaults to 20.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>An ordered page of matching contacts or an API error response.</returns>
    [HttpGet("by-tag/{tag}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ContactResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByTag(
        string tag,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        GetContactsByTagQuery query = new(
            tag,
            pageNumber ?? PaginationDefaults.PageNumber,
            pageSize ?? PaginationDefaults.PageSize);
        PagedData<ContactResponse> response = await _sender.Send(
            query,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<ContactResponse>>(
            ToPagedResponse(response),
            StatusCodes.Status200OK,
            "Contacts retrieved successfully."));
    }

    private static PagedResponse<T> ToPagedResponse<T>(PagedData<T> page)
    {
        return new PagedResponse<T>(
            page.Items,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.HasPreviousPage,
            page.HasNextPage);
    }
}
