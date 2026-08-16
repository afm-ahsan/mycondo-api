using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutVehicle;

public sealed class CheckOutVehicleCommandHandler(
    IAccessSessionRepository accessSessions,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckOutVehicleCommandHandler> logger
) : IRequestHandler<CheckOutVehicleCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckOutVehicleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AccessSessionId id = new(command.AccessSessionId);
        AccessSession session = await accessSessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccessSession), command.AccessSessionId);

        if (session.TenantId != tenantId || session.AccessCategory != AccessCategory.Vehicle)
        {
            throw new NotFoundException(nameof(AccessSession), command.AccessSessionId);
        }

        GateId exitGateId = new(command.ExitGateId);
        Gate exitGate = await gates.GetByIdAsync(exitGateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Gate), command.ExitGateId);
        if (exitGate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Gate), command.ExitGateId);
        }

        if (!exitGate.IsActive)
        {
            throw new ConflictException($"Gate '{exitGate.Name}' is not active.");
        }

        if (!exitGate.IsExitAllowed)
        {
            throw new ConflictException($"Gate '{exitGate.Name}' does not allow exit.");
        }

        session.CheckOut(exitGateId, currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Access session {AccessSessionId} (vehicle) checked out for tenant {TenantId}", id, tenantId);

        return session.ToDto();
    }
}
