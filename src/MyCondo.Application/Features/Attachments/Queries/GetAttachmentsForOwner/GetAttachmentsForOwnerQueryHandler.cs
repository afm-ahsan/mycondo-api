using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentsForOwner;

public sealed class GetAttachmentsForOwnerQueryHandler(
    IAttachmentRepository attachments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAttachmentsForOwnerQuery, List<AttachmentDto>>
{
    public async ValueTask<List<AttachmentDto>> Handle(GetAttachmentsForOwnerQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AttachmentOwnerType ownerType = Enum.Parse<AttachmentOwnerType>(query.OwnerType);

        List<Attachment> ownerAttachments = await attachments.GetForOwnerAsync(
            tenantId, ownerType, query.OwnerId, cancellationToken);

        return ownerAttachments
            .Select(a => new AttachmentDto(
                a.Id.Value, a.OwnerType.ToString(), a.OwnerId, a.StorageKey, a.FileName, a.ContentType,
                a.SizeBytes, a.CreatedAtUtc))
            .ToList();
    }
}
