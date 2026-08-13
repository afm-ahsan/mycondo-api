using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Commands.SetBuildingPrimaryPhoto;

public sealed class SetBuildingPrimaryPhotoCommandHandler(
    IBuildingRepository buildings,
    IAttachmentRepository attachments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetBuildingPrimaryPhotoCommandHandler> logger
) : IRequestHandler<SetBuildingPrimaryPhotoCommand, BuildingDto>
{
    public async ValueTask<BuildingDto> Handle(SetBuildingPrimaryPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);
        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        if (command.AttachmentId is Guid attachmentId)
        {
            Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken)
                ?? throw new NotFoundException(nameof(Attachment), attachmentId);
            if (attachment.TenantId != tenantId
                || attachment.OwnerType != AttachmentOwnerType.Building
                || attachment.OwnerId != command.BuildingId)
            {
                throw new NotFoundException(nameof(Attachment), attachmentId);
            }
        }

        building.SetPrimaryPhoto(command.AttachmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Building {BuildingId} primary photo set to attachment {AttachmentId}, tenant {TenantId}",
            buildingId, command.AttachmentId, tenantId);

        return new BuildingDto(building.Id.Value, building.Name, building.Code, building.Address, building.PrimaryPhotoAttachmentId);
    }
}
