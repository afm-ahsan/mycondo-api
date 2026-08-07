using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Leasing.Commands.AssignVehicleToOccupancyRegistration;

/// <summary>Links an existing <see cref="Vehicle"/> — created via the Security module's own
/// <c>POST /api/v1/vehicles</c>, never duplicated here — to a Tenant Registration.</summary>
public sealed class AssignVehicleToOccupancyRegistrationCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationVehicleAssignmentRepository assignments,
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AssignVehicleToOccupancyRegistrationCommandHandler> logger
) : IRequestHandler<AssignVehicleToOccupancyRegistrationCommand, OccupancyRegistrationVehicleAssignmentDto>
{
    public async ValueTask<OccupancyRegistrationVehicleAssignmentDto> Handle(
        AssignVehicleToOccupancyRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        VehicleId vehicleId = new(command.VehicleId);
        Vehicle vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), command.VehicleId);
        if (vehicle.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Vehicle), command.VehicleId);
        }

        OccupancyRegistrationVehicleAssignment assignment = OccupancyRegistrationVehicleAssignment.Assign(
            tenantId, registrationId, vehicleId, clock.UtcNow);

        assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Vehicle {VehicleId} assigned to occupancy registration {OccupancyRegistrationId}, tenant {TenantId}",
            vehicleId, registrationId, tenantId);

        return assignment.ToDto(vehicle);
    }
}
