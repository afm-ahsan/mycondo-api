using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.AssignWorkerToOccupancyRegistration;

public sealed record AssignWorkerToOccupancyRegistrationCommand(
    Guid OccupancyRegistrationId, Guid DomesticWorkerProfileId
) : IRequest<OccupancyRegistrationWorkerAssignmentDto>;
