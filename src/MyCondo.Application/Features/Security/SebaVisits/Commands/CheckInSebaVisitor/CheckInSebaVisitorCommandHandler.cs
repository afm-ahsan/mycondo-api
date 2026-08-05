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

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;

public sealed class CheckInSebaVisitorCommandHandler(
    IAccessSessionRepository accessSessions,
    ISebaVisitDetailRepository sebaVisitDetails,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInSebaVisitorCommandHandler> logger
) : IRequestHandler<CheckInSebaVisitorCommand, SebaVisitDto>
{
    public async ValueTask<SebaVisitDto> Handle(CheckInSebaVisitorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GateId entryGateId = new(command.EntryGateId);
        Gate entryGate = await gates.GetByIdAsync(entryGateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Gate), command.EntryGateId);
        if (entryGate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Gate), command.EntryGateId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;

        AccessSession session = AccessSession.CheckInSebaVisitor(
            tenantId, entryGateId, currentUser.UserId, command.DepartmentOrEmployeeToMeet, nowUtc);
        accessSessions.Add(session);

        SebaVisitDetail detail = SebaVisitDetail.Record(
            tenantId, session.Id, command.VisitorFullName, command.VisitorPhone, command.Organization,
            command.DepartmentOrEmployeeToMeet, command.TokenNumber, command.RelatedReferenceType,
            command.RelatedReferenceId, nowUtc);
        sebaVisitDetails.Add(detail);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seba visitor '{VisitorFullName}' checked in via access session {AccessSessionId} for tenant {TenantId}",
            detail.VisitorFullName, session.Id, tenantId);

        return session.ToDto(detail);
    }
}
