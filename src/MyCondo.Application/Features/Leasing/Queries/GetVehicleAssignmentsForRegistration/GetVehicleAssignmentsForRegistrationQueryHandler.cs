using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Leasing.Queries.GetVehicleAssignmentsForRegistration;

public sealed class GetVehicleAssignmentsForRegistrationQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationVehicleAssignmentRepository assignments,
    IVehicleRepository vehicles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetVehicleAssignmentsForRegistrationQuery, IReadOnlyList<OccupancyRegistrationVehicleAssignmentDto>>
{
    public async ValueTask<IReadOnlyList<OccupancyRegistrationVehicleAssignmentDto>> Handle(
        GetVehicleAssignmentsForRegistrationQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(query.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        }

        IReadOnlyList<OccupancyRegistrationVehicleAssignment> result =
            await assignments.GetForRegistrationAsync(registrationId, cancellationToken);

        List<OccupancyRegistrationVehicleAssignmentDto> dtos = [];
        foreach (OccupancyRegistrationVehicleAssignment assignment in result)
        {
            Vehicle? vehicle = await vehicles.GetByIdAsync(assignment.VehicleId, cancellationToken);
            if (vehicle is not null)
            {
                dtos.Add(assignment.ToDto(vehicle));
            }
        }

        return dtos;
    }
}
