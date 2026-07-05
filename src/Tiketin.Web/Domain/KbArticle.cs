namespace Tiketin.Web.Domain;

public class KbArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BodyMarkdown { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
