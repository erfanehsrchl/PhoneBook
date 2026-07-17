using MediatR;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Contacts.Common;
using PhoneBook.Application.Contacts.Create;
using PhoneBook.Application.Contacts.Delete;
using PhoneBook.Application.Contacts.GetById;
using PhoneBook.Application.Contacts.GetByTag;
using PhoneBook.Application.Contacts.Update;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Api.Controllers;

/// <summary>
/// Exposes HTTP operations for contacts.
/// </summary>
[ApiController]
[Route("api/contacts")]
public class ContactsController : ApiController
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
    /// <returns>The created contact or a ProblemDetails response.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        CreateContactCommand command = new(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Tag);
        Result<ContactResponse> result = await _sender.Send(command, cancellationToken);

        return result.IsFailure
            ? Problem(result)
            : CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.Id },
                result.Value);
    }

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="request">The updated contact input.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated contact or a ProblemDetails response.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
        Result<ContactResponse> result = await _sender.Send(command, cancellationToken);

        return result.IsFailure
            ? Problem(result)
            : Ok(result.Value);
    }

    /// <summary>
    /// Deletes a contact.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content or a ProblemDetails response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result result = await _sender.Send(
            new DeleteContactCommand(id),
            cancellationToken);

        return result.IsFailure
            ? Problem(result)
            : NoContent();
    }

    /// <summary>
    /// Gets a contact by identifier.
    /// </summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The contact or a ProblemDetails response.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ContactResponse> result = await _sender.Send(
            new GetContactByIdQuery(id),
            cancellationToken);

        return result.IsFailure
            ? Problem(result)
            : Ok(result.Value);
    }

    /// <summary>
    /// Gets contacts matching a tag.
    /// </summary>
    /// <param name="tag">The tag to match.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The ordered matching contacts or a ProblemDetails response.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByTag(
        [FromQuery] string? tag,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyCollection<ContactResponse>> result = await _sender.Send(
            new GetContactsByTagQuery(tag),
            cancellationToken);

        return result.IsFailure
            ? Problem(result)
            : Ok(result.Value);
    }
}
