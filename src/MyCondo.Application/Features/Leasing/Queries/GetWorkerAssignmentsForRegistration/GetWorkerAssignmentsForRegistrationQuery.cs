using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetWorkerAssignmentsForRegistration;

public sealed record GetWorkerAssignmentsForRegistrationQuery(
    Guid OccupancyRegistrationId
) : IRequest<IReadOnlyList<OccupancyRegistrationWorkerAssignmentDto>>;
