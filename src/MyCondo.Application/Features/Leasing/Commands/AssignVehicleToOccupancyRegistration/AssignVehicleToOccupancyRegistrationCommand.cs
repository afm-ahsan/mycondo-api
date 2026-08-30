using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.AssignVehicleToOccupancyRegistration;

public sealed record AssignVehicleToOccupancyRegistrationCommand(
    Guid OccupancyRegistrationId, Guid VehicleId
) : IRequest<OccupancyRegistrationVehicleAssignmentDto>, ILifecycleWriteOperation;
