using Microsoft.AspNetCore.Mvc;
using Tiketin.Web.Contracts;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Controllers.Api;

/// <summary>Ticket categories with their SLA targets.</summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>Lists all categories with SLA response and resolution targets (minutes).</summary>
    /// <response code="200">Categories returned.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryResponse>>>> List(CancellationToken ct)
    {
        return Ok(new ApiResponse<IReadOnlyList<CategoryResponse>>(await categoryService.GetAllAsync(ct)));
    }
}
