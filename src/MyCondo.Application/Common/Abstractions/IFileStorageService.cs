namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Persists uploaded file bytes and returns an opaque storage key that <see cref="Domain.Features.Attachments.Attachment"/>
/// records as <c>StorageKey</c>. MVP-1 implementation is local disk (see mycondo-docs); the interface
/// intentionally has no path/filesystem concept so a future cloud-storage implementation is a drop-in
/// replacement.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    /// <summary>Returns null if no file exists for <paramref name="storageKey"/>. Content-type is not
    /// re-derived here — callers already have it from the owning <see cref="Domain.Features.Attachments.Attachment"/>
    /// record.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
