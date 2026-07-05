using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Categories.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.SlaResponseMinutes, c.SlaResolutionMinutes))
            .ToListAsync(ct);
    }
}
