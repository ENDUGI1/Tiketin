using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;

namespace Tiketin.Web.Services.Interfaces;

public interface IKbService
{
    /// <summary>
    /// Lists articles. Full-text search uses the Postgres GIN index (Indonesian
    /// dictionary). Non-staff only ever see published articles.
    /// </summary>
    Task<PagedResponse<KbArticleListItem>> ListAsync(UserContext actor, KbListQuery query, CancellationToken ct = default);

    /// <summary>Loads an article by slug and increments its view counter.</summary>
    /// <exception cref="Domain.NotFoundException">Unknown slug, or unpublished for a non-staff caller.</exception>
    Task<KbArticleResponse> GetBySlugAsync(UserContext actor, string slug, CancellationToken ct = default);

    /// <summary>Staff only. Creates a draft article; the slug is derived from the title.</summary>
    Task<KbArticleResponse> CreateAsync(UserContext actor, SaveKbArticleRequest request, CancellationToken ct = default);

    /// <summary>Staff only. Updates title, body, and category. The slug is stable.</summary>
    Task<KbArticleResponse> UpdateAsync(UserContext actor, Guid id, SaveKbArticleRequest request, CancellationToken ct = default);

    /// <summary>Staff only. Publishes or unpublishes an article.</summary>
    Task SetPublishedAsync(UserContext actor, Guid id, bool isPublished, CancellationToken ct = default);

    /// <summary>Staff only. Loads an article by id for editing (no view count increment).</summary>
    Task<KbArticleResponse> GetByIdAsync(UserContext actor, Guid id, CancellationToken ct = default);
}
