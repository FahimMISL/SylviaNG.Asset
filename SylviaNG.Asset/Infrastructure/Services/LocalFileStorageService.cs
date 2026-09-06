using RMS.Application.Interfaces;

namespace RMS.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _root = configuration["Rms:AttachmentStorageRoot"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "requisition-attachments");
    }

    public async Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(_root, relativeFolder);
        Directory.CreateDirectory(folder);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(folder, uniqueName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Path.Combine(relativeFolder, uniqueName).Replace('\\', '/');
    }

    public Stream OpenRead(string storagePath)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        return File.OpenRead(fullPath);
    }

    public void Delete(string storagePath)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
