using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;

public sealed class DeactivateBuildingCommandHandler(
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeactivateBuildingCommandHandler> logger
) : IRequestHandler<DeactivateBuildingCommand>
{
    public async ValueTask<Unit> Handle(DeactivateBuildingCommand command, CancellationToken cancellationToken)
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

        building.Deactivate(clock.UtcNow, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Building {BuildingId} deactivated for tenant {TenantId}", buildingId, tenantId);

        return Unit.Value;
    }
}
