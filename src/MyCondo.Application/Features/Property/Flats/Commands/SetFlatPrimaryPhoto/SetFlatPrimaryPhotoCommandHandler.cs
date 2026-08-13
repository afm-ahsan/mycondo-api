using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;

public sealed class SetFlatPrimaryPhotoCommandHandler(
    IFlatRepository flats,
    IAttachmentRepository attachments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetFlatPrimaryPhotoCommandHandler> logger
) : IRequestHandler<SetFlatPrimaryPhotoCommand, FlatDto>
{
    public async ValueTask<FlatDto> Handle(SetFlatPrimaryPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        if (command.AttachmentId is Guid attachmentId)
        {
            Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken)
                ?? throw new NotFoundException(nameof(Attachment), attachmentId);
            if (attachment.TenantId != tenantId
                || attachment.OwnerType != AttachmentOwnerType.Flat
                || attachment.OwnerId != command.FlatId)
            {
                throw new NotFoundException(nameof(Attachment), attachmentId);
            }
        }

        flat.SetPrimaryPhoto(command.AttachmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Flat {FlatId} primary photo set to attachment {AttachmentId}, tenant {TenantId}",
            flatId, command.AttachmentId, tenantId);

        return new FlatDto(
            flat.Id.Value, flat.BuildingId.Value, flat.FlatNumber, flat.FloorNumber, flat.FlatType.ToString(),
            flat.AreaSqFt, flat.PrimaryPhotoAttachmentId);
    }
}
