using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentContent;

public sealed class GetAttachmentContentQueryHandler(
    IAttachmentRepository attachments,
    IFileStorageService fileStorage,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAttachmentContentQuery, AttachmentContentDto?>
{
    public async ValueTask<AttachmentContentDto?> Handle(GetAttachmentContentQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        Attachment? attachment = await attachments.GetByIdAsync(new AttachmentId(query.AttachmentId), cancellationToken);
        if (attachment is null || attachment.TenantId != tenantId)
        {
            return null;
        }

        Stream? content = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return content is null ? null : new AttachmentContentDto(content, attachment.ContentType, attachment.FileName);
    }
}
