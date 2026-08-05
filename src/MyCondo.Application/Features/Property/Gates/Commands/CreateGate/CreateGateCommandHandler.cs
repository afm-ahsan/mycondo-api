using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

public sealed class CreateGateCommandHandler(
    IGateRepository gates,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateGateCommandHandler> logger
) : IRequestHandler<CreateGateCommand, GateDto>
{
    public async ValueTask<GateDto> Handle(CreateGateCommand command, CancellationToken cancellationToken)
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
        Gate? existing = await gates.GetByNameAsync(tenantId, buildingId, name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A gate named '{name}' already exists for this building.");
        }

        Gate gate = Gate.Create(tenantId, buildingId, name, clock.UtcNow);

        gates.Add(gate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Gate {GateId} '{Name}' created for building {BuildingId}, tenant {TenantId}",
            gate.Id, gate.Name, buildingId, tenantId);

        return new GateDto(gate.Id.Value, gate.BuildingId.Value, gate.Name);
    }
}
