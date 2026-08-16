using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;

public sealed class DeactivateGateCommandHandler(
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeactivateGateCommandHandler> logger
) : IRequestHandler<DeactivateGateCommand>
{
    public async ValueTask<Unit> Handle(DeactivateGateCommand command, CancellationToken cancellationToken)
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

        gate.Deactivate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Gate {GateId} deactivated for tenant {TenantId}", gateId, tenantId);

        return Unit.Value;
    }
}
