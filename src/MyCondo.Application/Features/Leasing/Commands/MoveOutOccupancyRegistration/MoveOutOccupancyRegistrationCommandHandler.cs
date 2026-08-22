using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;

namespace MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;

/// <summary>
/// Ends an active occupancy and, in the same transaction, deactivates every household member and ends
/// every active worker/vehicle assignment on the registration — this is the cascade the security-facing
/// view (<c>GetSecurityDirectoryDetailQueryHandler</c>) relies on to stop treating this flat's
/// former occupants, workers, and vehicles as currently authorized (mirrors
/// <c>DomesticWorkerAssignment.Deactivate</c>'s role in removing access eligibility once a worker is no
/// longer engaged). The underlying <c>DomesticWorkerProfile</c>/<c>Vehicle</c> records themselves are
/// never touched — only this registration's claim on them ends.
/// </summary>
public sealed class MoveOutOccupancyRegistrationCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IHouseholdMemberRepository members,
    IOccupancyRegistrationWorkerAssignmentRepository workerAssignments,
    IOccupancyRegistrationVehicleAssignmentRepository vehicleAssignments,
    IOccupancyRegistrationStatusHistoryRepository history,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<MoveOutOccupancyRegistrationCommandHandler> logger
) : IRequestHandler<MoveOutOccupancyRegistrationCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        MoveOutOccupancyRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        OccupancyRegistrationStatus fromStatus = registration.Status;
        registration.MoveOut(command.Reason, clock.UtcNow);

        IReadOnlyList<HouseholdMember> registrationMembers = await members.GetForRegistrationAsync(id, cancellationToken);
        foreach (HouseholdMember member in registrationMembers.Where(m => m.IsActive))
        {
            member.Deactivate();
        }

        IReadOnlyList<OccupancyRegistrationWorkerAssignment> workers =
            await workerAssignments.GetForRegistrationAsync(id, cancellationToken);
        foreach (OccupancyRegistrationWorkerAssignment worker in workers.Where(w => w.IsActive))
        {
            worker.End(clock.UtcNow);
        }

        IReadOnlyList<OccupancyRegistrationVehicleAssignment> vehicles =
            await vehicleAssignments.GetForRegistrationAsync(id, cancellationToken);
        foreach (OccupancyRegistrationVehicleAssignment vehicle in vehicles.Where(v => v.IsActive))
        {
            vehicle.End(clock.UtcNow);
        }

        history.Add(OccupancyRegistrationStatusHistory.Record(
            tenantId, id, fromStatus, registration.Status, currentUser.UserId, command.Reason, clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occupancy registration {OccupancyRegistrationId} moved out: {HouseholdMemberCount} household members, " +
            "{WorkerCount} worker assignments, {VehicleCount} vehicle assignments deactivated, tenant {TenantId}",
            id, registrationMembers.Count, workers.Count, vehicles.Count, tenantId);

        return registration.ToDto();
    }
}
