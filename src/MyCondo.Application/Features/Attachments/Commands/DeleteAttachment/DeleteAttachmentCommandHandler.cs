using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachments,
    IFileStorageService fileStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeleteAttachmentCommandHandler> logger
) : IRequestHandler<DeleteAttachmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(command.AttachmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), command.AttachmentId);
        if (attachment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Attachment), command.AttachmentId);
        }

        attachment.Delete(clock.UtcNow, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(attachment.StorageKey, cancellationToken);

        logger.LogInformation("Attachment {AttachmentId} deleted, tenant {TenantId}", attachment.Id, tenantId);

        return Unit.Value;
    }
}
