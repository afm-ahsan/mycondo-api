using FluentValidation;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Commands.RecordAttachment;

public sealed class RecordAttachmentCommandValidator : AbstractValidator<RecordAttachmentCommand>
{
    public RecordAttachmentCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.StorageKey).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.OwnerType).Must(BeAValidOwnerType)
            .WithMessage($"OwnerType must be one of: {string.Join(", ", Enum.GetNames<AttachmentOwnerType>())}.");
    }

    private static bool BeAValidOwnerType(string value) => Enum.TryParse<AttachmentOwnerType>(value, out _);
}
