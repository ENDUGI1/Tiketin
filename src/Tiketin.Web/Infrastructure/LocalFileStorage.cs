using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Infrastructure;

/// <summary>
/// Stores attachments on local disk under the configured root, partitioned by
/// year/month. Storage paths are relative keys like "2026/07/{guid}.png".
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["Storage:Root"] ?? "storage";
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var key = Path.Combine(now.ToString("yyyy"), now.ToString("MM"), $"{Guid.NewGuid():N}{extension}");

        var fullPath = Path.Combine(_root, key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, ct);

        // Normalized to forward slashes so keys are portable across hosts.
        return key.Replace('\\', '/');
    }

    public Stream OpenRead(string storagePath)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        return File.OpenRead(fullPath);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
