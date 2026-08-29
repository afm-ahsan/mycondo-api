using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationStatusHistory;

public sealed record GetOccupancyRegistrationStatusHistoryQuery(
    Guid OccupancyRegistrationId
) : IRequest<IReadOnlyList<OccupancyRegistrationStatusHistoryDto>>, ILifecycleReadOperation;
