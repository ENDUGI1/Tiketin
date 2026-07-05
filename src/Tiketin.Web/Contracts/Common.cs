namespace Tiketin.Web.Contracts;

/// <summary>Standard envelope for single resources.</summary>
public record ApiResponse<T>(T Data);

/// <summary>Standard envelope for paginated lists.</summary>
public record PagedResponse<T>(IReadOnlyList<T> Data, PageMeta Meta);

public record PageMeta(int Page, int PageSize, long TotalItems, int TotalPages)
{
    public static PageMeta Create(int page, int pageSize, long totalItems)
        => new(page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
}
