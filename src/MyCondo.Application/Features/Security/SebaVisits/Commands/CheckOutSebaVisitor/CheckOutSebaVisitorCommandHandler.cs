using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.SebaVisits.DTOs;
using MyCondo.Application.Features.Security.SebaVisits.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.SebaVisits;

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckOutSebaVisitor;

public sealed class CheckOutSebaVisitorCommandHandler(
    IAccessSessionRepository accessSessions,
    ISebaVisitDetailRepository sebaVisitDetails,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckOutSebaVisitorCommandHandler> logger
) : IRequestHandler<CheckOutSebaVisitorCommand, SebaVisitDto>
{
    public async ValueTask<SebaVisitDto> Handle(CheckOutSebaVisitorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AccessSessionId id = new(command.AccessSessionId);
        AccessSession session = await accessSessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccessSession), command.AccessSessionId);

        if (session.TenantId != tenantId || session.AccessCategory != AccessCategory.SebaVisitor)
        {
            throw new NotFoundException(nameof(AccessSession), command.AccessSessionId);
        }

        SebaVisitDetail detail = await sebaVisitDetails.GetByAccessSessionIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SebaVisitDetail), command.AccessSessionId);

        GateId exitGateId = new(command.ExitGateId);
        Gate exitGate = await gates.GetByIdAsync(exitGateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Gate), command.ExitGateId);
        if (exitGate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Gate), command.ExitGateId);
        }

        session.CheckOut(exitGateId, currentUser.UserId, clock.UtcNow);
        detail.RecordOutcome(command.ServiceOutcome, command.Acknowledged);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seba visit (access session {AccessSessionId}) checked out for tenant {TenantId}", id, tenantId);

        return session.ToDto(detail);
    }
}
