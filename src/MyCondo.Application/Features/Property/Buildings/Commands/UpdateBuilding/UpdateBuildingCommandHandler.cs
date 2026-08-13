using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Commands.UpdateBuilding;

public sealed class UpdateBuildingCommandHandler(
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateBuildingCommandHandler> logger
) : IRequestHandler<UpdateBuildingCommand, BuildingDto>
{
    public async ValueTask<BuildingDto> Handle(UpdateBuildingCommand command, CancellationToken cancellationToken)
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

        string name = command.Name.Trim();
        string code = command.Code.Trim().ToUpperInvariant();

        Building? existingByName = await buildings.GetByNameAsync(tenantId, name, cancellationToken);
        if (existingByName is not null && existingByName.Id != buildingId)
        {
            throw new ConflictException($"A building named '{name}' already exists for this tenant.");
        }

        Building? existingByCode = await buildings.GetByCodeAsync(tenantId, code, cancellationToken);
        if (existingByCode is not null && existingByCode.Id != buildingId)
        {
            throw new ConflictException($"A building with code '{code}' already exists for this tenant.");
        }

        building.UpdateDetails(name, code, command.Address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Building {BuildingId} updated for tenant {TenantId}", buildingId, tenantId);

        return new BuildingDto(building.Id.Value, building.Name, building.Code, building.Address, building.PrimaryPhotoAttachmentId);
    }
}
