namespace Tiketin.Web.Services.Interfaces;

/// <summary>Abstraction over attachment storage (local disk in dev, swappable later).</summary>
public interface IFileStorage
{
    /// <summary>Persists a file and returns its storage path (opaque key).</summary>
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);

    /// <summary>Opens a stored file for reading.</summary>
    Stream OpenRead(string storagePath);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
