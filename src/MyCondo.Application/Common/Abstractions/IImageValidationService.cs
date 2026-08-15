namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Sniffs an uploaded file's actual bytes against the signature expected for a declared image
/// content-type, guarding against a mislabeled/renamed upload (e.g. a script saved as "photo.png")
/// rather than trusting the client-supplied Content-Type header alone. Deliberately does not accept
/// SVG — it can embed script content, so it is never a valid avatar type regardless of signature.
/// </summary>
public interface IImageValidationService
{
    /// <summary>
    /// Resets <paramref name="content"/>'s position to 0 both before and after inspecting its header,
    /// so the caller can still read the full stream afterward. Requires a seekable stream.
    /// </summary>
    Task<bool> IsValidImageAsync(Stream content, string declaredContentType, CancellationToken cancellationToken);
}
