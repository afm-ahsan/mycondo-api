using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationStatusHistory;

public sealed record GetOccupancyRegistrationStatusHistoryQuery(
    Guid OccupancyRegistrationId
) : IRequest<IReadOnlyList<OccupancyRegistrationStatusHistoryDto>>;
