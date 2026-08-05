using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutDomesticWorker;

public sealed class CheckOutDomesticWorkerCommandHandler(
    IAccessSessionRepository accessSessions,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckOutDomesticWorkerCommandHandler> logger
) : IRequestHandler<CheckOutDomesticWorkerCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckOutDomesticWorkerCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AccessSessionId id = new(command.AccessSessionId);
        AccessSession session = await accessSessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccessSession), command.AccessSessionId);

        if (session.TenantId != tenantId || session.AccessCategory != AccessCategory.DomesticWorker)
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

        session.CheckOut(exitGateId, currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Access session {AccessSessionId} (domestic worker) checked out for tenant {TenantId}", id, tenantId);

        return session.ToDto();
    }
}
