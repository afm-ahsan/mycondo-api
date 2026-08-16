using FluentValidation;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Commands.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    private const string PdfContentType = "application/pdf";
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public UploadAttachmentCommandValidator(IImageValidationService imageValidation)
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.OwnerType).Must(BeAValidOwnerType)
            .WithMessage($"OwnerType must be one of: {string.Join(", ", Enum.GetNames<AttachmentOwnerType>())}.");

        RuleFor(x => x.ContentType)
            .Must(contentType => AllowedImageContentTypes.Contains(contentType) || contentType.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF, JPEG, PNG, or WebP files are allowed.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("File must be 10 MB or smaller.");

        // Guards against a mislabeled/renamed upload (e.g. a script saved as "photo.png") — only runs
        // once the declared content-type has already passed the allowlist check above.
        RuleFor(x => x.Content)
            .MustAsync((command, content, ct) => imageValidation.IsValidImageAsync(content, command.ContentType, ct))
            .WithMessage("The uploaded file is not a valid image.")
            .When(x => AllowedImageContentTypes.Contains(x.ContentType));

        RuleFor(x => x.Content)
            .MustAsync(HasPdfSignatureAsync)
            .WithMessage("The uploaded file is not a valid PDF.")
            .When(x => x.ContentType.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase));
    }

    private static bool BeAValidOwnerType(string value) => Enum.TryParse<AttachmentOwnerType>(value, out _);

    private static async Task<bool> HasPdfSignatureAsync(Stream content, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            return false;
        }

        content.Position = 0;
        byte[] header = new byte[PdfSignature.Length];
        int read = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        content.Position = 0;

        return read == PdfSignature.Length && header.AsSpan().SequenceEqual(PdfSignature);
    }
}
