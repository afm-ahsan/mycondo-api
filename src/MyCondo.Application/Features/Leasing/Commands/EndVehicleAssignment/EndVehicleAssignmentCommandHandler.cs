using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Leasing.Commands.EndVehicleAssignment;

public sealed class EndVehicleAssignmentCommandHandler(
    IOccupancyRegistrationVehicleAssignmentRepository assignments,
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<EndVehicleAssignmentCommandHandler> logger
) : IRequestHandler<EndVehicleAssignmentCommand, OccupancyRegistrationVehicleAssignmentDto>
{
    public async ValueTask<OccupancyRegistrationVehicleAssignmentDto> Handle(
        EndVehicleAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationVehicleAssignmentId id = new(command.OccupancyRegistrationVehicleAssignmentId);
        OccupancyRegistrationVehicleAssignment assignment = await assignments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistrationVehicleAssignment), command.OccupancyRegistrationVehicleAssignmentId);
        if (assignment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistrationVehicleAssignment), command.OccupancyRegistrationVehicleAssignmentId);
        }

        assignment.End(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Vehicle vehicle = await vehicles.GetByIdAsync(assignment.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), assignment.VehicleId.Value);

        logger.LogInformation(
            "Vehicle assignment {OccupancyRegistrationVehicleAssignmentId} ended, tenant {TenantId}", id, tenantId);

        return assignment.ToDto(vehicle);
    }
}
