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
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInVehicle;

public sealed class CheckInVehicleCommandHandler(
    IAccessSessionRepository accessSessions,
    IVehicleRepository vehicles,
    IFlatRepository flats,
    IGateRepository gates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInVehicleCommandHandler> logger
) : IRequestHandler<CheckInVehicleCommand, AccessSessionDto>
{
    public async ValueTask<AccessSessionDto> Handle(CheckInVehicleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        VehicleId vehicleId = new(command.VehicleId);
        Vehicle vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), command.VehicleId);
        if (vehicle.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Vehicle), command.VehicleId);
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

        FlatId? hostFlatId = null;
        if (command.HostFlatId is Guid rawHostFlatId)
        {
            hostFlatId = new FlatId(rawHostFlatId);
            Flat hostFlat = await flats.GetByIdAsync(hostFlatId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Flat), rawHostFlatId);
            if (hostFlat.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Flat), rawHostFlatId);
            }

            if (entryGate.BuildingId != hostFlat.BuildingId)
            {
                throw new ConflictException("Entry gate does not belong to the host flat's building.");
            }
        }

        if (vehicle.IsBlocked)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
            {
                throw new ForbiddenException($"Vehicle is blocked ({vehicle.BlockReason}); an override reason is required to check in.");
            }

            if (!currentUser.HasPermission("vehicle.override"))
            {
                throw new ForbiddenException("Overriding a blocked vehicle requires the vehicle.override permission.");
            }
        }

        AccessSession? open = await accessSessions.GetOpenSessionForVehicleAsync(tenantId, vehicleId, cancellationToken);
        if (open is not null)
        {
            throw new ConflictException("This vehicle already has an open (unclosed) trip.");
        }

        AccessSession session = AccessSession.CheckInVehicle(
            tenantId, vehicleId, hostFlatId, entryGateId, currentUser.UserId, command.Remarks,
            command.OverrideReason, clock.UtcNow);

        accessSessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Vehicle {VehicleId} checked in via access session {AccessSessionId} for tenant {TenantId}",
            vehicleId, session.Id, tenantId);

        return session.ToDto();
    }
}
