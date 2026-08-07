using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderConsumptionReport;

public sealed record GetCylinderConsumptionReportQuery(
    string? CylinderType,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<CylinderConsumptionReportLineDto>>;
