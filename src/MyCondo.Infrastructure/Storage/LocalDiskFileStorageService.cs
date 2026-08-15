using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Settings;

namespace MyCondo.Infrastructure.Storage;

/// <summary>
/// MVP-1 file storage: writes to a directory on the API host's local disk, resolved relative to the
/// content root. Not durable across container redeploys/multi-instance deployments — an accepted,
/// documented limitation until a real object-storage backend is wired (see StorageSettings).
/// </summary>
public sealed class LocalDiskFileStorageService(
    IOptions<StorageSettings> settings,
    IHostEnvironment environment) : IFileStorageService
{
    private string RootPath => Path.IsPathRooted(settings.Value.RootPath)
        ? settings.Value.RootPath
        : Path.Combine(environment.ContentRootPath, settings.Value.RootPath);

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        string extension = Path.GetExtension(fileName);
        string storageKey = $"{Guid.NewGuid():N}{extension}";
        string fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using FileStream destination = new(fullPath, FileMode.CreateNew, FileAccess.Write);
        content.Position = 0;
        await content.CopyToAsync(destination, cancellationToken);

        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        string fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        string fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey) => Path.Combine(RootPath, storageKey);
}
