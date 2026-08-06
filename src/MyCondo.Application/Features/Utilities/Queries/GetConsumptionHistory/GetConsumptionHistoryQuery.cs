using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionHistory;

public sealed record GetConsumptionHistoryQuery(
    Guid MeterId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<ReadingDto>>;
