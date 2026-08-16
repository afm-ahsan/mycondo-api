using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInGuest;

public sealed class CheckInGuestCommandHandler(
    IAccessSessionRepository accessSessions,
    IGuestProfileRepository guestProfiles,
    IFlatRepository flats,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInGuestCommandHandler> logger
) : IRequestHandler<CheckInGuestCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckInGuestCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GuestProfileId guestProfileId = new(command.GuestProfileId);
        GuestProfile guest = await guestProfiles.GetByIdAsync(guestProfileId, cancellationToken)
            ?? throw new NotFoundException(nameof(GuestProfile), command.GuestProfileId);
        if (guest.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GuestProfile), command.GuestProfileId);
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

        if (guest.IsBlocked)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
            {
                throw new ForbiddenException($"Guest is blocked ({guest.BlockReason}); an override reason is required to check in.");
            }

            if (!currentUser.HasPermission("visitor.override"))
            {
                throw new ForbiddenException("Overriding a blocked guest requires the visitor.override permission.");
            }
        }

        AccessSession? open = await accessSessions.GetOpenSessionForGuestAsync(tenantId, guestProfileId, cancellationToken);
        if (open is not null)
        {
            throw new ConflictException("This guest already has an open (unclosed) visit.");
        }

        AccessSession session = AccessSession.CheckInGuest(
            tenantId, guestProfileId, hostFlatId, command.PurposeOfVisit, entryGateId, currentUser.UserId,
            command.PassOrQrNumber, command.Remarks, command.OverrideReason, clock.UtcNow);

        accessSessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Guest {GuestProfileId} checked in via access session {AccessSessionId} for tenant {TenantId}",
            guestProfileId, session.Id, tenantId);

        return session.ToDto();
    }
}
