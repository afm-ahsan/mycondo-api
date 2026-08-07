using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.EndVehicleAssignment;

public sealed record EndVehicleAssignmentCommand(
    Guid OccupancyRegistrationVehicleAssignmentId
) : IRequest<OccupancyRegistrationVehicleAssignmentDto>;
