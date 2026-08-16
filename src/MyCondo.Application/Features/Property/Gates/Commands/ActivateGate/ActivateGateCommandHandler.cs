using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;

public sealed class ActivateGateCommandHandler(
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ActivateGateCommandHandler> logger
) : IRequestHandler<ActivateGateCommand>
{
    public async ValueTask<Unit> Handle(ActivateGateCommand command, CancellationToken cancellationToken)
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

        gate.Activate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Gate {GateId} activated for tenant {TenantId}", gateId, tenantId);

        return Unit.Value;
    }
}
