using FluentValidation;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Auth.Commands.UploadMyAvatar;

public sealed class UploadMyAvatarCommandValidator : AbstractValidator<UploadMyAvatarCommand>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    public UploadMyAvatarCommandValidator(IImageValidationService imageValidation)
    {
        RuleFor(x => x.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage("Only JPEG, PNG, or WebP images are allowed.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("Image must be 5 MB or smaller.");

        // Guards against a mislabeled/renamed upload (e.g. a script saved as "photo.png") — only runs
        // once the declared content-type has already passed the allowlist check above.
        RuleFor(x => x.Content)
            .MustAsync((command, content, ct) => imageValidation.IsValidImageAsync(content, command.ContentType, ct))
            .WithMessage("The uploaded file is not a valid image.")
            .When(x => AllowedContentTypes.Contains(x.ContentType));
    }
}
