using System.ComponentModel.DataAnnotations;

namespace Tiketin.Web.Contracts;

public record SaveKbArticleRequest(
    [Required, MaxLength(150)] string Title,
    [Required] string BodyMarkdown,
    [Required, Range(1, int.MaxValue)] int CategoryId);

public record PublishKbArticleRequest(bool IsPublished);

public class KbListQuery
{
    /// <summary>Full-text search (Indonesian dictionary) over title and body.</summary>
    public string? Search { get; init; }

    public int? Category { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 20;
}

public record KbArticleListItem(
    Guid Id,
    string Title,
    string Slug,
    string CategoryName,
    bool IsPublished,
    int ViewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record KbArticleResponse(
    Guid Id,
    string Title,
    string Slug,
    string BodyMarkdown,
    int CategoryId,
    string CategoryName,
    string AuthorName,
    bool IsPublished,
    int ViewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
