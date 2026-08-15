using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Common.Services;

public sealed class ImageValidationService : IImageValidationService
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public async Task<bool> IsValidImageAsync(
        Stream content, string declaredContentType, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            return false;
        }

        byte[] header = new byte[12];
        content.Position = 0;
        int bytesRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        content.Position = 0;

        return declaredContentType switch
        {
            "image/jpeg" => Matches(header, bytesRead, JpegSignature),
            "image/png" => Matches(header, bytesRead, PngSignature),
            "image/webp" => IsWebP(header, bytesRead),
            _ => false,
        };
    }

    private static bool Matches(byte[] header, int bytesRead, byte[] signature) =>
        bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static bool IsWebP(byte[] header, int bytesRead) =>
        bytesRead >= 12
        && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
        && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
}
