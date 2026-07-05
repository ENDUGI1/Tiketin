using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Controllers.Api;

/// <summary>Knowledge base articles with Postgres full-text search.</summary>
[ApiController]
[Route("api/v1/kb/articles")]
[Produces("application/json")]
public class KbController(IKbService kbService) : ControllerBase
{
    private UserContext Actor => UserContext.FromPrincipal(User);

    /// <summary>Lists articles. `search` runs full-text search (Indonesian dictionary) over title and body.</summary>
    /// <response code="200">Paginated article list. Drafts are visible to staff only.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<KbArticleListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<KbArticleListItem>>> List(
        [FromQuery] KbListQuery query, CancellationToken ct)
    {
        return Ok(await kbService.ListAsync(Actor, query, ct));
    }

    /// <summary>Gets one article by slug and increments its view counter.</summary>
    /// <response code="200">Article returned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Unknown slug, or draft requested by a non-staff caller.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<KbArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<KbArticleResponse>>> GetBySlug(string slug, CancellationToken ct)
    {
        return Ok(new ApiResponse<KbArticleResponse>(await kbService.GetBySlugAsync(Actor, slug, ct)));
    }

    /// <summary>Creates a draft article. Technician/Admin only.</summary>
    /// <response code="201">Article created as draft.</response>
    /// <response code="400">Validation failed or unknown category.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not staff.</response>
    [HttpPost]
    [Authorize(Roles = "Technician,Admin")]
    [ProducesResponseType(typeof(ApiResponse<KbArticleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<KbArticleResponse>>> Create(
        SaveKbArticleRequest request, CancellationToken ct)
    {
        var article = await kbService.CreateAsync(Actor, request, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = article.Slug },
            new ApiResponse<KbArticleResponse>(article));
    }

    /// <summary>Updates an article's title, body, and category. The slug never changes.</summary>
    /// <response code="200">Article updated.</response>
    /// <response code="400">Validation failed or unknown category.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not staff.</response>
    /// <response code="404">Article does not exist.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Technician,Admin")]
    [ProducesResponseType(typeof(ApiResponse<KbArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<KbArticleResponse>>> Update(
        Guid id, SaveKbArticleRequest request, CancellationToken ct)
    {
        return Ok(new ApiResponse<KbArticleResponse>(await kbService.UpdateAsync(Actor, id, request, ct)));
    }

    /// <summary>Publishes or unpublishes an article. Technician/Admin only.</summary>
    /// <response code="204">Publish state changed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not staff.</response>
    /// <response code="404">Article does not exist.</response>
    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = "Technician,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPublished(
        Guid id, PublishKbArticleRequest request, CancellationToken ct)
    {
        await kbService.SetPublishedAsync(Actor, id, request.IsPublished, ct);
        return NoContent();
    }
}
