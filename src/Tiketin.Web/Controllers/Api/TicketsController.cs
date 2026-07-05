using Microsoft.AspNetCore.Mvc;
using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Controllers.Api;

/// <summary>Ticket lifecycle: create, list, detail, comments, attachments, rating, audit trail.</summary>
[ApiController]
[Route("api/v1/tickets")]
[Produces("application/json")]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    private UserContext Actor => UserContext.FromPrincipal(User);

    /// <summary>Lists tickets. Employees see only their own; technicians and admins see all.</summary>
    /// <response code="200">Paginated ticket list.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TicketListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<TicketListItem>>> List(
        [FromQuery] TicketListQuery query, CancellationToken ct)
    {
        return Ok(await ticketService.ListAsync(Actor, query, ct));
    }

    /// <summary>Creates a new ticket for the authenticated user.</summary>
    /// <response code="201">Ticket created; body contains the full ticket.</response>
    /// <response code="400">Validation failed or unknown category.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TicketDetailResponse>>> Create(
        CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await ticketService.CreateAsync(Actor, request, ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, new ApiResponse<TicketDetailResponse>(ticket));
    }

    /// <summary>Gets one ticket with comments and attachments.</summary>
    /// <response code="200">Ticket returned. Internal notes are omitted for the reporter.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Employee requesting a ticket that is not theirs.</response>
    /// <response code="404">Ticket does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetailResponse>>> Get(Guid id, CancellationToken ct)
    {
        return Ok(new ApiResponse<TicketDetailResponse>(await ticketService.GetAsync(Actor, id, ct)));
    }

    /// <summary>Adds a comment. `isInternal` is allowed for technicians and admins only.</summary>
    /// <response code="201">Comment added.</response>
    /// <response code="400">Validation failed or ticket is closed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">No access to this ticket, or internal note without staff role.</response>
    /// <response code="404">Ticket does not exist.</response>
    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CommentResponse>>> AddComment(
        Guid id, AddCommentRequest request, CancellationToken ct)
    {
        var comment = await ticketService.AddCommentAsync(Actor, id, request, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<CommentResponse>(comment));
    }

    /// <summary>Uploads an attachment (multipart/form-data). Max 5 MB; images and PDF only.</summary>
    /// <response code="201">Attachment stored.</response>
    /// <response code="400">File missing, too large, or unsupported type.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">No access to this ticket.</response>
    /// <response code="404">Ticket does not exist.</response>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(ApiResponse<AttachmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AttachmentResponse>>> AddAttachment(
        Guid id, IFormFile? file, [FromForm] Guid? commentId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File wajib diisi.");
        }

        await using var stream = file.OpenReadStream();
        var attachment = await ticketService.AddAttachmentAsync(
            Actor, id, stream, file.FileName, file.ContentType, file.Length, commentId, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<AttachmentResponse>(attachment));
    }

    private const long MaxUploadBytes = 6 * 1024 * 1024; // 5 MB payload + multipart overhead

    /// <summary>Downloads an attachment.</summary>
    /// <response code="200">File stream.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">No access to this ticket or attachment.</response>
    /// <response code="404">Ticket or attachment does not exist.</response>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var (content, fileName, contentType) = await ticketService.OpenAttachmentAsync(Actor, id, attachmentId, ct);
        return File(content, contentType, fileName);
    }

    /// <summary>Reporter rates a resolved or closed ticket (1 to 5), once.</summary>
    /// <response code="204">Rating stored.</response>
    /// <response code="400">Ticket not resolved yet or already rated.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not the reporter.</response>
    /// <response code="404">Ticket does not exist.</response>
    [HttpPost("{id:guid}/rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rate(Guid id, RateTicketRequest request, CancellationToken ct)
    {
        await ticketService.RateAsync(Actor, id, request, ct);
        return NoContent();
    }

    /// <summary>Returns the ticket audit trail (created, status changes, assignments, comments).</summary>
    /// <response code="200">Events, oldest first.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">No access to this ticket.</response>
    /// <response code="404">Ticket does not exist.</response>
    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TicketEventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketEventResponse>>>> Events(
        Guid id, CancellationToken ct)
    {
        return Ok(new ApiResponse<IReadOnlyList<TicketEventResponse>>(
            await ticketService.GetEventsAsync(Actor, id, ct)));
    }
}
