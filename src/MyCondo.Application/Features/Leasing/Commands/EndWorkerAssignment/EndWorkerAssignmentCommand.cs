using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.EndWorkerAssignment;

public sealed record EndWorkerAssignmentCommand(
    Guid OccupancyRegistrationWorkerAssignmentId
) : IRequest<OccupancyRegistrationWorkerAssignmentDto>, ILifecycleWriteOperation;
