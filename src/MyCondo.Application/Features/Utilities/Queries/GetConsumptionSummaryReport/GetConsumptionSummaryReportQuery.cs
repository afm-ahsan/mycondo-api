using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;

public sealed record GetConsumptionSummaryReportQuery(
    Guid? BuildingId,
    string? UtilityType,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<ConsumptionSummaryLineDto>>;
