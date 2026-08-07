using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetVehicleAssignmentsForRegistration;

public sealed record GetVehicleAssignmentsForRegistrationQuery(
    Guid OccupancyRegistrationId
) : IRequest<IReadOnlyList<OccupancyRegistrationVehicleAssignmentDto>>;
