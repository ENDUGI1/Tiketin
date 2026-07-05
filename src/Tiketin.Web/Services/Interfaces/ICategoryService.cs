using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default);
}
