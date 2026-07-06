using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public partial class KbService(AppDbContext db, TimeProvider clock) : IKbService
{
    public async Task<PagedResponse<KbArticleListItem>> ListAsync(
        UserContext actor, KbListQuery query, CancellationToken ct = default)
    {
        var articles = db.KbArticles.AsNoTracking();

        if (!actor.IsStaff)
        {
            articles = articles.Where(a => a.IsPublished);
        }

        if (query.Category is not null)
        {
            articles = articles.Where(a => a.CategoryId == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Matches the expression GIN index ix_kb_articles_search.
            var term = query.Search.Trim();
            articles = articles.Where(a =>
                EF.Functions.ToTsVector("indonesian", a.Title + " " + a.BodyMarkdown)
                    .Matches(EF.Functions.PlainToTsQuery("indonesian", term)));
        }

        var total = await articles.LongCountAsync(ct);

        var items = await articles
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new KbArticleListItem(
                a.Id, a.Title, a.Slug, a.Category.Name, a.IsPublished,
                a.ViewCount, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResponse<KbArticleListItem>(items, PageMeta.Create(query.Page, query.PageSize, total));
    }

    public async Task<KbArticleResponse> GetBySlugAsync(
        UserContext actor, string slug, CancellationToken ct = default)
    {
        var article = await db.KbArticles
            .Include(a => a.Category)
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Slug == slug, ct);

        if (article is null || (!article.IsPublished && !actor.IsStaff))
        {
            throw new NotFoundException("Artikel tidak ditemukan.");
        }

        article.ViewCount++;
        await db.SaveChangesAsync(ct);

        return Map(article);
    }

    public async Task<KbArticleResponse> GetByIdAsync(
        UserContext actor, Guid id, CancellationToken ct = default)
    {
        EnsureStaff(actor);

        var article = await db.KbArticles.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Artikel tidak ditemukan.");

        return Map(article);
    }

    public async Task<KbArticleResponse> CreateAsync(
        UserContext actor, SaveKbArticleRequest request, CancellationToken ct = default)
    {
        EnsureStaff(actor);
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var article = new KbArticle
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = await UniqueSlugAsync(request.Title, ct),
            BodyMarkdown = request.BodyMarkdown.Trim(),
            CategoryId = request.CategoryId,
            AuthorId = actor.UserId,
            IsPublished = false,
            CreatedAt = clock.GetUtcNow()
        };
        db.KbArticles.Add(article);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(actor, article.Id, ct);
    }

    public async Task<KbArticleResponse> UpdateAsync(
        UserContext actor, Guid id, SaveKbArticleRequest request, CancellationToken ct = default)
    {
        EnsureStaff(actor);
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var article = await db.KbArticles
            .SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Artikel tidak ditemukan.");

        article.Title = request.Title.Trim();
        article.BodyMarkdown = request.BodyMarkdown.Trim();
        article.CategoryId = request.CategoryId;
        article.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(actor, article.Id, ct);
    }

    public async Task SetPublishedAsync(
        UserContext actor, Guid id, bool isPublished, CancellationToken ct = default)
    {
        EnsureStaff(actor);

        var article = await db.KbArticles
            .SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Artikel tidak ditemukan.");

        article.IsPublished = isPublished;
        article.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    // ---------- helpers ----------

    private static void EnsureStaff(UserContext actor)
    {
        if (!actor.IsStaff)
        {
            throw new ForbiddenException("Hanya teknisi dan admin yang bisa mengelola artikel.");
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
        {
            throw new DomainRuleException("Kategori tidak ditemukan.");
        }
    }

    private async Task<string> UniqueSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await db.KbArticles.AnyAsync(a => a.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    public static string Slugify(string title)
    {
        var lowered = title.Trim().ToLowerInvariant();
        var cleaned = NonSlugChars().Replace(lowered, "-");
        var collapsed = Regex.Replace(cleaned, "-{2,}", "-").Trim('-');
        // Slug column is 170; leave room for a uniqueness suffix.
        return collapsed.Length <= 160 ? collapsed : collapsed[..160].TrimEnd('-');
    }

    private static KbArticleResponse Map(KbArticle a) => new(
        a.Id, a.Title, a.Slug, a.BodyMarkdown, a.CategoryId, a.Category.Name,
        a.Author.FullName, a.IsPublished, a.ViewCount, a.CreatedAt, a.UpdatedAt);

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();
}
