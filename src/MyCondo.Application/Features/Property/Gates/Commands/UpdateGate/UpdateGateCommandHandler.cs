using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Commands.UpdateGate;

public sealed class UpdateGateCommandHandler(
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateGateCommandHandler> logger
) : IRequestHandler<UpdateGateCommand, GateDto>
{
    public async ValueTask<GateDto> Handle(UpdateGateCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GateId gateId = new(command.GateId);
        Gate gate = await gates.GetByIdAsync(gateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Gate), command.GateId);

        if (gate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Gate), command.GateId);
        }

        string name = command.Name.Trim();
        string code = command.Code.Trim().ToUpperInvariant();

        if (await gates.ExistsByCodeAsync(tenantId, gate.BuildingId, code, gateId, cancellationToken))
        {
            throw new ConflictException($"A gate with code '{code}' already exists for this building.");
        }

        if (await gates.ExistsByNameAsync(tenantId, gate.BuildingId, name, gateId, cancellationToken))
        {
            throw new ConflictException($"A gate named '{name}' already exists for this building.");
        }

        gate.Update(
            name, code, command.Description, command.IsEntryAllowed, command.IsExitAllowed, command.DisplayOrder,
            clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Gate {GateId} updated for tenant {TenantId}", gateId, tenantId);

        return new GateDto(
            gate.Id.Value, gate.BuildingId.Value, gate.Name, gate.Code, gate.Description, gate.IsActive,
            gate.IsEntryAllowed, gate.IsExitAllowed, gate.DisplayOrder);
    }
}
