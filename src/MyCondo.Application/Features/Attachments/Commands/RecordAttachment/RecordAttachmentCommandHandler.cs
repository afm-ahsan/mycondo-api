using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Attachments.Commands.RecordAttachment;

public sealed class RecordAttachmentCommandHandler(
    IAttachmentRepository attachments,
    IResidentRepository residents,
    IOccupancyRegistrationRepository occupancyRegistrations,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordAttachmentCommandHandler> logger
) : IRequestHandler<RecordAttachmentCommand, AttachmentDto>
{
    public async ValueTask<AttachmentDto> Handle(RecordAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AttachmentOwnerType ownerType = Enum.Parse<AttachmentOwnerType>(command.OwnerType);

        await EnsureOwnerExistsForTenantAsync(ownerType, command.OwnerId, tenantId, cancellationToken);

        Attachment attachment = Attachment.Record(
            tenantId, ownerType, command.OwnerId, command.StorageKey, command.FileName, command.ContentType,
            command.SizeBytes, clock.UtcNow);

        attachments.Add(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Attachment {AttachmentId} recorded for {OwnerType} {OwnerId}, tenant {TenantId}",
            attachment.Id, ownerType, command.OwnerId, tenantId);

        return new AttachmentDto(
            attachment.Id.Value, attachment.OwnerType.ToString(), attachment.OwnerId, attachment.StorageKey,
            attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedAtUtc);
    }

    private async Task EnsureOwnerExistsForTenantAsync(
        AttachmentOwnerType ownerType, Guid ownerId, Guid tenantId, CancellationToken cancellationToken)
    {
        switch (ownerType)
        {
            case AttachmentOwnerType.Resident:
                Resident resident = await residents.GetByIdAsync(new ResidentId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(Resident), ownerId);
                if (resident.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(Resident), ownerId);
                }

                break;
            case AttachmentOwnerType.OccupancyRegistration:
                OccupancyRegistration registration = await occupancyRegistrations.GetByIdAsync(
                        new OccupancyRegistrationId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(OccupancyRegistration), ownerId);
                if (registration.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(OccupancyRegistration), ownerId);
                }

                break;
            default:
                throw new NotFoundException(nameof(Attachment), ownerId);
        }
    }
}
