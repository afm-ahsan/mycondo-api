using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Application.Features.Leasing.Commands.SetOccupancyRegistrationPrimaryPhoto;

public sealed class SetOccupancyRegistrationPrimaryPhotoCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IAttachmentRepository attachments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetOccupancyRegistrationPrimaryPhotoCommandHandler> logger
) : IRequestHandler<SetOccupancyRegistrationPrimaryPhotoCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        SetOccupancyRegistrationPrimaryPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        if (command.AttachmentId is Guid attachmentId)
        {
            Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken)
                ?? throw new NotFoundException(nameof(Attachment), attachmentId);
            if (attachment.TenantId != tenantId
                || attachment.OwnerType != AttachmentOwnerType.OccupancyRegistration
                || attachment.OwnerId != command.OccupancyRegistrationId)
            {
                throw new NotFoundException(nameof(Attachment), attachmentId);
            }
        }

        registration.SetPrimaryPhoto(command.AttachmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occupancy registration {OccupancyRegistrationId} primary photo set to attachment {AttachmentId}, tenant {TenantId}",
            id, command.AttachmentId, tenantId);

        return registration.ToDto();
    }
}
