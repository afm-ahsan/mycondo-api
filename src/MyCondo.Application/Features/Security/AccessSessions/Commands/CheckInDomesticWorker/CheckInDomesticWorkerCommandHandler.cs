using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInDomesticWorker;

public sealed class CheckInDomesticWorkerCommandHandler(
    IAccessSessionRepository accessSessions,
    IDomesticWorkerProfileRepository profiles,
    IDomesticWorkerAssignmentRepository assignments,
    IFlatRepository flats,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInDomesticWorkerCommandHandler> logger
) : IRequestHandler<CheckInDomesticWorkerCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckInDomesticWorkerCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DomesticWorkerProfileId workerId = new(command.DomesticWorkerProfileId);
        DomesticWorkerProfile worker = await profiles.GetByIdAsync(workerId, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        if (worker.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        }

        FlatId hostFlatId = new(command.HostFlatId);
        Flat hostFlat = await flats.GetByIdAsync(hostFlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.HostFlatId);
        if (hostFlat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.HostFlatId);
        }

        GateId entryGateId = new(command.EntryGateId);
        Gate entryGate = await gates.GetByIdAsync(entryGateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Gate), command.EntryGateId);
        if (entryGate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Gate), command.EntryGateId);
        }

        if (entryGate.BuildingId != hostFlat.BuildingId)
        {
            throw new ConflictException("Entry gate does not belong to the host flat's building.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        DateTimeOffset nowLocal = DhakaTimeZone.ToLocal(nowUtc);

        List<DomesticWorkerAssignment> workerAssignments = await assignments.GetForWorkerAsync(tenantId, workerId, cancellationToken);
        bool hasValidAssignment = workerAssignments
            .Where(a => a.FlatId == hostFlatId)
            .Any(a => a.IsCurrentlyValid(nowUtc, nowLocal));

        bool profileEligible = worker.Status == RecurringAccessProfileStatus.Active;

        if (!profileEligible || !hasValidAssignment)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
            {
                string reason = !profileEligible
                    ? $"Worker status is {worker.Status} ({worker.StatusReason})"
                    : "No currently valid assignment for this flat/schedule";
                throw new ForbiddenException($"Domestic worker cannot enter: {reason}. An override reason is required.");
            }

            if (!currentUser.HasPermission("domesticworker.override"))
            {
                throw new ForbiddenException("Overriding domestic worker entry requires the domesticworker.override permission.");
            }
        }

        AccessSession? open = await accessSessions.GetOpenSessionForDomesticWorkerAsync(tenantId, workerId, cancellationToken);
        if (open is not null)
        {
            throw new ConflictException("This domestic worker already has an open (unclosed) visit.");
        }

        AccessSession session = AccessSession.CheckInDomesticWorker(
            tenantId, workerId, hostFlatId, entryGateId, currentUser.UserId, command.Remarks,
            command.OverrideReason, nowUtc);

        accessSessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Domestic worker {WorkerId} checked in via access session {AccessSessionId} for tenant {TenantId}",
            workerId, session.Id, tenantId);

        return session.ToDto();
    }
}
