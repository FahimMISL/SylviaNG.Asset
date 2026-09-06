namespace RMS.Application.Interfaces;

/// <summary>
/// US-007 minimal storage abstraction: save a stream, get it back by the path
/// this returned. Local-disk implementation for now - real object storage
/// (MinIO, etc.) is Feature 16's job; callers only depend on this interface.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken cancellationToken = default);
    Stream OpenRead(string storagePath);
    void Delete(string storagePath);
}
