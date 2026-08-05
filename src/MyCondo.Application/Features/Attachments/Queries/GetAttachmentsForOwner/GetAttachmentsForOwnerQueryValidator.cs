using FluentValidation;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentsForOwner;

public sealed class GetAttachmentsForOwnerQueryValidator : AbstractValidator<GetAttachmentsForOwnerQuery>
{
    public GetAttachmentsForOwnerQueryValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.OwnerType).Must(BeAValidOwnerType)
            .WithMessage($"OwnerType must be one of: {string.Join(", ", Enum.GetNames<AttachmentOwnerType>())}.");
    }

    private static bool BeAValidOwnerType(string value) => Enum.TryParse<AttachmentOwnerType>(value, out _);
}
