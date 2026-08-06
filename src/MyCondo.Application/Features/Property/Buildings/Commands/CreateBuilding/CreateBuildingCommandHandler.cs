using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;

public sealed class CreateBuildingCommandHandler(
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateBuildingCommandHandler> logger
) : IRequestHandler<CreateBuildingCommand, CreateBuildingResult>
{
    public async ValueTask<CreateBuildingResult> Handle(CreateBuildingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string name = command.Name.Trim();
        string code = command.Code.Trim().ToUpperInvariant();

        Building? existingByName = await buildings.GetByNameAsync(tenantId, name, cancellationToken);
        if (existingByName is not null)
        {
            throw new ConflictException($"A building named '{name}' already exists for this tenant.");
        }

        Building? existingByCode = await buildings.GetByCodeAsync(tenantId, code, cancellationToken);
        if (existingByCode is not null)
        {
            throw new ConflictException($"A building with code '{code}' already exists for this tenant.");
        }

        Building building = Building.Create(tenantId, name, code, command.Address, clock.UtcNow);

        buildings.Add(building);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Building {BuildingId} '{Name}' created for tenant {TenantId}", building.Id, building.Name, tenantId);

        return new CreateBuildingResult(building.Id.Value, building.Name, building.Code, building.Address);
    }
}
