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
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInServiceProvider;

public sealed class CheckInServiceProviderCommandHandler(
    IAccessSessionRepository accessSessions,
    IServiceProviderProfileRepository profiles,
    IServiceProviderAssignmentRepository assignments,
    IFlatRepository flats,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInServiceProviderCommandHandler> logger
) : IRequestHandler<CheckInServiceProviderCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckInServiceProviderCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceProviderProfileId providerId = new(command.ServiceProviderProfileId);
        ServiceProviderProfile provider = await profiles.GetByIdAsync(providerId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);
        if (provider.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);
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

        if (!entryGate.IsActive)
        {
            throw new ConflictException($"Gate '{entryGate.Name}' is not active.");
        }

        if (!entryGate.IsEntryAllowed)
        {
            throw new ConflictException($"Gate '{entryGate.Name}' does not allow entry.");
        }

        if (entryGate.BuildingId != hostFlat.BuildingId)
        {
            throw new ConflictException("Entry gate does not belong to the host flat's building.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        DateTimeOffset nowLocal = DhakaTimeZone.ToLocal(nowUtc);

        List<ServiceProviderAssignment> providerAssignments = await assignments.GetForProviderAsync(tenantId, providerId, cancellationToken);
        bool hasValidAssignment = providerAssignments
            .Where(a => a.FlatId == hostFlatId)
            .Any(a => a.IsCurrentlyValid(nowUtc, nowLocal));

        bool profileEligible = provider.Status == RecurringAccessProfileStatus.Active;

        if (!profileEligible || !hasValidAssignment)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
            {
                string reason = !profileEligible
                    ? $"Provider status is {provider.Status} ({provider.StatusReason})"
                    : "No currently valid assignment for this flat/schedule";
                throw new ForbiddenException($"Service provider cannot enter: {reason}. An override reason is required.");
            }

            if (!currentUser.HasPermission("serviceprovider.override"))
            {
                throw new ForbiddenException("Overriding service provider entry requires the serviceprovider.override permission.");
            }
        }

        AccessSession? open = await accessSessions.GetOpenSessionForServiceProviderAsync(tenantId, providerId, cancellationToken);
        if (open is not null)
        {
            throw new ConflictException("This service provider already has an open (unclosed) visit.");
        }

        AccessSession session = AccessSession.CheckInServiceProvider(
            tenantId, providerId, hostFlatId, entryGateId, currentUser.UserId, command.Remarks,
            command.OverrideReason, nowUtc);

        accessSessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Service provider {ProviderId} checked in via access session {AccessSessionId} for tenant {TenantId}",
            providerId, session.Id, tenantId);

        return session.ToDto();
    }
}
